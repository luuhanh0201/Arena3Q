using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Checks how Mặc Ảnh's fan moves in each battle clip (user 2026-10-22: "the fan turns the wrong way when she attacks;
    /// the new animation seems to be on the body only, not on the weapon"): where the fan parts hang in the model, whether
    /// the clips animate them, and how much the fan turns relative to the right hand and in the world over each clip.
    /// Writes Logs/MacAnhFanAudit.txt. Arena3Q > Mac Anh > Audit Fan Motion.
    /// </summary>
    public static class MacAnhFanAudit
    {
        private const string ViewPrefab = "Assets/Arena3Q/Art/Heroes/MacAnh/Resources/MacAnh/MacAnhRealtimeBattleView.prefab";

        [MenuItem("Arena3Q/Mac Anh/Audit Fan Motion")]
        public static void Run()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefab);
            var serialized = new SerializedObject(view.GetComponents<MonoBehaviour>().First(m => m != null && m.GetType().Name == "MacAnhRealtimeBattleView"));
            var character = (GameObject)serialized.FindProperty("characterPrefab").objectReferenceValue;
            var clips = serialized.FindProperty("clips");
            var report = new StringBuilder();
            var model = UnityEngine.Object.Instantiate(character);
            try
            {
                var all = model.GetComponentsInChildren<Transform>(true);
                var fanParts = all.Where(t => t.name.StartsWith("Fan", StringComparison.Ordinal)).ToArray();
                var hand = all.FirstOrDefault(t => t.name == "RightHand");
                report.AppendLine("Fan parts and their parent chain:");
                foreach (var part in fanParts)
                    report.AppendLine($"  {part.name} <- {Chain(part, model.transform)}");
                var handle = fanParts.FirstOrDefault(t => t.name == "Fan_Handle") ?? fanParts.FirstOrDefault();
                ReportFingerSkin(model, all, report);
                for (var i = 0; i < clips.arraySize; i++)
                {
                    var entry = clips.GetArrayElementAtIndex(i);
                    var action = entry.FindPropertyRelative("action").enumDisplayNames[entry.FindPropertyRelative("action").enumValueIndex];
                    var clip = (AnimationClip)entry.FindPropertyRelative("clip").objectReferenceValue;
                    if (clip == null)
                        continue;
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    var fanCurves = bindings.Count(b => b.path.Split('/').Last().StartsWith("Fan", StringComparison.Ordinal));
                    float maxToHand = 0f, maxWorld = 0f, maxGrip = 0f;
                    Quaternion? firstLocal = null, firstWorld = null;
                    Vector3 handMin = Vector3.positiveInfinity, handMax = Vector3.negativeInfinity;
                    var skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(r => r.sharedMesh != null ? r.sharedMesh.vertexCount : 0).FirstOrDefault();
                    var mesh = new Mesh();
                    var handVertex = -1;
                    const int steps = 60;
                    for (var s = 0; s <= steps; s++)
                    {
                        clip.SampleAnimation(model, clip.length * s / steps);
                        if (handle == null || hand == null)
                            break;
                        var toHand = Quaternion.Inverse(hand.rotation) * handle.rotation;
                        firstLocal ??= toHand;
                        firstWorld ??= handle.rotation;
                        maxToHand = Mathf.Max(maxToHand, Quaternion.Angle(firstLocal.Value, toHand));
                        maxWorld = Mathf.Max(maxWorld, Quaternion.Angle(firstWorld.Value, handle.rotation));
                        // What the player sees: the skinned palm surface (the vertex nearest the hand bone), not the bone.
                        if (skinned == null)
                            continue;
                        skinned.BakeMesh(mesh, true);
                        var vertices = mesh.vertices;
                        if (handVertex < 0)
                        {
                            var best = float.MaxValue;
                            for (var v = 0; v < vertices.Length; v++)
                            {
                                var d = (skinned.transform.TransformPoint(vertices[v]) - hand.position).sqrMagnitude;
                                if (d < best) { best = d; handVertex = v; }
                            }
                        }
                        var palm = skinned.transform.TransformPoint(vertices[handVertex]);
                        handMin = Vector3.Min(handMin, palm);
                        handMax = Vector3.Max(handMax, palm);
                        maxGrip = Mathf.Max(maxGrip, Vector3.Distance(palm, handle.position));
                    }
                    var travel = handMax - handMin;
                    report.AppendLine($"{action,-12} {clip.length:0.00}s  curves {bindings.Length,4} (fan {fanCurves,3})  " +
                                      $"fan vs hand turns up to {maxToHand,6:0.0}°  fan in world up to {maxWorld,6:0.0}°  " +
                                      $"palm surface travels {travel.x * 100,4:0}x{travel.y * 100,4:0}x{travel.z * 100,4:0} cm  fan grip from palm up to {maxGrip * 100,5:0.0} cm");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/MacAnhFanAudit.txt", report.ToString());
            Debug.Log("[Arena3Q] FAN AUDIT\n" + report);
        }

        /// <summary>The bones that carry the skin around each finger bone of the imported model (rest pose): a hand
        /// whose fingers hang on the wrong bone stays behind while the fan moves on.</summary>
        private static void ReportFingerSkin(GameObject model, Transform[] all, StringBuilder report)
        {
            foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = skinned.sharedMesh;
                if (mesh == null || mesh.vertexCount < 1000)
                    continue;
                var baked = new Mesh();
                skinned.BakeMesh(baked, true);
                var vertices = baked.vertices;
                var weights = mesh.boneWeights;
                report.AppendLine($"Finger skin of {skinned.name} ({mesh.vertexCount} vertices, {skinned.bones.Length} bones):");
                var hands = all.Where(t => t.name == "RightHand").ToArray();
                report.AppendLine($"  transforms named RightHand: {hands.Length} ({string.Join(" | ", hands.Select(h => Chain(h, model.transform)))}); " +
                                  $"skin uses the first: {skinned.bones.Contains(hands.FirstOrDefault())}, root bone {(skinned.rootBone != null ? skinned.rootBone.name : "-")}");
                foreach (var boneName in new[] { "RightHand", "RightIndex2", "RightMiddle3", "RightThumb2", "LeftHand", "LeftIndex2" })
                {
                    var bone = all.FirstOrDefault(t => t.name == boneName);
                    if (bone == null)
                        continue;
                    var nearest = Enumerable.Range(0, vertices.Length)
                        .OrderBy(v => (skinned.transform.TransformPoint(vertices[v]) - bone.position).sqrMagnitude).Take(25).ToArray();
                    var carriers = nearest.GroupBy(v => skinned.bones[weights[v].boneIndex0] != null ? skinned.bones[weights[v].boneIndex0].name : "-")
                        .OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}");
                    var distance = (skinned.transform.TransformPoint(vertices[nearest[0]]) - bone.position).magnitude;
                    report.AppendLine($"  {boneName,-12} nearest skin {distance * 100:0.0} cm, carried by {string.Join(", ", carriers)}");
                }
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static string Chain(Transform t, Transform root)
        {
            var names = new List<string>();
            for (var p = t.parent; p != null && p != root; p = p.parent)
                names.Add(p.name);
            return string.Join(" <- ", names);
        }
    }
}
