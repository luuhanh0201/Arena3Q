using System;
using System.IO;
using System.Linq;
using System.Text;
using Arena3Q.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arena3Q.Editor.Chapter01
{
    /// <summary>Close views keep the native metre scale and include a wooden crate from the game.</summary>
    public static class Chapter01SketchfabDetailReview
    {
        [Serializable] private sealed class Manifest { public Piece[] pieces; }
        [Serializable] private sealed class Piece { public string name,path; public float[] size; public bool cutout; }
        public static void BuildAndRender()
        {
            Chapter01ColorReference.RenderSketchfabChecks();
            Render();
        }
        public static void Render()
        {
            const string root="Assets/ThirdParty/Chapter01/Sketchfab";
            const string output="ArtSource/Chapter01/3D/Renders/Sketchfab/UnityDetails";
            Directory.CreateDirectory(output);
            var library=AssetDatabase.LoadAssetAtPath<BattlefieldAssetLibrary>("Assets/Arena3Q/Content/Battlefield/BattlefieldAssetLibrary.asset");
            var config=AssetDatabase.LoadAssetAtPath<BattlefieldConfig>("Assets/Arena3Q/Content/Battlefield/BattlefieldConfig.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=Chapter01ColorReference.Stage(config,out _);
            var report=new StringBuilder("name\theight_m\tcomparison\tfile\n");int count=0;
            foreach(var manifest in Directory.GetFiles(root,"manifest.json",SearchOption.AllDirectories).OrderBy(x=>x))
            foreach(var piece in JsonUtility.FromJson<Manifest>(File.ReadAllText(manifest)).pieces.Where(p=>p.size[2]<3))
            {
                var model=Chapter01ColorReference.Spawn(new Chapter01ColorReference.Candidate("detail",piece.name,Path.ChangeExtension(piece.path,".prefab"),0,cutout:piece.cutout),library,out _,out _,out _);
                var reference=Chapter01ColorReference.Spawn(new Chapter01ColorReference.Candidate("detail","Game Wooden Crate","lib:Crate_Wooden",0),library,out _,out _,out _);
                if(model==null||reference==null)throw new InvalidOperationException("Missing detail candidate / reference");
                try
                {
                    var a=Chapter01ColorReference.Bounds(model.GetComponentsInChildren<Renderer>());
                    var b=Chapter01ColorReference.Bounds(reference.GetComponentsInChildren<Renderer>());
                    var right=new Vector3(camera.transform.right.x,0,camera.transform.right.z).normalized;
                    reference.transform.position+=right*(a.extents.magnitude+b.extents.magnitude+.35f);
                    a.Encapsulate(Chapter01ColorReference.Bounds(reference.GetComponentsInChildren<Renderer>()));
                    var inverse=Quaternion.Inverse(camera.transform.rotation);float half=.1f;
                    for(int i=0;i<8;i++)
                    {
                        var corner=new Vector3((i&1)==0?a.min.x:a.max.x,(i&2)==0?a.min.y:a.max.y,(i&4)==0?a.min.z:a.max.z);
                        var local=inverse*(corner-a.center);half=Mathf.Max(half,Mathf.Abs(local.x)/1.5f,Mathf.Abs(local.y));
                    }
                    camera.orthographicSize=half*1.16f;camera.transform.position=a.center-camera.transform.forward*100;
                    Chapter01ColorReference.Capture(camera,output+"/"+piece.name+".png",1080,720);
                    report.AppendLine($"{piece.name}\t{piece.size[2]:F4}\tlib:Crate_Wooden native scale\t{piece.name}.png");count++;
                }
                finally{Object.DestroyImmediate(model);Object.DestroyImmediate(reference);}
            }
            File.WriteAllText(output+"/report.tsv",report.ToString());
            Debug.Log("SKETCHFAB_DETAIL_PASS pieces="+count);
        }
    }
}
