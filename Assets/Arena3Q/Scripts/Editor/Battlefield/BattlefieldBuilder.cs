using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Arena3Q.Editor
{
    /// <summary>Builds only owned derivatives and a new scene; never overwrites the original Battle scene or imported meshes.</summary>
    public static class BattlefieldBuilder
    {
        public const string ScenePath = "Assets/Arena3Q/Scenes/Battlefield_HighQuality.unity";
        public const string Content = "Assets/Arena3Q/Content/Battlefield";
        public const string Art = "Assets/Arena3Q/Art/Battlefield";
        private const string Evidence = "NghiemThu/Battlefield/XemTruoc_Map_20261025";
        private static BattlefieldConfig _config;
        private static BattlefieldAssetLibrary _library;
        private static readonly List<Rect> Obstacles = new List<Rect>();
        [Serializable] private sealed class BindingFile { public Binding[] entries; }
        [Serializable] private sealed class Binding { public string id, category, model; public BindingMaterial[] materials; }
        [Serializable] private sealed class BindingMaterial { public string name, albedo, normal; public float cutoff; public bool doubleSided, leaf; }

        [MenuItem("Arena3Q/Battlefield/1 Generate Battlefield")]
        public static void Generate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before generating scene assets.");
            Ensure(Content); Ensure(Art + "/Materials"); Ensure(Art + "/Prefabs"); Ensure(Art + "/Meshes"); Ensure(Art + "/Textures");
            _config = Asset<BattlefieldConfig>(Content + "/BattlefieldConfig.asset");
            _library = Asset<BattlefieldAssetLibrary>(Content + "/BattlefieldAssetLibrary.asset");
            if (!File.Exists(Evidence + "/original-battle.png")) CaptureOriginal();
            BuildLibrary();
            if (!File.Exists(ScenePath))
            {
                if (!AssetDatabase.CopyAsset("Assets/Arena3Q/Scenes/Battle.unity", ScenePath)) throw new IOException("Cannot create separate battlefield scene.");
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildEnvironment();
        }

        [MenuItem("Arena3Q/Battlefield/2 Rebuild Environment")]
        public static void Rebuild()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before rebuilding.");
            if (SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open Battlefield_HighQuality before rebuilding.");
            _config = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(Content + "/BattlefieldConfig.asset");
            _library = AssetDatabase.LoadAssetAtPath<BattlefieldAssetLibrary>(Content + "/BattlefieldAssetLibrary.asset");
            if (_config == null || _library == null) throw new InvalidOperationException("Generate config/library first.");
            BuildEnvironment();
        }

        private static void BuildLibrary()
        {
            var entries = new List<BattlefieldAssetLibrary.Entry>();
            var shader = Shader.Find("Arena3Q/Battlefield/Stylized Environment");
            if (shader == null) throw new InvalidOperationException("Environment shader missing.");
            var owned = AssetDatabase.LoadAssetAtPath<MapProps3DLibrary>("Assets/Arena3Q/Content/Maps/MapProps3D.asset");
            foreach (var prop in owned.Props)
            {
                string filename = Art + "/Prefabs/" + prop.name + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(filename);
                if (prefab == null)
                {
                    var root = new GameObject(prop.name);
                    var mesh = new GameObject("Original geometry", typeof(MeshFilter), typeof(MeshRenderer));
                    mesh.transform.SetParent(root.transform, false);
                    mesh.transform.localPosition = (prop.meshPosition - prop.groundCentre) * prop.scale;
                    mesh.transform.localRotation = prop.meshRotation;
                    mesh.transform.localScale = prop.meshScale * prop.scale;
                    mesh.GetComponent<MeshFilter>().sharedMesh = prop.mesh;
                    var material = Material(prop.name, shader);
                    material.SetTexture("_BaseMap", prop.material.GetTexture("_BaseMap"));
                    if (prop.material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", prop.material.GetTexture("_BumpMap"));
                    material.SetFloat("_Smoothness", .08f);
                    if (prop.name.Contains("Banner")) { material.SetFloat("_WindBend", .1f); material.SetFloat("_PlantHeight", 3f); }
                    mesh.GetComponent<MeshRenderer>().sharedMaterial = material;
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, filename);
                    Object.DestroyImmediate(root);
                }
                var ownedMaterial=Material(prop.name,shader);
                ownedMaterial.SetTexture("_BaseMap",OwnedTexture(prop.name+"_Albedo",prop.material.GetTexture("_BaseMap"),false));
                if(prop.material.HasProperty("_BumpMap"))ownedMaterial.SetTexture("_BumpMap",OwnedTexture(prop.name+"_Normal",prop.material.GetTexture("_BumpMap"),true));
                EditorUtility.SetDirty(ownedMaterial);
                entries.Add(new BattlefieldAssetLibrary.Entry { id = prop.name, category = "Arena3Q original", prefab = prefab });
            }
            var bindings = JsonUtility.FromJson<BindingFile>(File.ReadAllText("docs/BanDo/Tools/BattlefieldAssetBindings.json"));
            foreach (var binding in bindings.entries)
            {
                string filename = Art + "/Prefabs/" + binding.id + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(filename);
                if (prefab == null)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(binding.model);
                    if (model == null) throw new InvalidOperationException("Missing licensed model: " + binding.model);
                    var root = new GameObject(binding.id);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    instance.transform.SetParent(root.transform, false);
                    foreach (var collider in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                    foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                    {
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            string sourceName = materials[i] != null ? materials[i].name : "";
                            var spec = binding.materials.FirstOrDefault(m => sourceName.StartsWith(m.name, StringComparison.Ordinal)) ?? binding.materials[Mathf.Min(i, binding.materials.Length - 1)];
                            string materialId = "Nature_" + spec.name;
                            var material = Material(materialId, shader);
                            material.SetTexture("_BaseMap", Texture(spec.albedo, false));
                            if (!string.IsNullOrEmpty(spec.normal)) material.SetTexture("_BumpMap", Texture(spec.normal, true));
                            material.SetFloat("_Cutoff", spec.cutoff);
                            material.SetFloat("_Cull", spec.doubleSided ? 0f : 2f);
                            material.SetFloat("_WindBend", spec.leaf ? 1f : .05f);
                            material.SetFloat("_PlantHeight", binding.id.Contains("Tree") || binding.id.Contains("Pine") ? 5.5f : 1f);
                            material.SetFloat("_Smoothness", .06f);
                            materials[i] = material;
                        }
                        renderer.sharedMaterials = materials;
                    }
                    Bounds bounds = BoundsOf(root);
                    float desired = DesiredSize(binding.id);
                    bool tall = binding.id.Contains("Tree") || binding.id.Contains("Pine");
                    float scale = desired / Mathf.Max(.01f, tall ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.z));
                    instance.transform.localScale *= scale;
                    bounds = BoundsOf(root);
                    instance.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, filename);
                    Object.DestroyImmediate(root);
                }
                if(binding.id.Contains("Tree") || binding.id.StartsWith("Pine"))
                    prefab=BattlefieldVegetationLod.Ensure(prefab,Art);
                entries.Add(new BattlefieldAssetLibrary.Entry { id = binding.id, category = binding.category, prefab = prefab });
            }
            foreach (string missing in new[] { "Tree_Bamboo", "Tree_Peach", "Tree_Maple", "Oriental_StoneLantern", "Oriental_Ruins" })
                entries.Add(new BattlefieldAssetLibrary.Entry { id = missing, category = "Missing exact asset", missing = true, note = "Use approved generic vegetation/rocks; no exact-species claim." });
            _library.entries = entries.ToArray();
            string[] ground = { "DrySand", "PackedEarth", "GravelSoil", "OldPaving", "DryGrass", "SparseGreenGrass" };
            for (int i = 0; i < ground.Length; i++)
            {
                string target = Art + "/Textures/" + ground[i] + ".png";
                if (!File.Exists(target)) File.Copy("NghiemThu/Battlefield/XemTruoc_Assets_20261025/textures/" + ground[i] + ".png", target);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
                _library.groundAlbedo[i] = Texture(target, false, true);
            }
            _library.groundMaterial = Material("Ground", Shader.Find("Arena3Q/Battlefield/Blended Ground"));
            string[] slots = { "_Sand", "_Road", "_Gravel", "_Paving", "_Dry", "_Green" };
            for (int i = 0; i < slots.Length; i++) _library.groundMaterial.SetTexture(slots[i], _library.groundAlbedo[i]);
            _library.runeMaterial = Material("RuneAmber", Shader.Find("Arena3Q/Battlefield/Gate Rune"));
            _library.fireMaterial = ParticleMaterial("Fire", "fire_01");
            _library.smokeMaterial = ParticleMaterial("Smoke", "smoke_01");
            _library.dustMaterial = ParticleMaterial("Dust", "dirt_02");
            BuildLeaf();
            EditorUtility.SetDirty(_library);
            AssetDatabase.SaveAssets();
        }

        private static void CaptureOriginal()
        {
            EditorSceneManager.OpenScene("Assets/Arena3Q/Scenes/Battle.unity",OpenSceneMode.Single);
            var battle=Object.FindAnyObjectByType<BattleController>();
            battle.StartBattle(battle.Stages[0]);
            var camera=Object.FindAnyObjectByType<Camera>();
            BattleView3D.ConfigureCamera(camera,battle);
            BattleView3D.PlaceCamera(camera.transform,battle,Vector2.zero);
            camera.aspect=1600f/900f;
            camera.orthographicSize=Mathf.Max((_config.size.x+_config.rimWidth*2f)/camera.aspect,(_config.size.y+_config.rimWidth*2f)*Mathf.Sin(_config.cameraElevation*Mathf.Deg2Rad))*_config.cameraPadding*.5f;
            Capture(camera,"original-battle.png");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }

        private static float DesiredSize(string id)
        {
            if (id.Contains("Tree")) return 5.5f;
            if (id.Contains("Pine")) return 7f;
            if (id.Contains("Bush")) return 2f;
            if (id.StartsWith("Grass")) return .7f;
            if (id.StartsWith("Rock_Medium")) return 3f;
            if (id.StartsWith("RockPath")) return 1.2f;
            if (id.StartsWith("Pebble")) return .35f;
            if (id.StartsWith("Flower") || id.StartsWith("Clover") || id.StartsWith("Fern") || id.StartsWith("Plant")) return .85f;
            if (id.Contains("Cart")) return 2.8f;
            if (id.Contains("WeaponStand") || id == "Workbench") return 2.2f;
            if (id.Contains("Torch") || id.Contains("Lantern")) return .65f;
            return 1.1f;
        }

        private static void BuildEnvironment()
        {
            foreach (var marker in Object.FindObjectsByType<BattlefieldGenerated>())
                if (marker.gameObject.scene == SceneManager.GetActiveScene()) Object.DestroyImmediate(marker.gameObject);
            Obstacles.Clear();
            var root = new GameObject("Arena3Q_Battlefield");
            root.AddComponent<BattlefieldGenerated>().seed = _config.seed;
            var environment = root.AddComponent<BattlefieldEnvironment>(); environment.config = _config; environment.assets = _library;
            var world = Group("Environment", root.transform);
            var terrain = Group("Terrain", world); var roads = Group("Roads", world);
            var cliffs = Group("Cliffs", world); var rocks = Group("Rocks", world);
            var flora = Group("Vegetation", world); var trees = Group("Trees", flora); var bushes = Group("Bushes", flora);
            var grass = Group("Grass", flora); var flowers = Group("Flowers", flora);
            environment.grassRoot = grass;
            var details = Group("Decorations", world); environment.detailRoot = details;
            Terrain(terrain);
            var center = Group("BattleCenter", root.transform); Center(center);
            var camps = Group("Camps", root.transform); Camp(Group("BlueCamp", camps), true); Camp(Group("RedCamp", camps), false);
            Scatter(trees, bushes, grass, flowers, rocks, cliffs, roads, details);
            Lighting(root.transform, environment);
            BuildQuality(environment);
            Vfx(root.transform, environment);
            var camera = Object.FindAnyObjectByType<Camera>();
            environment.battleCamera = camera;
            if (camera == null) throw new InvalidOperationException("Original battle camera missing.");
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.37f, .48f, .53f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camera.GetUniversalAdditionalCameraData().renderShadows = true;
            camera.aspect = 1600f/900f;
            environment.FitCamera();
            ConfigureGameplay(root.transform, camera, environment);
            foreach (var sceneRoot in SceneManager.GetActiveScene().GetRootGameObjects())
                if (sceneRoot.name == "Global Light 2D") sceneRoot.SetActive(false);
            environment.ApplyLighting(); environment.ApplyQuality();
            MissingAssetAnchors(root.transform);
            SaveSections(root.transform);
            var instancing=root.AddComponent<BattlefieldInstancing>();instancing.environment=environment;
            instancing.battle=Object.FindAnyObjectByType<BattleController>();
            instancing.roots=new[]{world.Find("Vegetation"),world.Find("Rocks"),world.Find("Cliffs"),world.Find("Roads"),world.Find("Decorations"),camps};
            var controls=root.AddComponent<Arena3Q.UI.BattlefieldTestControls>();controls.environment=environment;controls.battle=instancing.battle;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Validate();
            Capture(environment.battleCamera, "phase2-overview.png");
            Debug.Log("[Battlefield] Scene built safely: " + ScenePath);
        }

        private static void MissingAssetAnchors(Transform root)
        {
            var parent=Group("AssetReplacementAnchors",root);int index=0;
            foreach(var entry in _library.entries.Where(e=>e.missing))
            {
                foreach(float side in new[]{-1f,1f})
                {
                    var marker=Group(entry.id,parent);marker.position=_config.Point(new Vector2(side*(_config.size.x*.5f-2f),side*(17f+index*1.3f)));
                    var slot=marker.gameObject.AddComponent<BattlefieldAssetSlot>();slot.assetId=entry.id;slot.replacementNote=entry.note+" Drag a licensed replacement under this anchor; update the matching library entry. Anchor only, no visible placeholder.";
                }
                index++;
            }
        }
        private static void SaveSections(Transform root)
        {
            Ensure(Art+"/Prefabs/Sections");
            // Small nested prefab files keep the scene editable and reviewable in Git.
            var vfx=root.Find("EnvironmentVFX");
            for(int i=0;i<vfx.childCount;i++)PrefabUtility.SaveAsPrefabAssetAndConnect(vfx.GetChild(i).gameObject,Art+"/Prefabs/Sections/VFX_"+i+".prefab",InteractionMode.AutomatedAction);
            var environment=root.Find("Environment");
            for(int i=0;i<environment.childCount;i++)
            {
                var section=environment.GetChild(i);
                if(section.name=="Vegetation")for(int j=0;j<section.childCount;j++)Save(section.GetChild(j),"Vegetation_");
                else Save(section,"Environment_");
            }
            Save(root.Find("BattleCenter"),"");Save(root.Find("Camps/BlueCamp"),"");Save(root.Find("Camps/RedCamp"),"");Save(root.Find("Lighting"),"");
            void Save(Transform section,string prefix)=>PrefabUtility.SaveAsPrefabAssetAndConnect(section.gameObject,Art+"/Prefabs/Sections/"+prefix+section.name+".prefab",InteractionMode.AutomatedAction);
        }

        [MenuItem("Arena3Q/Battlefield/4 Clear Generated Decorations")]
        public static void ClearDecorations()
        {
            var marker=Object.FindAnyObjectByType<BattlefieldGenerated>();if(marker==null)throw new InvalidOperationException("No generated battlefield in scene.");
            var target=marker.transform.Find("Environment/Decorations");if(target==null)return;
            if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.UnpackPrefabInstance(target.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.UserAction);
            for(int i=target.childCount-1;i>=0;i--)Undo.DestroyObjectImmediate(target.GetChild(i).gameObject);
            EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
        }
        [MenuItem("Arena3Q/Battlefield/Graphics/Mobile Low")]
        public static void Low()=>Preset(BattlefieldGraphics.Low);
        [MenuItem("Arena3Q/Battlefield/Graphics/Mobile Medium")]
        public static void Medium()=>Preset(BattlefieldGraphics.Medium);
        [MenuItem("Arena3Q/Battlefield/Graphics/High")]
        public static void High()=>Preset(BattlefieldGraphics.High);
        private static void Preset(BattlefieldGraphics preset)
        {
            var environment=Object.FindAnyObjectByType<BattlefieldEnvironment>();if(environment==null)throw new InvalidOperationException("No battlefield environment.");
            Undo.RecordObject(environment,"Battlefield graphics preset");environment.quality=preset;environment.GetComponent<BattlefieldQuality>().Apply();EditorUtility.SetDirty(environment);
        }

        private static void Terrain(Transform parent)
        {
            // Continue the landscape beyond the tactical frame so no rectangular mesh edge is visible.
            float margin = _config.rimWidth + 18f;
            int nx = Mathf.CeilToInt((_config.size.x + margin * 2f) / _config.cellSize);
            int nz = Mathf.CeilToInt((_config.size.y + margin * 2f) / _config.cellSize);
            var vertices = new Vector3[(nx + 1) * (nz + 1)]; var colors = new Color[vertices.Length]; var uv = new Vector2[vertices.Length]; var uv2 = new Vector2[vertices.Length];
            for (int z = 0; z <= nz; z++) for (int x = 0; x <= nx; x++)
            {
                int index = z * (nx + 1) + x;
                var p = new Vector2(Mathf.Lerp(-_config.size.x * .5f - margin, _config.size.x * .5f + margin, x / (float)nx), Mathf.Lerp(-_config.size.y * .5f - margin, _config.size.y * .5f + margin, z / (float)nz));
                vertices[index] = _config.Point(p, _config.TerrainHeight(p)); uv[index] = p / 4f;
                float noise = Mathf.PerlinNoise(Mathf.Abs(p.x) * .2f, Mathf.Abs(p.y) * .2f);
                float road = 1f - SmoothRange(_config.roadWidth * .3f, _config.roadWidth * .7f, _config.RoadDistance(p));
                float paving = 1f - SmoothRange(_config.altarDiameter * .52f, _config.altarDiameter * .75f, p.magnitude);
                float camp = Mathf.Min(Vector2.Distance(p, new Vector2(-_config.campX, 0f)), Vector2.Distance(p, new Vector2(_config.campX, 0f)));
                paving = Mathf.Max(paving, (1f - SmoothRange(3f, 6.5f, camp)) * .8f);
                float grass = SmoothRange(_config.ringRadius + 1.5f, _config.ringRadius + 7f, p.magnitude) * SmoothRange(2f, 6f, _config.RoadDistance(p));
                if (Mathf.Abs(p.x) > _config.campX - 9f && Mathf.Abs(p.y) < 15f) grass *= .55f;
                colors[index] = new Color(road * (1f - paving), noise * .5f, paving, grass * (.8f + noise * .2f));
                uv2[index] = new Vector2(SmoothRange(.5f, .9f, noise), 0f);
            }
            var triangles = new int[nx * nz * 6]; int t = 0;
            for (int z = 0; z < nz; z++) for (int x = 0; x < nx; x++)
            {
                int a = z * (nx + 1) + x, b = a + 1, c = a + nx + 1, d = c + 1;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b; triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
            Array.Resize(ref triangles,t);
            // Four native mesh tiles keep each editable generated asset below the repository's size limit.
            // Shared border positions and analytic normals avoid lighting seams between tiles.
            for(int part=0;part<4;part++)
            {
                int start=part*nx/4,end=(part+1)*nx/4,width=end-start+1,count=width*(nz+1);
                var tv=new Vector3[count];var tc=new Color[count];var tu=new Vector2[count];var tu2=new Vector2[count];var normals=new Vector3[count];
                for(int z=0;z<=nz;z++)for(int x=start;x<=end;x++)
                {
                    int local=z*width+x-start,source=z*(nx+1)+x;tv[local]=vertices[source];tc[local]=colors[source];tu[local]=uv[source];tu2[local]=uv2[source];
                    var p=new Vector2(tv[local].x,tv[local].z);const float e=.05f;
                    normals[local]=new Vector3(_config.TerrainHeight(p-Vector2.right*e)-_config.TerrainHeight(p+Vector2.right*e),2f*e,_config.TerrainHeight(p-Vector2.up*e)-_config.TerrainHeight(p+Vector2.up*e)).normalized;
                }
                var indices=new int[(width-1)*nz*6];int index=0;
                for(int z=0;z<nz;z++)for(int x=0;x<width-1;x++){int a=z*width+x,b=a+1,c=a+width,d=c+1;indices[index++]=a;indices[index++]=c;indices[index++]=b;indices[index++]=b;indices[index++]=c;indices[index++]=d;}
                string assetPath=Art+"/Meshes/Terrain_"+part+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                bool created=mesh==null;if(created)mesh=new Mesh{name="BattlefieldTerrain_"+part};else mesh.Clear();
                mesh.vertices=tv;mesh.normals=normals;mesh.colors=tc;mesh.uv=tu;mesh.uv2=tu2;mesh.triangles=indices;mesh.RecalculateBounds();mesh.UploadMeshData(false);
                if(created)AssetDatabase.CreateAsset(mesh,assetPath);else EditorUtility.SetDirty(mesh);
                var go=new GameObject("Terrain_6LayerBlend_"+part,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent,false);go.isStatic=true;
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=_library.groundMaterial;
            }
        }

        private static void Center(Transform parent)
        {
            var altar = Place("BaguaRoundStoneAltar", parent, Vector2.zero, 0f, _config.altarDiameter / 4f);
            altar.name = "ExistingBaguaAltar";
            var altarFilter=altar.GetComponentInChildren<MeshFilter>();
            altarFilter.gameObject.AddComponent<MeshCollider>().sharedMesh=altarFilter.sharedMesh;
            var ring = Group("ExistingOctagonalPillarsAndRunes", parent);
            Color[] colors = { new Color(.24f,.65f,1f), new Color(.7f,.27f,1f), new Color(1f,.65f,.13f), new Color(.9f,.2f,.12f), new Color(.3f,.65f,1f), new Color(.7f,.25f,1f), new Color(.4f,.9f,.3f), new Color(1f,.66f,.1f) };
            for (int i = 0; i < 8; i++)
            {
                float angle = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                var p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _config.ringRadius;
                var pillar=Place("BaguaStonePillar", ring, p, -i * 45f - 22.5f, 1.7f);
                var body=pillar.AddComponent<CapsuleCollider>();body.radius=1f;body.height=BoundsOf(pillar).size.y/1.7f;body.center=Vector3.up*body.height*.5f;
                float gateAngle = i * Mathf.PI / 4f;
                var gate = new Vector2(Mathf.Cos(gateAngle), Mathf.Sin(gateAngle)) * _config.ringRadius;
                var rune = new GameObject("Rune " + i, typeof(MeshFilter), typeof(MeshRenderer)); rune.transform.SetParent(ring, false);
                rune.transform.position = _config.Point(gate, .04f); rune.transform.rotation = Quaternion.Euler(90f, 0f, i * 45f); rune.transform.localScale = Vector3.one * 2.5f;
                rune.GetComponent<MeshFilter>().sharedMesh = Quad();
                var mat = Material("Rune_" + i, _library.runeMaterial.shader); mat.SetColor("_BaseColor", colors[i]); rune.GetComponent<MeshRenderer>().sharedMaterial = mat;
                rune.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void Camp(Transform parent, bool blue)
        {
            float side = blue ? -1f : 1f; float face = blue ? 90f : 270f; float x = _config.campX * side;
            void Add(string id, float dx, float z, float yaw, float scale = 1f, bool obstacle = false)
            {
                var go = Place(id, parent, new Vector2(x + dx * side, z * side), yaw + (blue ? 0f : 180f), scale);
                if (blue) Recolour(go, _config.blue);
                if (obstacle) AddObstacle(go);
            }
            Add("ThreeKingdomsCommand_2", 0f, 0f, blue ? face : face - 180f, 1.45f);
            Add("ThreeKingdomsSquare", 3.3f, 8f, 0f, 1.65f, true);
            Add("ThreeKingdomsSquare", 4f, -8.3f, 18f, 1.2f, true);
            Add("ThreeKingdomsSquare", 3.8f, -12.4f, 3f, .95f, true);
            Add("WoodenCampWatchtower", -6.5f, 12.7f, -90f, 1.45f, true);
            Add("WoodenCampWatchtower", -6.5f, -12.7f, -90f, 1.45f, true);
            Add("WoodenCampWatchtower", -6.8f, 5.3f, -90f, .85f, true);
            Add("WoodenCampWatchtower", -6.8f, -5.3f, -90f, .85f, true);
            for (int i = 0; i < 4; i++)
            {
                Add("WoodenPalisadeFence", -6.8f, 5.4f + i * 2.15f, 90f, 1.05f, true);
                Add("WoodenPalisadeFence", -6.8f, -5.4f - i * 2.15f, 90f, 1.05f, true);
                Add("WoodenPalisadeFence", -4.5f + i * 2.2f, 15f, 0f, 1.05f, true);
                Add("WoodenPalisadeFence", -4.5f + i * 2.2f, -15f, 0f, 1.05f, true);
            }
            Add("MilitaryFlagpoleBanner", -6f, 3.1f, face, 1.2f);
            Add("MilitaryFlagpoleBanner", -6f, -3.1f, face, 1.2f);
            Add("MilitaryFlagpoleBanner", 4f, 3f, face, 1f);
            Add("WeaponStand", -1.5f, 6f, 0f, 1f);
            Add("Sword_Bronze", -1.5f, 6.1f, 0f, .8f);
            Add("Stall_Cart_Empty", 5.5f, 3.7f, 90f, .85f);
            Add("Crate_Wooden", 2.7f, 4.8f, 14f);
            Add("Crate_Wooden", 4f, 5.7f, -5f);
            Add("Barrel", .8f, 7f, 0f, .8f);
            Add("Barrel", 1.8f, 7.3f, 0f, .7f);
            Add("Bag", 4.8f, 5.7f, 35f, .7f);
            Add("Dummy", -2f, -7.5f, 90f, 1f);
            Add("Workbench", 4.6f, -5f, 90f, .7f);
            Add("Torch_Metal", -5.8f, 3.9f, 0f);
            Add("Torch_Metal", -5.8f, -3.9f, 0f);
        }

        private static void Scatter(Transform trees, Transform bushes, Transform grass, Transform flowers, Transform rocks, Transform cliffs, Transform roads, Transform details)
        {
            var rng = new System.Random(_config.seed);
            float Rand(float lo, float hi) => Mathf.Lerp(lo, hi, (float)rng.NextDouble());
            string[] treeIds = { "CommonTree_1", "CommonTree_2", "CommonTree_5", "Pine_1", "Pine_3", "TwistedTree_1" };
            int accepted = 0;
            for (int attempt = 0; accepted < _config.treePairs && attempt < 500; attempt++)
            {
                var p = new Vector2(Rand(-_config.size.x*.5f-2f,_config.size.x*.5f+2f), Rand(_config.size.y*.5f+2.8f,_config.size.y*.5f+5f));
                if (Mathf.Abs(p.x) < 7f) continue;
                string id = treeIds[rng.Next(treeIds.Length)]; float scale = Rand(.65f, 1.2f), yaw = Rand(0f, 360f);
                Pair(id, trees, p, yaw, scale); accepted++;
            }
            // Side groves stay outside the walkable rectangle and leave the camp entrances clear.
            for(int i=0;i<12;i++)Pair(treeIds[i%treeIds.Length],trees,new Vector2(Rand(_config.size.x*.5f+3f,_config.size.x*.5f+6f),Rand(-_config.size.y*.5f+1f,_config.size.y*.5f-1f)),Rand(0,360),Rand(.55f,.85f));
            for (int i = 0; i < 30; i++)
            {
                var p = new Vector2(Rand(-_config.size.x*.5f-2f,_config.size.x*.5f+2f), Rand(_config.size.y*.5f+1.4f,_config.size.y*.5f+3.4f));
                if (Mathf.Abs(p.x) < 7f) continue;
                Pair("Rock_Medium_" + (1 + i % 3), cliffs, p, Rand(0, 360), Rand(.65f, 1.45f));
            }
            for (int i = 0; i < _config.bushPairs; i++)
            {
                var p = new Vector2(Rand(-_config.size.x*.5f+1f,_config.size.x*.5f-1f), Rand(_config.size.y*.5f-4.2f,_config.size.y*.5f));
                if (_config.RoadDistance(p) < _config.roadWidth * .5f) continue;
                Pair(i % 3 == 0 ? "Bush_Common_Flowers" : "Bush_Common", bushes, p, Rand(0, 360), Rand(.55f, 1f));
                Pair(i % 2 == 0 ? "Flower_3_Group" : "Flower_4_Group", flowers, p + new Vector2(.6f, -1f), Rand(0, 360), Rand(.7f, 1.5f));
                if (i % 2 == 0) Pair("Rock_Medium_" + (1 + i % 3), rocks, p + Vector2.right * 1.2f, Rand(0, 360), Rand(.25f, .65f));
            }
            for (int i = 0; i < 20; i++)
            {
                float angle = Rand(0, Mathf.PI * 2f);
                var p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Rand(_config.ringRadius + 2.8f, _config.ringRadius + 4.6f);
                if (_config.RoadDistance(p) < _config.roadWidth * .7f || Mathf.Abs(p.y) < 3f) continue;
                Pair(i % 2 == 0 ? "Fern_1" : "Plant_1_Big", bushes, p, Rand(0, 360), Rand(.5f, .9f));
                Pair("Pebble_Round_1", details, p + new Vector2(.8f,.4f), Rand(0,360), Rand(.7f,1.4f));
            }
            foreach(var p in new[]{new Vector2(5.5f,14.3f),new Vector2(-5.5f,14.3f),new Vector2(17f,10f),new Vector2(-17f,10f)})
            {
                Pair("Fern_1",bushes,p,30f,.75f);Pair("Flower_3_Group",flowers,p+new Vector2(.5f,.3f),70f,1.1f);
                Pair("Bush_Common",bushes,p+new Vector2(1f,.8f),20f,.45f);
            }
            // Bake many existing grass meshes into a small number of patch meshes, not thousands of GameObjects.
            for (int i = 0; i < _config.grassPatchPairs; i++)
            {
                var p = new Vector2(Rand(-_config.size.x*.5f,_config.size.x*.5f), Rand(_config.size.y*.5f-4.3f,_config.size.y*.5f+1f));
                if (_config.RoadDistance(p) < _config.roadWidth * .8f) continue;
                var patch=GrassPatch(grass,p,i);float yaw=Rand(0,360);patch.transform.rotation=Quaternion.Euler(0,yaw,0);
                var opposite=Object.Instantiate(patch,parent:grass);opposite.name="GrassPatch "+i+" opposite";opposite.transform.position=_config.Point(-p,_config.TerrainHeight(p));opposite.transform.rotation=Quaternion.Euler(0,yaw+180,0);
            }
            // Sparse worn stone edging, kept off the broad road's clear middle.
            for (int i = 0; i < 23; i++)
            {
                float x = Mathf.Lerp(-_config.campX + 7f, _config.campX - 7f, i / 22f);
                float z = Mathf.Sin((x + _config.campX) / (_config.campX * 2f) * Mathf.PI) * (_config.ringRadius + 5f);
                Pair("RockPath_Round_Small_1", roads, new Vector2(x, z + _config.roadWidth * .55f), Rand(0,360), Rand(.5f,.95f));
            }
        }

        private static GameObject GrassPatch(Transform parent, Vector2 p, int id)
        {
            int variant=id%4;var rng=new System.Random(_config.seed+variant);
            var prefab = _library.Find(id % 2 == 0 ? "Grass_Common_Short" : "Grass_Wispy_Short");
            var source = prefab.GetComponentInChildren<MeshFilter>();
            string assetPath = AssetDatabase.GetAssetPath(source.sharedMesh);
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer != null && !importer.isReadable) { importer.isReadable = true; importer.SaveAndReimport(); source = prefab.GetComponentInChildren<MeshFilter>(); }
            var parts = new CombineInstance[_config.grassPerPatch];
            for (int i = 0; i < parts.Length; i++)
            {
                float x = ((float)rng.NextDouble() - .5f) * 3f, z = ((float)rng.NextDouble() - .5f) * 2.5f;
                float scale = .65f + (float)rng.NextDouble() * .5f;
                parts[i] = new CombineInstance { mesh = source.sharedMesh, transform = Matrix4x4.TRS(new Vector3(x, 0f, z), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0), Vector3.one * scale) * source.transform.localToWorldMatrix };
            }
            var combined = new Mesh { name = "GrassPatch_" + id }; combined.CombineMeshes(parts, true, true); combined.RecalculateBounds();
            string filename = Art + "/Meshes/GrassVariant_" + variant + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(filename);
            if (existing == null) AssetDatabase.CreateAsset(combined, filename); else { existing.Clear();existing.vertices=combined.vertices;existing.normals=combined.normals;existing.tangents=combined.tangents;existing.uv=combined.uv;existing.triangles=combined.triangles;existing.RecalculateBounds();existing.UploadMeshData(false);EditorUtility.SetDirty(existing);Object.DestroyImmediate(combined);combined=existing; }
            var go = new GameObject("GrassPatch " + id, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false); go.transform.position = _config.Point(p, _config.TerrainHeight(p));
            go.GetComponent<MeshFilter>().sharedMesh = combined; go.GetComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials; go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static void Lighting(Transform root, BattlefieldEnvironment environment)
        {
            var lighting = Group("Lighting", root);
            var go = new GameObject("Warm evening sun", typeof(Light)); go.transform.SetParent(lighting, false);
            var light = go.GetComponent<Light>(); light.type = LightType.Directional; light.shadows = LightShadows.Soft; light.shadowBias = .025f; light.shadowNormalBias = .25f;
            environment.sun = light;
            string profilePath = Content + "/EveningVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, profilePath);
                var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES); AssetDatabase.AddObjectToAsset(tone, profile);
                var color = profile.Add<ColorAdjustments>(true); color.postExposure.Override(.05f); color.saturation.Override(-8f); color.contrast.Override(4f); AssetDatabase.AddObjectToAsset(color, profile);
                var bloom = profile.Add<Bloom>(true); bloom.threshold.Override(1.4f); bloom.intensity.Override(.16f); bloom.scatter.Override(.45f); AssetDatabase.AddObjectToAsset(bloom, profile);
            }
            var volumeGo = new GameObject("Evening grading", typeof(Volume)); volumeGo.transform.SetParent(lighting, false); var volume = volumeGo.GetComponent<Volume>();volume.isGlobal = true; volume.priority = 15f; volume.sharedProfile = profile; environment.volume = volume;
        }

        private static void Vfx(Transform root, BattlefieldEnvironment environment)
        {
            var parent = Group("EnvironmentVFX", root); var particles = new List<ParticleSystem>();
            foreach (float side in new[] { -1f, 1f }) foreach (float z in new[] { -4f, 4f })
            {
                var p = new Vector3(side * (_config.campX - 5.8f), 1.5f, z);
                particles.Add(Particle("Torch flame", parent, p, _library.fireMaterial, new Color(1f,.6f,.13f,.8f), 6f, 1.1f, .45f, .35f, 20));
                particles.Add(Particle("Torch smoke", parent, p + Vector3.up * .25f, _library.smokeMaterial, new Color(.36f,.32f,.28f,.15f), 1.5f, 3f, .7f, .45f, 10));
            }
            foreach (float side in new[] { -1f, 1f })
            {
                var dust = Particle("Sparse golden dust", parent, new Vector3(0f,1.5f,side*19f), _library.dustMaterial, new Color(1f,.84f,.55f,.12f), 1.5f, 9f, .2f, .22f, 24);
                var shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(65f,2f,4f); particles.Add(dust);
                var leaves=Particle("Wind-blown leaves",parent,new Vector3(0f,5f,side*21f),_library.leafMaterial,new Color(.9f,.75f,.4f,.65f),1.4f,8f,.22f,.05f,18);
                var ls=leaves.shape;ls.shapeType=ParticleSystemShapeType.Box;ls.scale=new Vector3(66f,1f,3f);
                var velocity=leaves.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=new ParticleSystem.MinMaxCurve(.35f,.7f);velocity.y=new ParticleSystem.MinMaxCurve(-.6f,-.25f);velocity.z=new ParticleSystem.MinMaxCurve(.05f,.2f);
                var spin=leaves.rotationOverLifetime;spin.enabled=true;spin.separateAxes=true;spin.x=new ParticleSystem.MinMaxCurve(-.8f,.8f);spin.z=new ParticleSystem.MinMaxCurve(-1.3f,1.3f);
                var noise=leaves.noise;noise.enabled=true;noise.strength=.17f;noise.frequency=.3f;noise.scrollSpeed=.15f;
                var lr=leaves.GetComponent<ParticleSystemRenderer>();lr.renderMode=ParticleSystemRenderMode.Mesh;lr.mesh=_library.leafMesh;particles.Add(leaves);
            }
            environment.particles = particles.ToArray();
        }

        private static void BuildLeaf()
        {
            var source=_library.Find("CommonTree_1").GetComponentInChildren<MeshFilter>();
            var importer=(ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(source.sharedMesh));
            if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();source=_library.Find("CommonTree_1").GetComponentInChildren<MeshFilter>();}
            var mats=source.GetComponent<MeshRenderer>().sharedMaterials;
            int sub=Array.FindIndex(mats,m=>m.name.Contains("Leaves"));
            if(sub<0)throw new InvalidOperationException("Cannot extract an approved tree leaf.");
            var sourceMesh=source.sharedMesh;var indices=sourceMesh.GetTriangles(sub).Take(6).ToArray();var unique=indices.Distinct().ToArray();
            string path=Art+"/Meshes/LeafCard.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var points=unique.Select(i=>sourceMesh.vertices[i]).ToArray();var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);
                mesh=new Mesh{name="Existing tree leaf card",vertices=points.Select(p=>(p-b.center)/Mathf.Max(b.size.x,b.size.y,b.size.z)).ToArray(),uv=unique.Select(i=>sourceMesh.uv[i]).ToArray(),triangles=indices.Select(i=>Array.IndexOf(unique,i)).ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            _library.leafMesh=mesh;
            var material=ParticleMaterial("Leaves","dirt_02");material.SetTexture("_BaseMap",mats[sub].GetTexture("_BaseMap"));material.SetFloat("_Cull",0f);EditorUtility.SetDirty(material);_library.leafMaterial=material;
        }

        private static ParticleSystem Particle(string name, Transform parent, Vector3 p, Material material, Color color, float rate, float life, float size, float speed, int maximum)
        {
            var go = new GameObject(name, typeof(ParticleSystem)); go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.rotation = Quaternion.Euler(-90,0,0);
            var ps = go.GetComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = true; main.startLifetime = new ParticleSystem.MinMaxCurve(life*.7f,life); main.startSize = new ParticleSystem.MinMaxCurve(size*.6f,size); main.startSpeed = new ParticleSystem.MinMaxCurve(speed*.7f,speed); main.startColor = color; main.maxParticles = maximum; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.radius = .12f; shape.angle = 10f;
            var fade = ps.colorOverLifetime; fade.enabled = true; var gradient = new Gradient(); gradient.SetKeys(new[] {new GradientColorKey(Color.white,0f),new GradientColorKey(Color.white,1f)},new[]{new GradientAlphaKey(0f,0f),new GradientAlphaKey(1f,.15f),new GradientAlphaKey(.65f,.65f),new GradientAlphaKey(0f,1f)});fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            return ps;
        }

        private static void ConfigureGameplay(Transform root, Camera camera, BattlefieldEnvironment environment)
        {
            var battle = Object.FindAnyObjectByType<BattleController>();
            if (battle == null) throw new InvalidOperationException("Original battle controller missing.");
            var formation = CloneAsset<BaguaFormationConfig>("Assets/Arena3Q/Content/Maps/BaguaFormation_BatTran.asset", Content + "/BaguaFormation.asset");
            var form = new SerializedObject(formation); float scale = _config.ringRadius / 5.7f;
            form.FindProperty("altarRadius").floatValue = _config.altarDiameter * .5f;
            var pillars = form.FindProperty("pillars"); var gates = form.FindProperty("gates");
            for (int i = 0; i < 8; i++)
            {
                float angle = (i*45f+22.5f)*Mathf.Deg2Rad; pillars.GetArrayElementAtIndex(i).vector2Value = new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*_config.ringRadius;
                gates.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2Value = new Vector2(Mathf.Cos(i*Mathf.PI/4f),Mathf.Sin(i*Mathf.PI/4f))*_config.ringRadius;
            }
            form.FindProperty("pillarRadius").floatValue = 1.7f;
            form.FindProperty("slabLength").floatValue = 2.5f*scale;
            form.FindProperty("triggerSize").vector2Value = new Vector2(2.4f*scale,1f);
            form.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(formation);
            var stages = new List<StageConfig>();
            foreach (string type in new[] {"Attack","Defense"})
            {
                var stage = CloneAsset<StageConfig>("Assets/Arena3Q/Content/Stages/Stage_"+type+"_01.asset",Content+"/Stage_"+type+".asset");
                var so = new SerializedObject(stage);so.FindProperty("stageId").stringValue = "battlefield_"+type.ToLowerInvariant();so.FindProperty("mapBounds").rectValue = _config.Bounds;so.FindProperty("backgroundArt").objectReferenceValue = null;so.FindProperty("decorations").objectReferenceValue = null;so.FindProperty("rivers").arraySize = 0;so.FindProperty("formation").objectReferenceValue = formation;
                var forbidden = so.FindProperty("forbiddenZones"); forbidden.arraySize = Obstacles.Count;
                for (int i=0;i<Obstacles.Count;i++) forbidden.GetArrayElementAtIndex(i).rectValue=Obstacles[i];
                so.FindProperty("playerCastle").FindPropertyRelative("position").vector2Value = new Vector2(-_config.campX,0);
                so.FindProperty("enemyCastle").FindPropertyRelative("position").vector2Value = new Vector2(_config.campX,0);
                so.FindProperty("summonZone").rectValue = new Rect(-_config.campX+4f,-4f,10f,8f);
                var points = so.FindProperty("quickSummonPoints");points.arraySize=5;
                for(int i=0;i<5;i++)points.GetArrayElementAtIndex(i).vector2Value=new Vector2(-_config.campX+6f,(i-2)*1.4f);
                var defenders = so.FindProperty("defenders");
                for(int i=0;i<defenders.arraySize;i++)
                {
                    // Keep the original enemy roster and stats; arrange valid posts in the open court.
                    defenders.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2Value=new Vector2(_config.campX-7f+(i%3)*1.5f,((i/3)%5-2)*1.3f);
                }
                var waves=so.FindProperty("waves");
                for(int i=0;i<waves.arraySize;i++)
                {
                    var spawns=waves.GetArrayElementAtIndex(i).FindPropertyRelative("spawns");
                    if(spawns!=null)for(int j=0;j<spawns.arraySize;j++)
                    {
                        var point=spawns.GetArrayElementAtIndex(j).FindPropertyRelative("spawnPoint");
                        if(point!=null)point.vector2Value=new Vector2(_config.campX-4f,(j%5-2)*1.5f);
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(stage);stages.Add(stage);
            }
            var view = CloneAsset<BattleViewConfig>("Assets/Arena3Q/Content/Resources/BattleView.asset",Content+"/BattleView.asset");
            var viewSo = new SerializedObject(view);viewSo.FindProperty("defaultTiltDegrees").floatValue = 90f-_config.cameraElevation;viewSo.FindProperty("cameraDistance").floatValue = 100f;viewSo.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(view);
            var props=CloneAsset<MapProps3DLibrary>("Assets/Arena3Q/Content/Maps/MapProps3D.asset",Content+"/MapProps3D.asset");
            var propsSo=new SerializedObject(props);var propItems=propsSo.FindProperty("props");
            for(int i=0;i<propItems.arraySize;i++)
            {
                var item=propItems.GetArrayElementAtIndex(i);string id=item.FindPropertyRelative("name").stringValue;
                item.FindPropertyRelative("material").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+id+".mat");
                if(id=="BaguaWideStoneSlab"){var original=AssetDatabase.LoadAssetAtPath<MapProps3DLibrary>("Assets/Arena3Q/Content/Maps/MapProps3D.asset").Props.First(p=>p.name==id);item.FindPropertyRelative("scale").floatValue=original.scale*scale;}
            }
            propsSo.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(props);viewSo.Update();viewSo.FindProperty("mapProps").objectReferenceValue=props;viewSo.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(view);
            battle.Configure(stages.ToArray(),AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat"),true,true);
            var bridge = battle.GetComponent<BattlefieldLegacyBridge>() ?? battle.gameObject.AddComponent<BattlefieldLegacyBridge>();bridge.battle=battle;bridge.battleCamera=camera;bridge.environment=environment;
            bridge.altarCollider=root.Find("BattleCenter/ExistingBaguaAltar").GetComponentInChildren<MeshCollider>();
            var gateState=root.gameObject.AddComponent<BattlefieldGateState>();gateState.battle=battle;
            for(int i=0;i<8;i++)gateState.runes[i]=root.Find("BattleCenter/ExistingOctagonalPillarsAndRunes/Rune "+i).GetComponent<MeshRenderer>();
            var startup = battle.GetComponent<BattlefieldStartup>() ?? battle.gameObject.AddComponent<BattlefieldStartup>();startup.battle=battle;startup.view=view;
            var controller = camera.GetComponent<BattleCameraController>();
            var cameraSo = new SerializedObject(controller);cameraSo.FindProperty("startOrthographicSize").floatValue=environment.battleCamera.orthographicSize;cameraSo.FindProperty("maxOrthographicSize").floatValue=environment.battleCamera.orthographicSize;cameraSo.FindProperty("minOrthographicSize").floatValue=8f;cameraSo.FindProperty("allowMapOverview").boolValue=true;cameraSo.ApplyModifiedPropertiesWithoutUndo();
            var spawnRoot=Group("SpawnPoints",root);
            foreach(var point in stages[0].QuickSummonPoints){var marker=Group("PlayerSpawn",spawnRoot);marker.position=_config.Point(point);}
            Group("Navigation",root);
        }

        [MenuItem("Arena3Q/Battlefield/3 Validate Battlefield")]
        public static void Validate()
        {
            var environment = Object.FindAnyObjectByType<BattlefieldEnvironment>();
            if(environment==null)throw new InvalidOperationException("No battlefield environment in the scene.");
            var failures=new List<string>();
            foreach(var stage in new[]{AssetDatabase.LoadAssetAtPath<StageConfig>(Content+"/Stage_Attack.asset"),AssetDatabase.LoadAssetAtPath<StageConfig>(Content+"/Stage_Defense.asset")})
            {
                stage.Validate(failures);var layout=stage.CreateLayout();
                foreach(var p in stage.QuickSummonPoints)if(!layout.IsWalkable(p)||layout.IsBlocked(p,.45f))failures.Add("Spawn blocked: "+p);
            }
            foreach(var renderer in environment.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
                if(material==null||material.shader==null||material.shader.name.Contains("InternalError"))failures.Add("Invalid material: "+renderer.name);
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence+"/validation.txt",string.Join("\n",failures));
            if(failures.Count>0)throw new InvalidOperationException("Battlefield validation: "+string.Join("; ",failures));
            Debug.Log("[Battlefield] Validation passed: stage data, valid spawn points and shaders.");
        }

        public static void Capture(Camera camera, string name)
        {
            Directory.CreateDirectory(Evidence);var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);rt.antiAliasing=2;
            var previous=camera.targetTexture;var active=RenderTexture.active;var previousPipeline=QualitySettings.renderPipeline;bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try
            {
                camera.targetTexture=rt;camera.aspect=rt.width/(float)rt.height;
                var environment=Object.FindAnyObjectByType<BattlefieldEnvironment>();if(environment!=null&&environment.battleCamera==camera){environment.FitCamera();var quality=environment.GetComponent<BattlefieldQuality>();if(quality!=null)QualitySettings.renderPipeline=quality.Selected;}
                camera.Render();camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Evidence+"/"+name,image.EncodeToPNG());Object.DestroyImmediate(image);
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);ShaderUtil.allowAsyncCompilation=async;QualitySettings.renderPipeline=previousPipeline;}
        }

        private static void BuildQuality(BattlefieldEnvironment environment)
        {
            var controller=environment.gameObject.AddComponent<BattlefieldQuality>();controller.environment=environment;
            for(int i=0;i<3;i++)
            {
                string name=((BattlefieldGraphics)i).ToString();
                var pipeline=CloneAsset<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset",Content+"/URP_"+name+".asset");
                var so=new SerializedObject(pipeline);so.FindProperty("m_DefaultRendererIndex").intValue=1;so.FindProperty("m_SoftShadowsSupported").boolValue=i>0;so.FindProperty("m_MainLightShadowsSupported").boolValue=i>0;
                so.FindProperty("m_ShadowDistance").floatValue=i==2?110f:75f;
                so.FindProperty("m_MainLightShadowmapResolution").intValue=i==2?2048:1024;
                so.FindProperty("m_ShadowCascadeCount").intValue=i==2?2:1;
                so.FindProperty("m_RenderScale").floatValue=i==0?.85f:1f;
                so.FindProperty("m_MSAA").intValue=i==2?2:1;
                so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
                if(i==0)controller.low=pipeline;else if(i==1)controller.medium=pipeline;else controller.high=pipeline;
            }
        }

        private static Transform Group(string name, Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        private static float SmoothRange(float from,float to,float value)=>Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(from,to,value));
        private static GameObject Place(string id,Transform parent,Vector2 p,float yaw=0f,float scale=1f)
        {
            var prefab=_library.Find(id);if(prefab==null)throw new InvalidOperationException("Missing approved prefab: "+id);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(parent,false);go.transform.localPosition=_config.Point(p,_config.TerrainHeight(p));go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one*scale;
            if(id.StartsWith("Rock_Medium"))AddObstacle(go);
            if(parent.name=="Trees")
            {
                var trunk=go.AddComponent<CapsuleCollider>();var b=BoundsOf(go);trunk.radius=.3f;trunk.height=b.size.y/scale*.8f;trunk.center=Vector3.up*trunk.height*.5f;
            }
            return go;
        }
        private static GameObject Pair(string id,Transform parent,Vector2 p,float yaw,float scale){var first=Place(id,parent,p,yaw,scale);Place(id,parent,-p,yaw+180f,scale);return first;}
        private static void AddObstacle(GameObject go)
        {
            var b=BoundsOf(go);var rect=new Rect(b.min.x,b.min.z,b.size.x,b.size.z);
            var bounds=_config.Bounds;rect.xMin=Mathf.Max(rect.xMin,bounds.xMin);rect.xMax=Mathf.Min(rect.xMax,bounds.xMax);rect.yMin=Mathf.Max(rect.yMin,bounds.yMin);rect.yMax=Mathf.Min(rect.yMax,bounds.yMax);
            if(rect.width>.1f&&rect.height>.1f)Obstacles.Add(rect);
            var localBounds=new Bounds(go.transform.InverseTransformPoint(b.center),Vector3.zero);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var rb=renderer.localBounds;var matrix=go.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                for(int corner=0;corner<8;corner++)localBounds.Encapsulate(matrix.MultiplyPoint3x4(new Vector3((corner&1)==0?rb.min.x:rb.max.x,(corner&2)==0?rb.min.y:rb.max.y,(corner&4)==0?rb.min.z:rb.max.z)));
            }
            var collider=go.AddComponent<BoxCollider>();collider.center=localBounds.center;collider.size=localBounds.size;
        }
        private static void Recolour(GameObject go,Color color)
        {
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())
            {
                var list=renderer.sharedMaterials;for(int i=0;i<list.Length;i++)
                {
                    string id=list[i].name+"_Blue";string filename=Art+"/Materials/"+id+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(filename);
                    if(material==null){material=new Material(list[i]);material.name=id;AssetDatabase.CreateAsset(material,filename);}
                    material.CopyPropertiesFromMaterial(list[i]);material.SetColor("_FactionColor",color);material.SetFloat("_FactionMask",1f);material.enableInstancing=true;EditorUtility.SetDirty(material);list[i]=material;
                }renderer.sharedMaterials=list;
            }
        }
        private static Bounds BoundsOf(GameObject go){var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return new Bounds(go.transform.position,Vector3.one);var b=renderers[0].bounds;foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);return b;}
        private static void Ensure(string folder){if(AssetDatabase.IsValidFolder(folder))return;string parent=Path.GetDirectoryName(folder).Replace('\\','/');Ensure(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));}
        private static T Asset<T>(string filename)where T:ScriptableObject{var asset=AssetDatabase.LoadAssetAtPath<T>(filename);if(asset!=null)return asset;asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,filename);return asset;}
        private static T CloneAsset<T>(string source,string target)where T:Object{if(!File.Exists(target)&&!AssetDatabase.CopyAsset(source,target))throw new IOException("Cannot copy "+source);return AssetDatabase.LoadAssetAtPath<T>(target);}
        private static Material Material(string id,Shader shader){string filename=Art+"/Materials/"+id+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(filename);if(material==null){material=new Material(shader){name=id,enableInstancing=true};AssetDatabase.CreateAsset(material,filename);}return material;}
        private static Texture2D Texture(string filename,bool normal,bool ground=false)
        {
            if(string.IsNullOrEmpty(filename))return null;var importer=AssetImporter.GetAtPath(filename)as TextureImporter;if(importer==null)throw new InvalidOperationException("Missing texture: "+filename);
            bool changed=importer.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default)||importer.sRGBTexture==normal||!importer.mipmapEnabled||importer.maxTextureSize!=(ground?2048:1024)||importer.wrapMode!=TextureWrapMode.Repeat||importer.alphaIsTransparency!=(!normal&&!ground);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!normal;importer.mipmapEnabled=true;importer.maxTextureSize=ground?2048:1024;importer.wrapMode=TextureWrapMode.Repeat;importer.alphaIsTransparency=!normal&&!ground;
            if(changed)importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(filename);
        }
        private static Texture2D OwnedTexture(string id,Texture source,bool normal)
        {
            if(source==null)return null;string original=AssetDatabase.GetAssetPath(source);string target=Art+"/Textures/Owned_"+id+Path.GetExtension(original);
            if(!File.Exists(target)){File.Copy(original,target);AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);}
            var result=Texture(target,normal,!normal);var importer=(TextureImporter)AssetImporter.GetAtPath(target);bool changed=importer.isReadable||importer.textureCompression!=TextureImporterCompression.CompressedHQ;
            importer.isReadable=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            foreach(string platform in new[]{"Android","iPhone"})
            {
                var settings=importer.GetPlatformTextureSettings(platform);int maximum=normal?1024:2048;
                if(!settings.overridden||settings.maxTextureSize!=maximum||settings.format!=TextureImporterFormat.ASTC_6x6)changed=true;
                settings.name=platform;settings.overridden=true;settings.maxTextureSize=maximum;settings.format=TextureImporterFormat.ASTC_6x6;settings.compressionQuality=100;importer.SetPlatformTextureSettings(settings);
            }
            if(changed)importer.SaveAndReimport();return result;
        }
        private static Material ParticleMaterial(string id,string source){string filename="Assets/ThirdParty/Kenney/ParticlePack/PNG (Transparent)/"+source+".png";var material=Material("VFX_"+id,Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetTexture("_BaseMap",Texture(filename,false));material.SetFloat("_Surface",1f);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0f);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;return material;}
        private static Mesh Quad(){string filename=Art+"/Meshes/RuneQuad.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(filename);if(mesh!=null)return mesh;mesh=new Mesh{name="RuneQuad",vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one},triangles=new[]{0,2,1,1,2,3}};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,filename);return mesh;}
    }
}
