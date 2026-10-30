using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Arena3Q.Exploration.Editor
{
    public static class Chapter01SystemsBuilder
    {
        public const string Root = "Assets/Arena3Q/Chapters/Chapter01_YellowTurban";
        public const string ConfigPath = Root + "/Data/CH01_MapConfig.asset";
        public const string ScenePath = Root + "/Scenes/CH01_Exploration.unity";
        public const string PrefabPath = Root + "/Prefabs/CH01_MapSystems.prefab";
        private const string NavigationRoot = "Assets/ThirdParty/Chapter01/_Navigation";
        [MenuItem("Arena3Q/Chapter 01/Update World Presentation")]
        public static void UpdateWorldPresentation()
        {
            var config=AssetDatabase.LoadAssetAtPath<ChapterMapConfig>(ConfigPath);
            string texture=Root+"/Data/CH01_FogNoise.png";
            var motion=config.motion;
            var image=new Texture2D(motion.fogNoiseSize,motion.fogNoiseSize,TextureFormat.RGB24,false,true);
            float Hash(int x,int y,int cells,int octave)
            {
                unchecked { uint n=(uint)((x%cells+cells)%cells)*374761393u+(uint)((y%cells+cells)%cells)*668265263u+(uint)(motion.fogNoiseSeed+octave)*2246822519u;
                    n=(n^(n>>13))*1274126177u;return (n^(n>>16))/(float)uint.MaxValue; }
            }
            for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++)
            {
                float value=0,total=0,weight=1;
                for(int octave=0;octave<motion.fogNoiseOctaves;octave++)
                {
                    int cells=motion.fogNoiseCells<<octave;
                    float px=(x+.5f)/image.width*cells,py=(y+.5f)/image.height*cells;
                    int ix=Mathf.FloorToInt(px),iy=Mathf.FloorToInt(py);
                    float u=Mathf.SmoothStep(0,1,px-ix),v=Mathf.SmoothStep(0,1,py-iy);
                    value+=weight*Mathf.Lerp(Mathf.Lerp(Hash(ix,iy,cells,octave),Hash(ix+1,iy,cells,octave),u),Mathf.Lerp(Hash(ix,iy+1,cells,octave),Hash(ix+1,iy+1,cells,octave),u),v);
                    total+=weight;weight*=.5f;
                }
                float n=value/total;image.SetPixel(x,y,new Color(n,n,n,1));
            }
            image.Apply();File.WriteAllBytes(texture,image.EncodeToPNG());Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(texture,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texture);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.mipmapEnabled=true;
            importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            config.motion.fogTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
            string path=Root+"/Data/CH01_Fog.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Arena3Q/Chapter Fog"));AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_CloudTex",config.motion.fogTexture);
            config.motion.fogMaterial=material;
            EditorUtility.SetDirty(material); EditorUtility.SetDirty(config.motion); AssetDatabase.SaveAssets();
            Debug.Log("[ChapterMap] World presentation bound; source scene/model/materials unchanged.");
        }
        [Serializable] private sealed class RoadLayout { public RoadLine road; }
        [Serializable] private sealed class RoadLine { public float[] points; }
        [MenuItem("Arena3Q/Chapter 01/Update Map Presentation")]
        public static void UpdatePresentation()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var config = AssetDatabase.LoadAssetAtPath<ChapterMapConfig>(ConfigPath);
            var layout = JsonUtility.FromJson<RoadLayout>(File.ReadAllText(Root + "/Data/CH01_MapLayout.json"));
            var raw = layout?.road?.points;
            if (raw == null || raw.Length < 4 || raw.Length % 2 != 0) throw new InvalidDataException("Road requires XZ pairs.");
            var points = new List<Vector3>();
            for (int i = 0; i < raw.Length; i += 2) points.Add(new Vector3(raw[i], 0, raw[i + 1]));
            var floors = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => !c.isTrigger &&
                (c.name == "Terrain" || c.GetComponentsInParent<Transform>().Any(t =>
                    config.walkableNames.Any(n => t.name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0)))).ToArray();
            var route = new List<Vector3>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                var a = points[Mathf.Max(0, i - 1)]; var b = points[i];
                var c = points[i + 1]; var d = points[Mathf.Min(points.Count - 1, i + 2)];
                for (int k = 0; k < Mathf.Max(1, config.ui.routeCurveSteps); k++)
                {
                    float t = k / (float)Mathf.Max(1, config.ui.routeCurveSteps), t2 = t * t, t3 = t2 * t;
                    route.Add(.5f * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3));
                }
            }
            route.Add(points[points.Count - 1]);
            for (int i = 0; i < route.Count; i++)
            {
                var point = route[i]; float height = float.MinValue;
                var ray = new Ray(point + Vector3.up * config.ui.routeRayHeight, Vector3.down);
                foreach (var floor in floors)
                    if (floor.Raycast(ray, out var hit, config.ui.routeRayHeight * 2)) height = Mathf.Max(height, hit.point.y);
                if (height == float.MinValue) throw new InvalidDataException("Road is missing a ground surface: " + point);
                point.y = height; route[i] = point;
            }
            config.roadRoute = route.ToArray();
            string materialPath = Root + "/Data/CH01_RoadGuide.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Arena3Q/Chapter Road Guide");
                if (shader == null) throw new InvalidOperationException("Road guide shader is missing.");
                material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath);
            }
            config.ui.routeMaterial = material;
            EditorUtility.SetDirty(config); EditorUtility.SetDirty(config.ui); AssetDatabase.SaveAssets();
            Debug.Log($"[ChapterMap] Updated presentation with {route.Count} ground samples and {floors.Length} floor colliders. Source scene unchanged.");
        }
        public static void AuditNavigation()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var config=AssetDatabase.LoadAssetAtPath<ChapterMapConfig>(ConfigPath);
            var nav=NavMesh.AddNavMeshData(config.navigationByUnlockedStage[1]);
            var path=new NavMeshPath();NavMesh.SamplePosition(config.initialPosition,out var start,3,NavMesh.AllAreas);
            NavMesh.SamplePosition(config.stages[1].targets[0].position-Vector3.up*2.4f,out var end,3,NavMesh.AllAreas);
            NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path);
            var audit=new NavigationAudit{vertices=NavMesh.CalculateTriangulation().vertices,indices=NavMesh.CalculateTriangulation().indices,corners=path.corners};
            foreach(var root in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="bridge"))
            {
                foreach(var mesh in root.GetComponentsInChildren<MeshFilter>())Debug.Log("[NavigationBridge] "+ChapterMapController.Path(mesh.transform)+" "+mesh.GetComponent<Renderer>().bounds+" local="+mesh.sharedMesh.bounds);
            }
            Directory.CreateDirectory("ArtSource/Chapter01/Navigation");File.WriteAllText("ArtSource/Chapter01/Navigation/audit.json",JsonUtility.ToJson(audit));nav.Remove();
            Debug.Log("[NavigationAudit] "+path.status+" "+string.Join("; ",path.corners.Select(v=>v.ToString())));
        }
        [Serializable] private class NavigationAudit{public Vector3[] vertices,corners;public int[] indices;}
        public static void BuildIntegrated()
        {
            Build();
            Arena3Q.Editor.Chapter01.Chapter01MapBuilder.Build();
            CurrentGameRelease.Apply();
        }
        [MenuItem("Arena3Q/Chapter 01/Build Map Systems")]
        public static void Build()
        {
            foreach (var folder in new[] { Root+"/Prefabs",Root+"/Data",NavigationRoot }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            foreach(var oldSystems in Object.FindObjectsByType<ChapterMapController>(FindObjectsSortMode.None))Object.DestroyImmediate(oldSystems.gameObject);
            var config=LoadOrCreate<ChapterMapConfig>(ConfigPath);
            config.frontend=AssetDatabase.LoadAssetAtPath<FrontendConfig>("Assets/Arena3Q/Content/Frontend/FrontendConfig.asset");
            config.ui=LoadOrCreate<MapUiConfig>(Root+"/Data/CH01_MapUi.asset");
            config.motion=LoadOrCreate<MapMotionConfig>(Root+"/Data/CH01_MapMotion.asset");
            config.introduction="Năm 184, khởi nghĩa Khăn Vàng lan rộng.\nQuân triều đình và các nghĩa binh cùng nhau dẹp loạn,\ngiải cứu dân chúng, ngăn chặn âm mưu của Trương Giác.";
            ConfigureIcons(config.ui);
            var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            string[] names={"Làng Bình An","Đồng Lúa","Rừng Trúc","Trại Lương","Tiền Doanh","Đại Doanh Hoàng Cân","Tế Đàn Hoàng Cân"};
            string[] objectives={"Giải cứu dân làng bị quân Khăn Vàng giam giữ.","Bảo vệ kho lương và trinh sát đường vận chuyển.","Phá trại tiền tiêu trong rừng trúc.","Giải cứu dân và chiếm lại kho lương.","Đánh bại Trình Viễn Chí.","Đột phá cổng, quân tinh nhuệ và hộ pháp.","Ngăn chặn nghi lễ của Trương Giác."};
            var existing=config.stages??Array.Empty<ChapterMapStage>();var stages=new List<ChapterMapStage>();
            for(int i=0;i<names.Length;i++)
            {
                var marker=transforms.Single(t=>t.name==$"StageMarker_{i+1:00}");var area=marker.parent;string id=$"ch01_{i+1:00}";
                var old=existing.FirstOrDefault(s=>s!=null&&s.id==id);
                var anchors=area.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("MarkerAnchor_",StringComparison.Ordinal)).ToArray();
                stages.Add(new ChapterMapStage { id=id,areaName=names[i],areaPath=ChapterMapController.Path(area),markerPath=ChapterMapController.Path(marker),
                    center=marker.position,regionRadius=16,boss=i==4||i==6,bossName=i==4?"Trình Viễn Chí":i==6?"Trương Giác":"",objective=objectives[i],battle=old?.battle,
                    targets=anchors.Select((a,index)=>new ChapterMapTarget {id=id+"_"+index,anchorPath=ChapterMapController.Path(a),position=a.position,
                        kind=a.name.EndsWith("quest",StringComparison.Ordinal)?MapInteractionKind.Quest:MapInteractionKind.Battle,
                        name=a.name.EndsWith("quest",StringComparison.Ordinal)?i==0?"Trưởng làng":i==3?"Dân bị giam":"Chỉ huy nghĩa binh":i==4?"Trình Viễn Chí":i==6?"Trương Giác":"Quân Khăn Vàng",
                        dialogue=objectives[i]+" Xin hãy giúp chúng tôi."}).ToArray() });
            }
            config.stages=stages.ToArray();
            // Combat scenarios and rewards require authored content. Never bind unrelated placeholder battles or invent grants.
            if(config.rewards==null||config.rewards.Length==0)config.rewards=new[]{new ChapterMapReward{id="ch01_7",label="Rương gỗ",stars=7},new ChapterMapReward{id="ch01_14",label="Rương bạc",stars=14},new ChapterMapReward{id="ch01_21",label="Rương vàng",stars=21}};
            // Reject invalid bake inputs before loops/division and before writing the prefab.
            var errors=new List<string>();config.Validate(errors);
            if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
            var spawn=stages[0].targets[0].position+new Vector3(-3,0,-3);spawn.y=0;config.initialPosition=spawn;
            var root=new GameObject("CH01_MapSystems");root.AddComponent<ChapterMapController>().Configure(config);
            BuildBridgeApproaches(root.transform,transforms);
            BakeNavigation(config,transforms.Concat(root.GetComponentsInChildren<Transform>()).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);Object.DestroyImmediate(root);
            foreach(var asset in new Object[]{config,config.ui,config.motion})EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            // Leave source scene untouched. The map builder's existing hook installs this prefab on its next build.
            var frontend=new SerializedObject(config.frontend);frontend.FindProperty("chapterMapSceneName").stringValue=config.sceneName;frontend.ApplyModifiedPropertiesWithoutUndo();
            var release=CurrentGameRelease.Settings;
            if(!release.scenes.Any(s=>AssetDatabase.GetAssetPath(s)==ScenePath)){release.scenes=release.scenes.Concat(new[]{AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)}).ToArray();EditorUtility.SetDirty(release);}
            AssetDatabase.SaveAssets();
            errors.Clear();config.Validate(errors);if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
            Debug.Log($"[ChapterMap] Built prefab, {config.stages.Length} stages, {config.stages.Sum(s=>s.targets.Length)} anchors, {config.navigationByUnlockedStage.Length} navigation states. Battle/reward content remains explicitly unbound until authored.");
        }
        private static T LoadOrCreate<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset!=null)return asset;
            asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
        private static void ConfigureIcons(MapUiConfig ui)
        {
            Sprite Icon(string name)
            {
                string path="Assets/Arena3Q/Art/UI/Icons/"+name+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new FileNotFoundException(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            ui.question=Icon("MapQuestion");ui.lockIcon=Icon("MapLock");ui.backIcon=Icon("MapBack");ui.infoIcon=Icon("MapInfo");ui.checkIcon=Icon("MapCheck");ui.swords=Icon("Swords");
            var shared=new SerializedObject(UiIconSet.Load());
            foreach(var pair in new[]{("mapBack",ui.backIcon),("mapInfo",ui.infoIcon),("mapQuestion",ui.question),("mapLock",ui.lockIcon),("mapCheck",ui.checkIcon)})shared.FindProperty(pair.Item1).objectReferenceValue=pair.Item2;
            shared.ApplyModifiedPropertiesWithoutUndo();
            Sprite Art(string name)
            {
                string path=Root+"/Textures/UI/"+name+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)return null;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            ui.medallion=Art("medallion");ui.bossMedallion=Art("boss-medallion");ui.nameplate=Art("nameplate");
            ui.chests=new[]{Art("chest-wood"),Art("chest-silver"),Art("chest-gold")};
            ui.introduction=new Rect(33,78,610,82);ui.bodyFont=17;
        }
        private static void BakeNavigation(ChapterMapConfig config,Transform[] transforms)
        {
            var temporary=new GameObject("Chapter navigation bake");
            var surface=temporary.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Volume;
            surface.center=new Vector3(0,5,0);surface.size=new Vector3(config.groundBounds.width,30,config.groundBounds.height);
            surface.useGeometry=NavMeshCollectGeometry.RenderMeshes;surface.overrideVoxelSize=true;surface.voxelSize=config.agentRadius/3;
            var modifiers=new List<Object>();
            try
            {
                // Exclude the submerged river bed, while retaining the bridge decks above water.
                var river=transforms.FirstOrDefault(t=>t.name=="River")?.GetComponent<MeshFilter>();
                if(river!=null)
                {
                    var vertices=river.sharedMesh.vertices;
                    for(int i=0;i+3<vertices.Length;i+=2)
                    {
                        var a=river.transform.TransformPoint((vertices[i]+vertices[i+1])*.5f);
                        var b=river.transform.TransformPoint((vertices[i+2]+vertices[i+3])*.5f);
                        var go=new GameObject("River bed excluded");go.transform.SetParent(temporary.transform,false);
                        go.transform.position=(a+b)*.5f-Vector3.up*5;
                        var direction=b-a;direction.y=0;go.transform.rotation=Quaternion.LookRotation(direction);
                        var volume=go.AddComponent<NavMeshModifierVolume>();volume.area=1;
                        volume.size=new Vector3(Vector3.Distance(vertices[i],vertices[i+1]),10, direction.magnitude+.3f);
                    }
                }
                // Source imports do not all have colliders. Bake the actual meshes so bridge decks
                // connect both banks and buildings block movement even when their prefab lacks physics.
                foreach(var renderer in transforms.SelectMany(t=>t.GetComponents<Renderer>()))
                {
                    // Walkable besides the terrain: bridge decks, the altar plateau and its stone stairs (config.walkableNames).
                    if(renderer.name=="Terrain" || renderer.GetComponentsInParent<Transform>().Any(t=>config.walkableNames.Any(w=>t.name.IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0)))continue;
                    var modifier=renderer.gameObject.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
                    string path=ChapterMapController.Path(renderer.transform);
                    modifier.ignoreFromBuild=path.Contains("Nature (scattered)")||path.Contains("NPC_")||path.Contains("StageMarker_")||renderer.name=="River";
                    modifiers.Add(modifier);
                }
                var cells=new List<(NavMeshModifierVolume volume,int area)>();float step=config.navigationCellSize;
                for(float z=config.groundBounds.yMin;z<config.groundBounds.yMax;z+=step)for(float x=config.groundBounds.xMin;x<config.groundBounds.xMax;x+=step)
                {
                    Vector3 p=new Vector3(x+step*.5f,0,z+step*.5f);int nearest=0;float best=float.MaxValue;
                    for(int i=0;i<config.stages.Length;i++){var delta=p-config.stages[i].center;delta.y=0;if(delta.sqrMagnitude<best){nearest=i;best=delta.sqrMagnitude;}}
                    var go=new GameObject("Locked region");go.transform.SetParent(temporary.transform,false);go.transform.position=p;
                    var volume=go.AddComponent<NavMeshModifierVolume>();volume.size=new Vector3(step,30,step);volume.center=Vector3.up*5;volume.area=1;cells.Add((volume,nearest));
                }
                config.navigationByUnlockedStage=new NavMeshData[config.stages.Length];
                for(int unlocked=1;unlocked<=config.stages.Length;unlocked++)
                {
                    foreach(var cell in cells)cell.volume.enabled=cell.area>=unlocked;
                    surface.RemoveData();surface.navMeshData=null;surface.BuildNavMesh();
                    if(surface.navMeshData==null)throw new InvalidOperationException("Navigation bake failed at "+unlocked);
                    string path=NavigationRoot+$"/CH01_Open{unlocked}.asset";var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
                    if(previous!=null){EditorUtility.CopySerialized(surface.navMeshData,previous);Object.DestroyImmediate(surface.navMeshData);surface.navMeshData=previous;EditorUtility.SetDirty(previous);}
                    else AssetDatabase.CreateAsset(surface.navMeshData,path);
                    config.navigationByUnlockedStage[unlocked-1]=surface.navMeshData;
                    Debug.Log("[ChapterMap] Navigation baked, open regions="+unlocked);
                }
            }
            finally{surface.RemoveData();foreach(var modifier in modifiers)if(modifier!=null)Object.DestroyImmediate(modifier);Object.DestroyImmediate(temporary);}
        }
        private static void BuildBridgeApproaches(Transform parent,Transform[] transforms)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(NavigationRoot+"/ApproachWood.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.28f,.15f,.075f));AssetDatabase.CreateAsset(material,NavigationRoot+"/ApproachWood.mat");}
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(NavigationRoot+"/BridgeApproach.asset");
            if(mesh==null)
            {
                mesh=new Mesh{name="Bridge timber approach"};
                // Visible sloping boards join the existing 8 m deck to banks beyond the wider water strip.
                mesh.vertices=new[]{new Vector3(2.9f,.48f,-1.05f),new Vector3(2.9f,.48f,1.05f),new Vector3(6.2f,.04f,-1.05f),new Vector3(6.2f,.04f,1.05f),new Vector3(2.9f,.32f,-1.05f),new Vector3(2.9f,.32f,1.05f),new Vector3(6.2f,-.12f,-1.05f),new Vector3(6.2f,-.12f,1.05f)};
                mesh.triangles=new[]{0,1,2,2,1,3,0,2,4,4,2,6,1,5,3,3,5,7,2,3,6,6,3,7,0,4,1,1,4,5};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,NavigationRoot+"/BridgeApproach.asset");
            }
            int index=0;
            foreach(var bridge in transforms.Where(t=>t.name=="bridge"))foreach(int side in new[]{-1,1})
            {
                var go=new GameObject("BridgeApproach_"+index++,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent,false);
                go.transform.SetPositionAndRotation(bridge.position,bridge.rotation*Quaternion.Euler(0,side<0?180:0,0));go.transform.localScale=bridge.lossyScale;
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshCollider>().sharedMesh=mesh;
            }
        }
    }
}
