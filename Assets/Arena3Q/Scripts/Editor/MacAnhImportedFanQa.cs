using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    // Export the actual imported mesh/weights and sampled bone/fan transforms.
    // The offline triangle audit reconstructs these poses, independently checked
    // here against Unity BakeMesh at distributed vertices on every half-frame.
    public static class MacAnhImportedFanQa
    {
        public sealed class Writer : IDisposable
        {
            private readonly BinaryWriter output;
            private readonly string label;
            private readonly SkinnedMeshRenderer body;
            private readonly MeshFilter[] fans;
            private readonly Vector3[] rest;
            private readonly BoneWeight[] weights;
            private readonly Matrix4x4[] bind;
            private readonly Transform[] bones;
            private readonly int expected;
            private int samples;
            private float maxError;
            private readonly string[] boneNames;

            [Serializable] private sealed class Proof
            {
                public string lod,geometry="actual Unity imported mesh, maximum four influences",pose="world bone matrices checked against BakeMesh(true) every sample";
                public int samples,expectedSamples,fanParts;
                public float maxReconstructionErrorM;
                public bool passed;
                public string[] bones;
            }

            public Writer(string lod,GameObject root,SkinnedMeshRenderer renderer,AnimationClip[] clips)
            {
                label=lod;body=renderer;
                fans=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Fan_")).OrderBy(f=>f.name).ToArray();
                if(fans.Length!=6)throw new InvalidOperationException("Native fan QA requires all six logical parts.");
                var mesh=body.sharedMesh;rest=mesh.vertices;weights=mesh.boneWeights;bind=mesh.bindposes;
                bones=body.bones;boneNames=bones.Select(b=>b.name).ToArray();
                if(weights.Length!=rest.Length)throw new InvalidOperationException("Missing imported four-weight skin data.");
                expected=clips.Sum(c=>Mathf.RoundToInt(c.length*60)+1);
                Directory.CreateDirectory("Library/MacAnhNativeContacts");
                output=new BinaryWriter(File.Create("Library/MacAnhNativeContacts/"+label+".bin"));
                output.Write(Encoding.ASCII.GetBytes("MAFAN001"));
                var triangles=mesh.triangles;
                output.Write(rest.Length);output.Write(triangles.Length);output.Write(bind.Length);output.Write(fans.Length);output.Write(expected);
                foreach(var vertex in rest)Write(vertex);
                foreach(int index in triangles)output.Write(index);
                for(int i=0;i<rest.Length;i++)
                {
                    var w=weights[i];output.Write(w.boneIndex0);output.Write(w.boneIndex1);output.Write(w.boneIndex2);output.Write(w.boneIndex3);
                    output.Write(w.weight0);output.Write(w.weight1);output.Write(w.weight2);output.Write(w.weight3);
                    bool palm=true;
                    foreach(var pair in new[]{(w.boneIndex0,w.weight0),(w.boneIndex1,w.weight1),(w.boneIndex2,w.weight2),(w.boneIndex3,w.weight3)})
                        if(pair.Item2>0&&boneNames[pair.Item1]!="RightHand"&&boneNames[pair.Item1]!="LeftHand")palm=false;
                    output.Write((byte)(palm?1:0));
                }
                foreach(var fan in fans)
                {
                    Write(fan.name);var vertices=fan.sharedMesh.vertices;var faces=fan.sharedMesh.triangles;
                    output.Write(vertices.Length);output.Write(faces.Length);
                    foreach(var vertex in vertices)Write(vertex);
                    foreach(int index in faces)output.Write(index);
                }
            }

            public void Sample(string clip,float time,List<Vector3> actualWorld)
            {
                Write(clip);output.Write(time);
                var matrices=new Matrix4x4[bind.Length];
                for(int i=0;i<bind.Length;i++){matrices[i]=bones[i].localToWorldMatrix*bind[i];Write(matrices[i]);}
                foreach(var fan in fans)Write(fan.transform.localToWorldMatrix);
                // Check 65 vertices distributed across the imported surface on
                // every sample; the full body edge audit uses actual BakeMesh.
                for(int k=0;k<=64;k++)
                {
                    int i=k*(rest.Length-1)/64;var w=weights[i];var p=rest[i];
                    var reconstructed=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+
                        matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+
                        matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+
                        matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                    maxError=Mathf.Max(maxError,Vector3.Distance(reconstructed,actualWorld[i]));
                }
                samples++;
                if(maxError>.00002f)throw new InvalidOperationException("Native contact pose reconstruction differs from BakeMesh: "+maxError);
            }

            private void Write(Vector3 p){output.Write(p.x);output.Write(p.y);output.Write(p.z);}
            private void Write(Matrix4x4 m){for(int row=0;row<4;row++)for(int col=0;col<4;col++)output.Write(m[row,col]);}
            private void Write(string value){var bytes=Encoding.UTF8.GetBytes(value);output.Write(bytes.Length);output.Write(bytes);}
            public void Dispose()
            {
                output.Dispose();
                var proof=new Proof{lod=label,samples=samples,expectedSamples=expected,fanParts=fans.Length,
                    maxReconstructionErrorM=maxError,bones=boneNames,passed=samples==expected&&maxError<=.00002f};
                File.WriteAllText("Library/MacAnhNativeContacts/"+label+".json",JsonUtility.ToJson(proof,true));
                Debug.Log("NATIVE_FAN_POSES_EXPORTED "+label+" samples="+samples+" error="+maxError);
            }
        }
    }
}
