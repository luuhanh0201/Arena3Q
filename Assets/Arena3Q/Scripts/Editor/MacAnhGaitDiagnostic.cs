using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class MacAnhGaitDiagnostic
    {
        public static void Run()
        {
            const string root = "Assets/Arena3Q/Art/Heroes/MacAnh/";
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Resources/MacAnh/MacAnhRealtimeBattleView.prefab");
            var data = new SerializedObject(view.GetComponents<MonoBehaviour>().First(x => x != null && x.GetType().Name == "MacAnhRealtimeBattleView"));
            var model = UnityEngine.Object.Instantiate((GameObject)data.FindProperty("characterPrefab").objectReferenceValue);
            var output = new StringBuilder("clip,frame,bone,x,y,z,weightedVertices,surfaceX,surfaceY,surfaceZ,asset\n");
            var clips = data.FindProperty("clips");
            foreach (var name in new[] { "Walk", "Run" })
            {
                AnimationClip clip = null;
                for (int i = 0; i < clips.arraySize; i++)
                {
                    var c = (AnimationClip)clips.GetArrayElementAtIndex(i).FindPropertyRelative("clip").objectReferenceValue;
                    if (c != null && c.name == name) clip = c;
                }
                if (clip == null) throw new InvalidOperationException(name);
                for (int f = 0; f <= Mathf.RoundToInt(clip.length * 30); f += 3)
                {
                    clip.SampleAnimation(model, f / 30f);
                    foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var mesh = new Mesh(); skin.BakeMesh(mesh, true);
                        var vertices = mesh.vertices; var weights = skin.sharedMesh.boneWeights;
                        foreach (var side in new[] { "RightFoot", "LeftFoot" })
                        {
                            int b = Array.FindIndex(skin.bones, t => t.name == side);
                            if (b < 0) continue;
                            var ids = Enumerable.Range(0, weights.Length).Where(i => weights[i].boneIndex0 == b && weights[i].weight0 > .5f).ToArray();
                            var sum = Vector3.zero;
                            foreach (int i in ids) sum += skin.transform.TransformPoint(vertices[i]);
                            var center = sum / Mathf.Max(1, ids.Length); var p = skin.bones[b].position;
                            output.AppendLine(FormattableString.Invariant($"{name},{f},{side},{p.x},{p.y},{p.z},{ids.Length},{center.x},{center.y},{center.z},{AssetDatabase.GetAssetPath(clip)}"));
                        }
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                }
            }
            UnityEngine.Object.DestroyImmediate(model);
            File.WriteAllText("Logs/MacAnh-gait-import.csv", output.ToString());
            Debug.Log("MACANH_GAIT_DIAGNOSTIC_COMPLETE");
        }
    }
}
