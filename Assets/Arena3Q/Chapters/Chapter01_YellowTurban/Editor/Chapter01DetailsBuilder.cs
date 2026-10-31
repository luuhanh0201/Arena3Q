using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arena3Q.Exploration.Editor
{
    public static class Chapter01DetailsBuilder
    {
        private const string Root="Assets/Arena3Q/Chapters/Chapter01_YellowTurban";
        private const string Art="Assets/ThirdParty/Chapter01/_Presentation";
        [Serializable] private sealed class PlacementFile { public Patch[] patches; }
        [Serializable] private sealed class UvRect { public float x,y,width,height; }
        [Serializable] private sealed class Patch { public string id,sprite; public int stage; public Vector3 position; public Vector2 size; public float yaw,opacity; public bool upright; public UvRect uv; }
        [Serializable] private sealed class Layout { public Area[] areas; public Prop[] props; public River river; }
        [Serializable] private sealed class Area { public int id; public float[] centre; }
        [Serializable] private sealed class Prop { public string asset; public int area; public float[] pos; }
        [Serializable] private sealed class River { public float[] points; }
        [Serializable] private sealed class Audit { public int planned,placed,vertices; public string[] skipped,placedIds; public int[] perStage; public float maxGroundGap; }
        private static T Asset<T>(string name) where T:ScriptableObject
        {
            string path=Root+"/Data/"+name+".asset";var a=AssetDatabase.LoadAssetAtPath<T>(path);
            if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;
        }
        private static Texture2D Import(string source,string name,bool alpha)
        {
            Directory.CreateDirectory(Art);string dest=Art+"/"+name+".png";
            File.Copy(source,dest,true);AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(dest);importer.textureType=TextureImporterType.Default;
            importer.alphaSource=alpha?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;importer.alphaIsTransparency=alpha;
            importer.mipmapEnabled=true;importer.wrapMode=alpha?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;importer.anisoLevel=4;importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            foreach(var platform in new[]{"Android","iPhone","WebGL","Standalone"})
            {
                var p=importer.GetPlatformTextureSettings(platform);p.name=platform;p.overridden=true;p.maxTextureSize=1024;p.compressionQuality=100;
                p.format=platform=="Android"||platform=="iPhone"?TextureImporterFormat.ASTC_6x6:alpha?TextureImporterFormat.DXT5:TextureImporterFormat.DXT1;
                importer.SetPlatformTextureSettings(p);
            }
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(dest);
        }
        private static Material Material(string name,Shader shader)
        {
            string path=Root+"/Data/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;return m;
        }
        [MenuItem("Arena3Q/Chapter 01/Configure Ground Details and Audio Anchors")]
        public static void Configure()
        {
            EditorSceneManager.OpenScene(Chapter01SystemsBuilder.ScenePath);
            var map=AssetDatabase.LoadAssetAtPath<ChapterMapConfig>(Chapter01SystemsBuilder.ConfigPath);
            var config=Asset<MapDetailsConfig>("CH01_MapDetails");
            config.groundSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/Chapter01/_Map/Materials/CH01_Ground.mat");
            if(config.groundSource==null)throw new InvalidDataException("Missing ground source");
            config.groundReplacement=Material("CH01_GroundR2",config.groundSource.shader);config.groundReplacement.CopyPropertiesFromMaterial(config.groundSource);
            foreach(var pair in new[]{("_Sand","01_ground-earth"),("_Road","02_ground-path"),("_Paving","04_paddy-ground")})
                config.groundReplacement.SetTexture(pair.Item1,Import("ArtSource/Chapter01/2D/Assets/Export/1024/"+pair.Item2+".png",pair.Item2,false));
            config.detailsMaterial=Material("CH01_Details",Shader.Find("Arena3Q/Chapter Details"));
            config.detailsMaterial.SetTexture("_BaseMap",Import("ArtSource/Chapter01/2D/P4/Export/chapter-details.png","chapter-details",true));
            config.detailsMaterial.SetColor("_BaseColor",Color.white);
            var terrain=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Single(c=>c.name=="Terrain");
            var buildings=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>!c.isTrigger).ToArray();
            var input=JsonUtility.FromJson<PlacementFile>(File.ReadAllText("ArtSource/Chapter01/2D/P4/Export/placements.json"));
            var skipped=new List<string>();var placedIds=new List<string>();var audit=new Audit{planned=input.patches.Length};
            config.stageMeshes=new Mesh[map.stages.Length];config.patchCounts=new int[map.stages.Length];
            bool Ground(Vector3 p,out RaycastHit hit)=>terrain.Raycast(new Ray(new Vector3(p.x,40,p.z),Vector3.down),out hit,100);
            for(int stage=0;stage<map.stages.Length;stage++)
            {
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
                foreach(var patch in input.patches.Where(p=>p.stage==stage))
                {
                    if(patch.uv==null||patch.uv.width<=0||patch.uv.height<=0||patch.uv.x<0||patch.uv.y<0||patch.uv.x+patch.uv.width>1||patch.uv.y+patch.uv.height>1)
                        throw new InvalidDataException("Invalid atlas UV: "+patch.id);
                    if(!Ground(patch.position,out var center)){skipped.Add(patch.id+": no terrain");continue;}
                    var probe=center.point+Vector3.up*.25f;
                    if(buildings.Any(c=>c.bounds.Contains(probe))){skipped.Add(patch.id+": building");continue;}
                    int grid=patch.upright?2:config.groundGrid,start=vertices.Count;var local=new List<Vector3>();var normal=new List<Vector3>();
                    var rotation=Quaternion.Euler(0,patch.yaw,0);bool valid=true;
                    for(int y=0;y<grid;y++)for(int x=0;x<grid;x++)
                    {
                        float u=x/(float)(grid-1),v=y/(float)(grid-1);Vector3 p,n;
                        if(patch.upright){p=center.point+Quaternion.Euler(0,map.cameraYaw,0)*Vector3.right*((u-.5f)*patch.size.x)+Vector3.up*(v*patch.size.y+config.groundOffset);n=Vector3.up;}
                        else
                        {
                            p=patch.position+rotation*new Vector3((u-.5f)*patch.size.x,0,(v-.5f)*patch.size.y);
                            if(!Ground(p,out var hit)){valid=false;break;}
                            p=hit.point+Vector3.up*config.groundOffset;n=hit.normal;
                            audit.maxGroundGap=Mathf.Max(audit.maxGroundGap,p.y-hit.point.y);
                        }
                        local.Add(p);normal.Add(n);
                    }
                    if(!valid||local.Count!=grid*grid){skipped.Add(patch.id+": edge");continue;}
                    vertices.AddRange(local);normals.AddRange(normal);
                    for(int y=0;y<grid;y++)for(int x=0;x<grid;x++)
                    {uv.Add(new Vector2(patch.uv.x+patch.uv.width*x/(grid-1),patch.uv.y+patch.uv.height*y/(grid-1)));colors.Add(new Color(1,1,1,patch.opacity));}
                    for(int y=0;y<grid-1;y++)for(int x=0;x<grid-1;x++)
                    {int a=start+y*grid+x,b=a+1,c=a+grid,d=c+1;triangles.AddRange(new[]{a,c,b,b,c,d});}
                    config.patchCounts[stage]++;
                    placedIds.Add(patch.id);
                }
                string path=Root+"/Data/CH01_Details_"+(stage+1)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.name="CH01 details "+(stage+1);mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh);config.stageMeshes[stage]=mesh;audit.vertices+=vertices.Count;
            }
            audit.perStage=config.patchCounts;audit.placed=config.patchCounts.Sum();audit.skipped=skipped.ToArray();audit.placedIds=placedIds.ToArray();
            foreach(string required in new[]{"wildflowers","fallen-cloth","village-litter"})
                if(input.patches.Any(p=>p.sprite==required)&&!input.patches.Any(p=>p.sprite==required&&placedIds.Contains(p.id)))
                    throw new InvalidDataException("All placements rejected for required detail: "+required);
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/P4b-details-placement.json",JsonUtility.ToJson(audit,true));
            config.enabled=true;map.details=config;ConfigureAudio(map);
            foreach(var a in new UnityEngine.Object[]{config,config.groundReplacement,config.detailsMaterial,map})EditorUtility.SetDirty(a);
            AssetDatabase.SaveAssets();Debug.Log("[P4b] "+audit.placed+" ground details / "+audit.vertices+" vertices; source scene untouched.");
        }
        private static void ConfigureAudio(ChapterMapConfig map)
        {
            var config=Asset<MapAudioConfig>("CH01_MapAudio");
            if(config.cues.Length==0)config.cues=Enum.GetValues(typeof(MapSound)).Cast<MapSound>().Select(s=>new MapAudioCue{sound=s}).ToArray();
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(Root+"/Data/CH01_MapLayout.json"));var anchors=new List<MapAudioAnchor>();
            foreach(var area in layout.areas)
                anchors.Add(new MapAudioAnchor{id="area-"+area.id,stage=area.id-1,sound=area.id==1?MapSound.Village:area.id<4?MapSound.WindBirds:MapSound.Camp,position=new Vector3(area.centre[0],0,area.centre[1]),radius=28});
            foreach(var fire in layout.props.Where(p=>p.asset=="campfire"))
                anchors.Add(new MapAudioAnchor{id="fire-"+fire.area,stage=fire.area-1,sound=MapSound.Campfire,position=new Vector3(fire.pos[0],0,fire.pos[1]),radius=12});
            for(int i=0;i<layout.river.points.Length;i+=2)
            {
                var p=new Vector3(layout.river.points[i],0,layout.river.points[i+1]);if(!map.groundBounds.Contains(new Vector2(p.x,p.z)))continue;
                int stage=Enumerable.Range(0,map.stages.Length).OrderBy(s=>(map.stages[s].center-p).sqrMagnitude).First();
                anchors.Add(new MapAudioAnchor{id="river-"+i,stage=stage,sound=MapSound.River,position=p,radius=18});
            }
            config.anchors=anchors.ToArray();map.audio=config;EditorUtility.SetDirty(config);
        }
    }
}
