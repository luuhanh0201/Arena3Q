using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arena3Q.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Arena3Q.Editor.Chapter01
{
    /// <summary>
    /// Chapter 1 (Loạn Khăn Vàng) 3D colour reference: renders the 3D candidates of each asset group from the
    /// user's sheet one by one under the release battlefield light, grading and 50° / 45° camera, so the best
    /// candidate per group can be picked before the colour reference scene is built. Raw models stay in the
    /// git-ignored Assets/ThirdParty/Chapter01.
    /// </summary>
    public static class Chapter01ColorReference
    {
        private const string ThirdParty = "Assets/ThirdParty/Chapter01";
        private const string LibraryPath = "Assets/Arena3Q/Content/Battlefield/BattlefieldAssetLibrary.asset";
        private const string ConfigPath = "Assets/Arena3Q/Content/Battlefield/BattlefieldConfig.asset";
        private const string VolumePath = "Assets/Arena3Q/Content/Battlefield/EveningVolume.asset";
        private const string PipelinePath = "Assets/Arena3Q/Content/Battlefield/URP_High.asset";
        private const string Out = "ArtSource/Chapter01/3D/Renders/Candidates";
        private const int Tile = 720;
        private const string ScenePath = "Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Scenes/CH01_ColorReference.unity";
        private const string SceneRenders = "ArtSource/Chapter01/3D/Renders/Reference";
        private static string _materialFolder;

        private sealed class Candidate
        {
            public string group, label, source, texture;
            public float height;
            public Candidate(string group, string label, string source, float height, string texture = null)
            { this.group = group; this.label = label; this.source = source; this.height = height; this.texture = texture; }
        }

        // source: "lib:<id>" = BattlefieldAssetLibrary entry already in the game, otherwise a path under ThirdParty.
        // texture: albedo that replaces the imported one (shared palette atlases, colour variants).
        // Heights follow the game's own asset of the group (library models keep their native scale);
        // a negative height normalises the footprint width instead (flat altars).
        private static readonly Candidate[] Candidates =
        {
            new("house", "CS Studio Bookstore V1", "CSStudio/bookstore/Bookstore.fbx", 5f, "CSStudio/bookstore/V1.png"),
            new("house", "CS Studio Bookstore V2", "CSStudio/bookstore/Bookstore.fbx", 5f, "CSStudio/bookstore/V2.png"),
            new("house", "CS Studio Bookstore V3", "CSStudio/bookstore/Bookstore.fbx", 5f, "CSStudio/bookstore/V3.png"),
            new("house", "CS Studio Butcher", "CSStudio/butcher/Butcher.fbx", 5f, "CSStudio/butcher/V1.png"),
            new("house", "CS Studio Clothing", "CSStudio/clothing/Clothing_Store.fbx", 5f, "CSStudio/clothing/V1.png"),
            new("house", "KayKit Home A", "KayKitHexagon/building_home_A_red.fbx", 5f, "KayKitHexagon/hexagons_medieval.png"),
            new("pavilion", "CS Studio Gazebo", "CSStudio/gazeebo/Gazeebo.fbx", 4.5f, "CSStudio/gazeebo/V1.png"),
            new("pavilion", "Four-corner pavilion", "Pavilion/FourCornerPavilion.fbx", 4.5f),
            new("pavilion", "Quaternius Gazebo", "QuaterniusVillage/Gazebo.fbx", 4.5f),
            new("granary", "CS Studio Horse room", "CSStudio/horse_room/Horse_room.fbx", 4.5f, "CSStudio/horse_room/V1.png"),
            new("granary", "KayKit Market", "KayKitHexagon/building_market_red.fbx", 4.5f, "KayKitHexagon/hexagons_medieval.png"),
            new("granary", "Farm Tool Shed", "FarmMarket/Tool Shed.fbx", 4.5f),
            new("cart", "Quaternius Cart", "QuaterniusVillage/Cart.fbx", 1.6f),
            new("cart", "MegaKit Wagon", "MegaKit/Prop_Wagon.fbx", 1.6f, "MegaKit/T_WoodTrim_BaseColor.png"),
            new("cart", "Farm Wheelbarrow", "FarmMarket/Wheelbarrow.fbx", 1.1f),
            new("sacks", "Quaternius Bags", "QuaterniusVillage/Bags.fbx", .8f),
            new("sacks", "Quaternius Hay", "QuaterniusVillage/Hay.fbx", .9f),
            new("sacks", "Farm Burlap Sack", "FarmMarket/Burlap Sack.fbx", .8f),
            new("sacks", "Farm Hay Bale", "FarmMarket/Hay Bale.fbx", .9f),
            new("tower", "Game Watchtower", "lib:WoodenCampWatchtower", 4f),
            new("tower", "KayKit Tower A", "KayKitHexagon/building_tower_A_red.fbx", 4.5f, "KayKitHexagon/hexagons_medieval.png"),
            new("tower", "Kenney Tower roof", "KenneyCastle/tower-square-top-roof-high.fbx", 4.5f, "KenneyCastle/Textures/colormap.png"),
            new("tent", "Game Command tent", "lib:ThreeKingdomsCommand_1", 2.6f),
            new("tent", "Kenney Tent open", "KenneyNature/tent_detailedOpen.fbx", 2.6f),
            new("tree", "Game CommonTree 1", "lib:CommonTree_1", 7.5f),
            new("tree", "UN Willow 1", "UltimateNature/Willow_1.fbx", 7.5f),
            new("tree", "UN Autumn tree 1", "UltimateNature/CommonTree_Autumn_1.fbx", 7.5f),
            new("tree", "KayKit Tree 1A", "KayKitForest/Tree_1_A_Color1.fbx", 7.5f, "KayKitForest/forest_texture.png"),
            new("rock", "Game Rock Medium 1", "lib:Rock_Medium_1", 2.1f),
            new("rock", "UN Moss rock 1", "UltimateNature/Rock_Moss_1.fbx", 2.1f),
            new("rock", "KayKit Rock 1A", "KayKitForest/Rock_1_A_Color1.fbx", 2.1f, "KayKitForest/forest_texture.png"),
            new("rock", "Quaternius Rock 1", "QuaterniusVillage/Rock_1.fbx", 2.1f),
            new("altar", "Game Round altar", "lib:BaguaRoundStoneAltar", -4f),
            new("altar", "Game Stone pillar", "lib:BaguaStonePillar", 2.2f),
            new("altar", "KayKit Stage A", "KayKitHexagon/building_stage_A.fbx", -4f, "KayKitHexagon/hexagons_medieval.png"),
            new("well", "Quaternius Well", "QuaterniusVillage/Well.fbx", 2.2f),
            new("well", "KayKit Well", "KayKitHexagon/building_well_red.fbx", 2.2f, "KayKitHexagon/hexagons_medieval.png"),
            new("fence", "Game Palisade", "lib:WoodenPalisadeFence", 1.6f),
            new("fence", "Kenney Wood fence wall", "KenneyCastle/wall-narrow-wood-fence.fbx", 1.6f, "KenneyCastle/Textures/colormap.png"),
            new("fence", "KayKit Wood fence", "KayKitHexagon/fence_wood_straight.fbx", 1.6f, "KayKitHexagon/hexagons_medieval.png"),
        };

        /// <summary>
        /// The project is a 2D template, so new PNGs import as Sprites (clamped, no mipmaps, alpha as transparency):
        /// tiled ground then smears into stripes and model atlases shimmer. Model and ground textures here are plain
        /// repeating textures with mipmaps, at the mobile size.
        /// </summary>
        private static void PrepareTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ThirdParty }))
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is not TextureImporter ti) continue;
                if (ti.textureType == TextureImporterType.Default && ti.wrapMode == TextureWrapMode.Repeat && ti.mipmapEnabled && !ti.alphaIsTransparency && ti.maxTextureSize == 1024) continue;
                ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true;
                ti.alphaIsTransparency = false; ti.sRGBTexture = true; ti.maxTextureSize = 1024; ti.anisoLevel = 2;
                ti.SaveAndReimport();
            }
        }

        // Sources the catalog lists as downloaded but no render had checked yet (map resource check).
        private static readonly Candidate[] GapChecks =
        {
            new("bamboo", "Nobiax Bamboo 01", "Gap/Bamboo01.fbx", 7f, "Gap/Bamboo01_diffuse.tga"),
            new("bridge", "Kenney Bridge wood", "Gap/bridge_wood.fbx", -4f),
            new("cliff", "Kenney Cliff large", "Gap/cliff_large_rock.fbx", 4f),
            new("cliff", "Kenney Cliff steps", "Gap/cliff_steps_rock.fbx", 2f),
            new("statue", "Kenney Statue block", "Gap/statue_block.fbx", 2.5f),
            new("gate", "Farm Wooden gate", "Gap/Wooden Farm Gate.fbx", 2f),
            new("cage", "Quaternius Cage small", "Assets/ThirdParty/Quaternius/FantasyPropsStandard/Exports/FBX/Cage_Small.fbx", 1.6f),
            new("props", "Quaternius Weapon stand", "Assets/ThirdParty/Quaternius/FantasyPropsStandard/Exports/FBX/WeaponStand.fbx", 1.6f),
            new("props", "Quaternius Cauldron", "Assets/ThirdParty/Quaternius/FantasyPropsStandard/Exports/FBX/Cauldron.fbx", 1f),
            new("npc", "Quaternius Male peasant", "Gap/Male_Peasant.fbx", 1.8f, "Gap/T_Peasant_BaseColor.png"),
            new("npc", "KayKit Knight", "Gap/Knight.fbx", 1.8f, "Gap/knight_texture.png"),
        };

        [MenuItem("Arena3Q/Chapter 01/Render 3D Candidates")]
        public static void RenderCandidates() => Render(Candidates, Out);

        [MenuItem("Arena3Q/Chapter 01/Render Map Resource Check")]
        public static void RenderGapChecks() => Render(GapChecks, "ArtSource/Chapter01/3D/Renders/GapCheck");

        private static string ModelPath(string source) => source.StartsWith("Assets/") ? source : ThirdParty + "/" + source;

        private static void Render(Candidate[] candidates, string outDir)
        {
            PrepareTextures();
            var library = AssetDatabase.LoadAssetAtPath<BattlefieldAssetLibrary>(LibraryPath);
            var config = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(ConfigPath);
            Directory.CreateDirectory(outDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = Stage(config, out _);
            var report = new StringBuilder("group\tlabel\tsource\ttriangles\tmaterials\tnotes\n");
            var index = new StringBuilder("[");
            var extent = new Dictionary<string, float>();
            foreach (var c in candidates)
            {
                var probe = Spawn(c, library, out _, out _, out _); if (probe == null) continue;
                extent.TryGetValue(c.group, out float e); extent[c.group] = Mathf.Max(e, Projected(camera, probe));
                Object.DestroyImmediate(probe);
            }
            foreach (var c in candidates)
            {
                var instance = Spawn(c, library, out string notes, out int triangles, out int materials);
                string file = instance != null ? c.group + "_" + Slug(c.label) + ".png" : "";
                if (instance != null)
                {
                    // Same scale for every candidate of a group: frame the group's largest model, not each model.
                    Frame(camera, extent[c.group] * 1.12f);
                    Capture(camera, outDir + "/" + file, Tile, Tile);
                    Object.DestroyImmediate(instance);
                }
                report.Append($"{c.group}\t{c.label}\t{c.source}\t{triangles}\t{materials}\t{notes}\n");
                index.Append($"{(index.Length > 1 ? "," : "")}\n{{\"group\":\"{c.group}\",\"label\":\"{c.label}\",\"file\":\"{file}\",\"triangles\":{triangles},\"notes\":\"{notes.Replace("\"", "'")}\"}}");
            }
            File.WriteAllText(outDir + "/report.tsv", report.ToString());
            File.WriteAllText(outDir + "/index.json", index + "\n]\n");
            Debug.Log("[Chapter01] Rendered " + candidates.Length + " candidates to " + outDir);
        }

        /// <summary>Release battlefield light, ambient, grading and camera (BattlefieldEnvironment.ApplyLighting / FitCamera).</summary>
        private static Camera Stage(BattlefieldConfig config, out GameObject ground)
        {
            var sun = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowBias = .025f; sun.shadowNormalBias = .25f;
            sun.color = config.sunColor; sun.intensity = config.sunIntensity; sun.transform.rotation = Quaternion.Euler(config.sunEuler);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.5f, .59f, .7f);
            RenderSettings.ambientEquatorColor = new Color(.5f, .46f, .36f);
            RenderSettings.ambientGroundColor = new Color(.24f, .25f, .23f);
            RenderSettings.fog = false; RenderSettings.skybox = null;
            var volume = new GameObject("Evening grading", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true; volume.priority = 15f; volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);

            ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "Ground earth";
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            var earth = Lit(Load<Texture2D>(ThirdParty + "/_Local2D/01_ground-earth.png"), Color.white);
            earth.SetTextureScale("_BaseMap", new Vector2(20f, 20f));
            ground.GetComponent<MeshRenderer>().sharedMaterial = earth;

            var camera = new GameObject("Battle camera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.3f, .3f, .3f);
            camera.transform.rotation = Quaternion.Euler(config.cameraElevation, config.cameraYaw, 0f);
            camera.nearClipPlane = .3f; camera.farClipPlane = 240f;
            var data = camera.GetUniversalAdditionalCameraData(); data.SetRenderer(1); data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return camera;
        }

        private static void Frame(Camera camera, float halfSize)
        {
            camera.orthographicSize = halfSize;
            camera.transform.position = Vector3.up * halfSize * .45f - camera.transform.forward * 100f;
        }

        /// <summary>Half size on screen of a model standing at the origin, seen by the battle camera.</summary>
        private static float Projected(Camera camera, GameObject instance)
        {
            var b = Bounds(Visible(instance)); float half = 0f;
            var inverse = Quaternion.Inverse(camera.transform.rotation);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                var local = inverse * (corner - Vector3.up * b.size.y * .3f);
                half = Mathf.Max(half, Mathf.Abs(local.y), Mathf.Abs(local.x));
            }
            return half;
        }

        /// <summary>Instantiates a candidate standing on the ground at the origin, scaled to the group height.</summary>
        private static GameObject Spawn(Candidate c, BattlefieldAssetLibrary library, out string notes, out int triangles, out int materialCount)
        {
            notes = ""; triangles = 0; materialCount = 0;
            GameObject source;
            bool fromLibrary = c.source.StartsWith("lib:");
            if (fromLibrary) source = library.Find(c.source.Substring(4));
            else
            {
                string path = ModelPath(c.source);
                if (AssetImporter.GetAtPath(path) is ModelImporter importer && importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
                { importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; importer.SaveAndReimport(); }
                source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            if (source == null) { notes = "missing"; return null; }
            var instance = (GameObject)Object.Instantiate(source);
            instance.transform.position = Vector3.zero;
            foreach (var particle in instance.GetComponentsInChildren<ParticleSystem>(true)) Object.DestroyImmediate(particle.gameObject);
            var renderers = Visible(instance);
            var fallback = c.texture != null ? Load<Texture2D>(ThirdParty + "/" + c.texture) : null;
            var converted = new Dictionary<Material, Material>();
            var shaders = new HashSet<string>();
            foreach (var renderer in renderers)
            {
                if (renderer is MeshRenderer) triangles += Triangles(renderer.GetComponent<MeshFilter>()?.sharedMesh);
                else if (renderer is SkinnedMeshRenderer skinned) triangles += Triangles(skinned.sharedMesh);
                renderer.shadowCastingMode = ShadowCastingMode.On;
                if (fromLibrary) { foreach (var m in renderer.sharedMaterials) if (m != null) shaders.Add(m.shader.name); continue; }
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var m = materials[i]; if (m == null) continue;
                    if (!converted.TryGetValue(m, out var lit)) { converted[m] = lit = Convert(m, fallback ?? ByName(c.source, m.name), out string note); if (note != null) shaders.Add(note); }
                    materials[i] = lit;
                }
                renderer.sharedMaterials = materials;
            }
            materialCount = fromLibrary ? renderers.Sum(r => r.sharedMaterials.Length) : converted.Count;
            notes = string.Join(", ", shaders);
            var bounds = Bounds(renderers);
            if (bounds.size.y < 1e-4f) { notes += " empty bounds"; return instance; }
            float scale = fromLibrary ? 1f : c.height > 0f ? c.height / bounds.size.y : -c.height / Mathf.Max(bounds.size.x, bounds.size.z);
            instance.transform.localScale *= scale;
            bounds = Bounds(renderers);
            instance.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            notes += $" size {bounds.size.x:0.0}x{bounds.size.y:0.0}x{bounds.size.z:0.0} m, scale {scale:0.###}";
            return instance;
        }

        /// <summary>URP Lit copy of an imported material: keeps its colour and albedo, matte like the battlefield props.</summary>
        private static Material Convert(Material source, Texture2D fallback, out string note)
        {
            note = null;
            Texture texture = null; Color colour = Color.white;
            foreach (var name in new[] { "_BaseMap", "_MainTex" }) if (texture == null && source.HasProperty(name)) texture = source.GetTexture(name);
            if (source.HasProperty("_BaseColor")) colour = source.GetColor("_BaseColor"); else if (source.HasProperty("_Color")) colour = source.GetColor("_Color");
            // Given textures win: CS Studio V1..V3 are colour variants of one palette atlas.
            if (fallback != null) { texture = fallback; colour = Color.white; note = "texture " + fallback.name; }
            if (texture == null) note = "colour only " + source.name;
            var lit = Lit(texture as Texture2D, colour); lit.name = source.name;
            if (_materialFolder != null) AssetDatabase.CreateAsset(lit, AssetDatabase.GenerateUniqueAssetPath(_materialFolder + "/" + Slug(source.name) + ".mat"));
            return lit;
        }

        /// <summary>Albedo next to the model whose file name starts with the material name (Four-corner pavilion).</summary>
        private static Texture2D ByName(string source, string material)
        {
            string folder = Path.GetDirectoryName(ModelPath(source)).Replace('\\', '/');
            string key = material.Split(' ', '.')[0];
            if (key.Length == 0) return null;
            foreach (var file in Directory.GetFiles(folder, "*.png"))
                if (Path.GetFileNameWithoutExtension(file).StartsWith(key)) return AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + Path.GetFileName(file));
            return null;
        }

        private static Material Lit(Texture2D texture, Color colour)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (texture != null) m.SetTexture("_BaseMap", texture);
            m.SetColor("_BaseColor", colour); m.SetFloat("_Smoothness", .12f); m.SetFloat("_Metallic", 0f);
            return m;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogWarning("[Chapter01] Missing " + path);
            return asset;
        }

        /// <summary>Renderers the battle camera draws up close: enabled ones, LOD 0 only.</summary>
        private static Renderer[] Visible(GameObject instance)
        {
            var hidden = new HashSet<Renderer>();
            foreach (var group in instance.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++) foreach (var r in lods[i].renderers) if (r != null) { hidden.Add(r); r.enabled = false; }
                group.enabled = false;
            }
            return instance.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled && !hidden.Contains(r)).ToArray();
        }

        private static Bounds Bounds(Renderer[] renderers)
        {
            var b = new Bounds(); bool any = false;
            foreach (var r in renderers) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return b;
        }

        private static int Triangles(Mesh mesh) => mesh == null ? 0 : Enumerable.Range(0, mesh.subMeshCount).Sum(i => (int)mesh.GetIndexCount(i)) / 3;

        private static string Slug(string label) => new string(label.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        /// <summary>Picked candidate of each group (Candidates/README.md), placed by the reference scene layout.</summary>
        private static readonly (Candidate model, Vector3 position, float yaw)[] Layout =
        {
            (new("house", "Bookstore V3", "CSStudio/bookstore/Bookstore.fbx", 5f, "CSStudio/bookstore/V3.png"), new(-7f, 0f, 6f), 0f),
            (new("house", "Butcher V2", "CSStudio/butcher/Butcher.fbx", 4.5f, "CSStudio/butcher/V2.png"), new(3.5f, 0f, 8.5f), 90f),
            (new("pavilion", "Gazebo V1", "CSStudio/gazeebo/Gazeebo.fbx", 4f, "CSStudio/gazeebo/V1.png"), new(12f, 0f, 2f), 0f),
            (new("granary", "Horse room V3", "CSStudio/horse_room/Horse_room.fbx", 4.5f, "CSStudio/horse_room/V3.png"), new(9f, 0f, -8f), 0f),
            (new("cart", "MegaKit Wagon", "MegaKit/Prop_Wagon.fbx", 1.4f, "MegaKit/T_WoodTrim_BaseColor.png"), new(4f, 0f, -4f), 30f),
            (new("sacks", "Bags", "QuaterniusVillage/Bags.fbx", .7f), new(6f, 0f, -2.8f), 10f),
            (new("well", "Quaternius Well", "QuaterniusVillage/Well.fbx", 2f), new(-1f, 0f, 3.8f), 0f),
            (new("tower", "Watchtower", "lib:WoodenCampWatchtower", 0f), new(-10f, 0f, -8f), 0f),
            (new("tent", "Command tent", "lib:ThreeKingdomsCommand_1", 0f), new(-4f, 0f, -9.5f), 0f),
            (new("fence", "Palisade 1", "lib:WoodenPalisadeFence", 0f), new(-12f, 0f, -12.5f), 0f),
            (new("fence", "Palisade 2", "lib:WoodenPalisadeFence", 0f), new(-7.6f, 0f, -12.5f), 0f),
            (new("fence", "Palisade 3", "lib:WoodenPalisadeFence", 0f), new(-3.2f, 0f, -12.5f), 0f),
            (new("tree", "Tree 1", "lib:CommonTree_1", 0f), new(-13f, 0f, 1f), 0f),
            (new("tree", "Tree 2", "lib:CommonTree_2", 0f), new(-2f, 0f, 12f), 70f),
            (new("tree", "Tree 3", "lib:CommonTree_3", 0f), new(15f, 0f, -4f), 140f),
            (new("tree", "Tree 4", "lib:CommonTree_1", 0f), new(-12.5f, 0f, 9f), 200f),
            (new("rock", "Rock 1", "lib:Rock_Medium_1", 0f), new(-15f, 0f, -3f), 0f),
            (new("rock", "Rock 2", "lib:Rock_Medium_2", 0f), new(13.5f, 0f, -6f), 60f),
            (new("altar", "Round altar", "lib:BaguaRoundStoneAltar", 0f), new(14f, 0f, 13f), 0f),
            (new("altar", "Pillar 1", "lib:BaguaStonePillar", 0f), new(10.5f, 0f, 16.5f), 0f),
            (new("altar", "Pillar 2", "lib:BaguaStonePillar", 0f), new(17.5f, 0f, 9.5f), 90f),
        };

        /// <summary>Ground of the reference scene: Codex's Chapter 1 earth, road and grass tiles.</summary>
        private static void Ground(Transform root)
        {
            void Patch(string name, string texture, Vector3 centre, Vector2 size, float lift, float tile)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(centre + Vector3.up * lift, Quaternion.Euler(90f, 0f, 0f));
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
                var m = Lit(Load<Texture2D>(ThirdParty + "/_Local2D/" + texture), Color.white);
                m.SetTextureScale("_BaseMap", size / tile);
                AssetDatabase.CreateAsset(m, _materialFolder + "/Ground_" + Slug(name) + ".mat");
                go.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            Patch("Earth", "01_ground-earth.png", Vector3.zero, new Vector2(90f, 90f), 0f, 4f);
            Patch("Grass west", "03_ground-grass.png", new Vector3(-11f, 0f, 5f), new Vector2(9f, 14f), .01f, 4f);
            Patch("Grass east", "03_ground-grass.png", new Vector3(14f, 0f, -4f), new Vector2(7f, 9f), .01f, 4f);
            Patch("Dirt road", "02_ground-path.png", Vector3.zero, new Vector2(40f, 3.2f), .02f, 3.2f);
            Patch("Road to altar", "02_ground-path.png", new Vector3(11f, 0f, 7f), new Vector2(3f, 14f), .02f, 3f);
        }

        [MenuItem("Arena3Q/Chapter 01/Build Colour Reference Scene")]
        public static void BuildReferenceScene()
        {
            PrepareTextures();
            var library = AssetDatabase.LoadAssetAtPath<BattlefieldAssetLibrary>(LibraryPath);
            var config = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(ConfigPath);
            _materialFolder = ThirdParty + "/_Materials";
            if (AssetDatabase.IsValidFolder(_materialFolder)) AssetDatabase.DeleteAsset(_materialFolder);
            AssetDatabase.CreateFolder(ThirdParty, "_Materials");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)); Directory.CreateDirectory(SceneRenders);
            AssetDatabase.Refresh();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = Stage(config, out var stageGround); Object.DestroyImmediate(stageGround);
                var root = new GameObject("Chapter 01 colour reference").transform;
                Ground(root);
                var placed = new Dictionary<string, Bounds>();
                foreach (var (model, position, yaw) in Layout)
                {
                    var instance = Spawn(model, library, out _, out _, out _);
                    if (instance == null) { Debug.LogWarning("[Chapter01] Missing " + model.source); continue; }
                    instance.name = model.label; instance.transform.SetParent(root, true);
                    instance.transform.RotateAround(Vector3.zero, Vector3.up, yaw); instance.transform.position += position;
                    var b = Bounds(Visible(instance));
                    if (placed.TryGetValue(model.group, out var g)) g.Encapsulate(b); else g = b;
                    placed[model.group] = g;
                }
                EditorSceneManager.SaveScene(scene, ScenePath);
                // Chuong1.md section 2 step 1 overview and the section 8 pairs, at the battle camera and exposure.
                Shot(camera, "00_overview", new Vector3(1f, 0f, 1f), 17f);
                Shot(camera, "01_house_tree", Between(placed["house"], placed["tree"]), 9f);
                Shot(camera, "02_house_dirt_road", new Vector3(-4f, 0f, 3f), 8f);
                Shot(camera, "03_granary_grain_cart", Between(placed["granary"], placed["cart"]), 7f);
                Shot(camera, "04_camp_watchtower", Between(placed["tower"], placed["tent"]), 7.5f);
                Shot(camera, "05_altar_main_light", placed["altar"].center, 7f);
                Debug.Log("[Chapter01] Colour reference scene saved to " + ScenePath + ", renders in " + SceneRenders);
            }
            finally { _materialFolder = null; }
        }

        private static Vector3 Between(Bounds a, Bounds b) { var c = (a.center + b.center) * .5f; c.y = 0f; return c; }

        private static void Shot(Camera camera, string name, Vector3 focus, float halfSize)
        {
            camera.orthographicSize = halfSize;
            camera.transform.position = focus + Vector3.up * 1.2f - camera.transform.forward * 100f;
            Capture(camera, SceneRenders + "/" + name + ".png", 1920, 1080);
        }

        public static void Capture(Camera camera, string path, int width, int height)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var previousTarget = camera.targetTexture; var active = RenderTexture.active; var pipeline = QualitySettings.renderPipeline;
            bool async = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
            try
            {
                QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
                camera.targetTexture = rt; camera.aspect = width / (float)height;
                camera.Render(); camera.Render(); RenderTexture.active = rt;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG()); Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = active; rt.Release(); Object.DestroyImmediate(rt);
                ShaderUtil.allowAsyncCompilation = async; QualitySettings.renderPipeline = pipeline;
            }
        }
    }
}
