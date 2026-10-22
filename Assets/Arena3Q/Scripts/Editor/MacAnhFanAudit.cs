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
                for (var i = 0; i < clips.arraySize; i++)
                {
                    var entry = clips.GetArrayElementAtIndex(i);
                    var action = entry.FindPropertyRelative("action").enumDisplayNames[entry.FindPropertyRelative("action").enumValueIndex];
                    var clip = (AnimationClip)entry.FindPropertyRelative("clip").objectReferenceValue;
                    if (clip == null)
                        continue;
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    var fanCurves = bindings.Count(b => b.path.Split('/').Last().StartsWith("Fan", StringComparison.Ordinal));
                    float maxToHand = 0f, maxWorld = 0f;
                    Quaternion? firstLocal = null, firstWorld = null;
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
                    }
                    report.AppendLine($"{action,-12} {clip.length:0.00}s  curves {bindings.Length,4} (fan {fanCurves,3})  " +
                                      $"fan vs hand turns up to {maxToHand,6:0.0}°  fan in world up to {maxWorld,6:0.0}°");
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

        private static string Chain(Transform t, Transform root)
        {
            var names = new List<string>();
            for (var p = t.parent; p != null && p != root; p = p.parent)
                names.Add(p.name);
            return string.Join(" <- ", names);
        }
    }
}
