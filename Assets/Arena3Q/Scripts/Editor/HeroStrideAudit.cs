using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Measures how far Mặc Ảnh's walk and run clips really carry the body per cycle, from the feet: while a foot is
    /// planted it slides backwards under the in-place clip exactly as fast as the body must move for it not to slip.
    /// Compares that with the stride lengths in MacAnhPerformance (which set the clip speed in battle) and writes
    /// Logs/StrideAudit.json. Arena3Q > Mac Anh > Audit Walk and Run Strides.
    /// </summary>
    public static class HeroStrideAudit
    {
        private const string ViewPrefab = "Assets/Arena3Q/Art/Heroes/MacAnh/Resources/MacAnh/MacAnhRealtimeBattleView.prefab";
        private const string Performance = "Assets/Arena3Q/Art/Heroes/MacAnh/MacAnhPerformance.asset";
        private static readonly string[] Feet = { "LeftToe", "RightToe", "LeftFoot", "RightFoot" };

        [Serializable]
        private sealed class ClipReport
        {
            public string action;
            public float clipSeconds, dataStride, measuredStride, contactSpeed, slipPercent;
            public int contactSamples;
        }

        [Serializable]
        private sealed class Report { public List<ClipReport> clips = new List<ClipReport>(); }

        [MenuItem("Arena3Q/Mac Anh/Audit Walk and Run Strides")]
        public static void Run()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefab) ?? throw new InvalidOperationException("Missing " + ViewPrefab);
            var serialized = new SerializedObject(view.GetComponents<MonoBehaviour>().First(m => m != null && m.GetType().Name == "MacAnhRealtimeBattleView"));
            var character = (GameObject)serialized.FindProperty("characterPrefab").objectReferenceValue;
            var clips = serialized.FindProperty("clips");
            var set = AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(Performance);
            var report = new Report();
            var model = UnityEngine.Object.Instantiate(character);
            try
            {
                for (var i = 0; i < clips.arraySize; i++)
                {
                    var entry = clips.GetArrayElementAtIndex(i);
                    var action = (MacAnhAction)entry.FindPropertyRelative("action").enumValueIndex;
                    if (action != MacAnhAction.Walk && action != MacAnhAction.Run)
                        continue;
                    var clip = (AnimationClip)entry.FindPropertyRelative("clip").objectReferenceValue;
                    var data = action == MacAnhAction.Walk ? set.WalkStrideLength : set.RunStrideLength;
                    report.clips.Add(Measure(model, clip, action, data));
                    // How much the answer depends on what counts as planted (the in-battle check uses the drawn size).
                    foreach (var contact in new[] { .005f, .01f, .02f, .03f })
                    {
                        var probe = Measure(model, clip, action, data, contact);
                        Debug.Log($"[Arena3Q] STRIDE {probe.action} contact {contact * 100:0.0} cm: {probe.measuredStride:0.000} m ({probe.contactSamples} samples)");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/StrideAudit.json", JsonUtility.ToJson(report, true));
            foreach (var c in report.clips)
                Debug.Log($"[Arena3Q] STRIDE {c.action}: clip {c.clipSeconds:0.000} s, data stride {c.dataStride:0.000} m, " +
                          $"measured {c.measuredStride:0.000} m (feet slip {c.slipPercent:0.0} %)");
        }

        private static ClipReport Measure(GameObject model, AnimationClip clip, MacAnhAction action, float dataStride,
            float contact = .005f)
        {
            var feet = Feet.Select(n => model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == n))
                .Where(t => t != null).ToArray();
            const int steps = 240;
            var positions = new Vector3[feet.Length, steps + 1];
            for (var s = 0; s <= steps; s++)
            {
                clip.SampleAnimation(model, clip.length * s / steps);
                for (var f = 0; f < feet.Length; f++)
                    positions[f, s] = model.transform.InverseTransformPoint(feet[f].position);
            }
            // Planted = within 5 mm of the foot's lowest point (wider thresholds take in the lift-off, where the foot already
            // moves forward, and underestimate the stride); its ground velocity there is the body speed (in place).
            var dt = clip.length / steps;
            // Mean velocity vector (not mean speed): the sideways sway of the feet cancels, the backward glide remains.
            var velocity = Vector2.zero;
            var samples = 0;
            for (var f = 0; f < feet.Length; f++)
            {
                var lowest = float.MaxValue;
                for (var s = 0; s <= steps; s++)
                    lowest = Mathf.Min(lowest, positions[f, s].y);
                for (var s = 0; s < steps; s++)
                {
                    if (positions[f, s].y > lowest + contact || positions[f, s + 1].y > lowest + contact)
                        continue;
                    var d = positions[f, s + 1] - positions[f, s];
                    velocity += new Vector2(d.x, d.z) / dt;
                    samples++;
                }
            }
            var plantedSpeed = samples > 0 ? (velocity / samples).magnitude : 0f;
            var measured = plantedSpeed * clip.length;
            return new ClipReport
            {
                action = action.ToString(), clipSeconds = clip.length, dataStride = dataStride, measuredStride = measured,
                contactSpeed = plantedSpeed, contactSamples = samples,
                // At the data stride the clip plays measured/data times too fast or slow: the planted foot slips by this.
                slipPercent = measured > 0f ? (measured / dataStride - 1f) * 100f : 0f
            };
        }
    }
}
