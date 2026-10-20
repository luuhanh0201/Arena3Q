using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class MacAnhImportedSurfaceQa
    {
        // UInt64.GetHashCode XORs the two vertex IDs. Adjacent IDs then
        // concentrate in very few buckets, making a dense surface quadratic.
        private sealed class EdgeComparer : IEqualityComparer<ulong>
        {
            public bool Equals(ulong a,ulong b)=>a==b;
            public int GetHashCode(ulong value)
            {
                unchecked
                {
                    value+=0x9e3779b97f4a7c15UL;
                    value=(value^(value>>30))*0xbf58476d1ce4e5b9UL;
                    value=(value^(value>>27))*0x94d049bb133111ebUL;
                    value^=value>>31;
                    return (int)(value^(value>>32));
                }
            }
        }
        public static void InspectScale()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MacAnhGraphicsBuilder.Root+"/LOD/MacAnh_Battle3D.prefab");
            var go=UnityEngine.Object.Instantiate(prefab);var baked=new Mesh();
            try
            {
                var body=go.GetComponentInChildren<SkinnedMeshRenderer>();
                foreach(bool useScale in new[]{false,true})
                {
                    body.BakeMesh(baked,useScale);var v=baked.vertices;
                    Debug.Log("MACANH_SCALE useScale="+useScale+" local="+body.transform.localScale+" lossy="+body.transform.lossyScale+
                        " shared="+body.sharedMesh.bounds+" baked="+baked.bounds+" renderer="+body.bounds+
                        " transformedHeight="+(v.Max(p=>body.transform.TransformPoint(p).y)-v.Min(p=>body.transform.TransformPoint(p).y)));
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(baked);}
        }
        public static void InspectMaterials()
        {
            string path=MacAnhGraphicsBuilder.Root+"/Source/LOD/MacAnh_BattleRig.fbx";
            foreach(var pair in ((ModelImporter)AssetImporter.GetAtPath(path)).GetExternalObjectMap())
                Debug.Log("MACANH_REMAP "+pair.Key.name+" -> "+AssetDatabase.GetAssetPath(pair.Value));
            foreach(var renderer in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>())
                foreach(var material in renderer.sharedMaterials)
                    Debug.Log("MACANH_ACTUAL_MATERIAL "+renderer.name+" "+material.name+" shader="+material.shader.name+" asset="+AssetDatabase.GetAssetPath(material));
        }
        [Serializable] private sealed class ClipResult
        {
            public string name;public int samples,spikes,stretched,compressed,heightErrors;public float maxRatio=1,minRatio=1,worstTime,minHeight=float.MaxValue,maxHeight;
        }
        [Serializable] private sealed class Report
        {
            public string lod,units="world metres; BakeMesh(useScale:true), then localToWorld";public int vertices,edges;public float height,absoluteDelta;public bool passed;
            public List<ClipResult> clips=new List<ClipResult>();
        }
        public static void Capture(string label)
        {
            string root=MacAnhGraphicsBuilder.Root;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/LOD/MacAnh_"+label+"3D.prefab");
            var clips=AssetDatabase.LoadAllAssetsAtPath(root+"/Source/LOD/MacAnh_LODActions.fbx")
                .OfType<AnimationClip>().Where(clip=>!clip.name.StartsWith("__")).ToArray();
            if(clips.Length!=11)throw new InvalidOperationException("Imported QA requires all eleven actions.");
            var go=UnityEngine.Object.Instantiate(prefab);var baked=new Mesh();
            try
            {
                foreach(var animator in go.GetComponentsInChildren<Animator>())animator.enabled=false;
                var body=go.GetComponentInChildren<SkinnedMeshRenderer>();body.quality=SkinQuality.Bone4;body.updateWhenOffscreen=true;
                // Imported FBX has renderer scale 100. BakeMesh(false) already
                // applies it; multiplying by Transform again counted it twice.
                // true compensates that scale, yielding actual renderer-local data.
                body.BakeMesh(baked,true);var rest=baked.vertices;
                var restToWorld=body.transform.localToWorldMatrix;
                for(int i=0;i<rest.Length;i++)rest[i]=restToWorld.MultiplyPoint3x4(rest[i]);
                var triangles=baked.triangles;var unique=new HashSet<ulong>(new EdgeComparer());
                Action<int,int> add=(a,b)=>unique.Add(((ulong)Math.Min(a,b)<<32)|(uint)Math.Max(a,b));
                for(int i=0;i<triangles.Length;i+=3)
                {add(triangles[i],triangles[i+1]);add(triangles[i+1],triangles[i+2]);add(triangles[i+2],triangles[i]);}
                var all=unique.ToArray();var aIds=new List<int>();var bIds=new List<int>();var lengths=new List<float>();
                foreach(ulong edge in all)
                {
                    int a=(int)(edge>>32),b=(int)(edge&uint.MaxValue);float length=Vector3.Distance(rest[a],rest[b]);
                    if(length<1e-8f)continue;aIds.Add(a);bIds.Add(b);lengths.Add(length);
                }
                float height=rest.Max(v=>v.y)-rest.Min(v=>v.y);
                if(height<1.65f||height>1.75f)throw new InvalidOperationException("Unexpected imported height in metres: "+height);
                var report=new Report{lod=label,vertices=rest.Length,edges=lengths.Count,height=height,absoluteDelta=.004f*height};
                var vertices=new List<Vector3>(rest.Length);
                Debug.Log("[MacAnh imported surface] Prepared "+label+" vertices="+rest.Length+" edges="+lengths.Count);
                using var fanPoses=new MacAnhImportedFanQa.Writer(label,go,body,clips);
                foreach(var clip in clips)
                {
                    var result=new ClipResult{name=clip.name};int last=Mathf.RoundToInt(clip.length*60);
                    for(int frame=0;frame<=last;frame++)
                    {
                        float time=Mathf.Min(frame/60f,clip.length);clip.SampleAnimation(go,time);body.BakeMesh(baked,true);baked.GetVertices(vertices);
                        if(vertices.Count!=rest.Length)throw new InvalidOperationException("Vertex count changed after import.");
                        var toWorld=body.transform.localToWorldMatrix;
                        float low=float.PositiveInfinity,high=float.NegativeInfinity;
                        for(int i=0;i<vertices.Count;i++)
                        {vertices[i]=toWorld.MultiplyPoint3x4(vertices[i]);float y=vertices[i].y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
                        float poseHeight=high-low;result.minHeight=Mathf.Min(result.minHeight,poseHeight);result.maxHeight=Mathf.Max(result.maxHeight,poseHeight);
                        fanPoses.Sample(clip.name,time,vertices);
                        if(poseHeight<height*.65f||poseHeight>height*1.2f)result.heightErrors++;
                        int spikes=0,stretched=0,compressed=0;
                        for(int i=0;i<lengths.Count;i++)
                        {
                            float length=Vector3.Distance(vertices[aIds[i]],vertices[bIds[i]]),delta=length-lengths[i],ratio=length/lengths[i];
                            if(float.IsNaN(ratio)||float.IsInfinity(ratio))throw new InvalidOperationException("Non-finite imported surface.");
                            if(ratio>result.maxRatio){result.maxRatio=ratio;result.worstTime=time;}
                            result.minRatio=Mathf.Min(result.minRatio,ratio);
                            if(ratio>3&&delta>.002f)spikes++;
                            if(ratio>1.35f&&delta>report.absoluteDelta)stretched++;
                            if(ratio<.65f&&-delta>report.absoluteDelta)compressed++;
                        }
                        result.samples++;result.spikes=Math.Max(result.spikes,spikes);
                        result.stretched=Math.Max(result.stretched,stretched);result.compressed=Math.Max(result.compressed,compressed);
                    }
                    report.clips.Add(result);Debug.Log("[MacAnh imported surface] "+label+" "+clip.name+" samples="+result.samples+
                        " spike="+result.spikes+" stretch="+result.stretched+" compression="+result.compressed);
                }
                report.passed=report.clips.All(item=>item.spikes==0&&item.stretched==0&&item.compressed==0&&item.heightErrors==0);
                string output="docs/art/concepts/MuuSi_main-hero/MacAnh/Production/LOD/ImportedSurface_"+label+".json";
                File.WriteAllText(output,JsonUtility.ToJson(report,true));
                if(!report.passed)throw new InvalidOperationException("Imported LOD surface audit failed; inspect "+output);
                Debug.Log("IMPORT_SURFACE_AUDIT_COMPLETE "+label+" passed=true");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(baked);}
        }
    }
}
