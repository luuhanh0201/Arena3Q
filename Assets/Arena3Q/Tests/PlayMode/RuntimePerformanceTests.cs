#if UNITY_EDITOR
using System.Collections;
using System.IO;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Arena3Q.Tests.PlayMode
{
    public sealed class RuntimePerformanceTests
    {
        [UnityTest]
        public IEnumerator StartupAndBothEntryScenesUseConfiguredFrameRate()
        {
            var config = Resources.Load<RuntimePerformanceConfig>(RuntimePerformanceConfig.ResourceName);
            Assert.That(config, Is.Not.Null, "The actual shipped config must be included in Resources.");
            Assert.That(config.TargetFrameRate, Is.EqualTo(60));
            var initialTarget = Application.targetFrameRate;
            var initialSync = QualitySettings.vSyncCount;
            var output = System.Environment.GetEnvironmentVariable("ARENA3Q_PERFORMANCE_EVIDENCE");
            var targets = new System.Collections.Generic.List<int>();
            var syncs = new System.Collections.Generic.List<int>();
            foreach (var scene in new[] { "Frontend", "Battle" })
            {
                yield return SceneManager.LoadSceneAsync(scene);
                yield return null;
                var battle = Object.FindAnyObjectByType<BattleController>();
                if (battle != null) battle.AutoSimulate = false;
                if (!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllText(Path.Combine(output, scene + ".json"),
                        $"{{\"startupTarget\":{initialTarget},\"startupVsync\":{initialSync},\"sceneTarget\":{Application.targetFrameRate},\"sceneVsync\":{QualitySettings.vSyncCount},\"configuredTarget\":{config.TargetFrameRate}}}");
                    var camera = Camera.main;
                    if (camera != null) Capture(camera, Path.Combine(output, scene + ".png"));
                }
                targets.Add(Application.targetFrameRate);
                syncs.Add(QualitySettings.vSyncCount);
            }
            Assert.That(targets, Is.All.EqualTo(config.TargetFrameRate), "Frontend and standalone Battle");
            Assert.That(syncs, Is.All.Zero, "Frontend and standalone Battle");
            Assert.That(initialTarget, Is.EqualTo(config.TargetFrameRate), "Before the first scene loads");
            Assert.That(initialSync, Is.Zero);
        }

        [Test]
        public void ConfigCannotRequestBelowSixty()
        {
            var config = ScriptableObject.CreateInstance<RuntimePerformanceConfig>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(config);
                serialized.FindProperty("targetFrameRate").intValue = 30;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(config.TargetFrameRate, Is.EqualTo(60));
                serialized.Update();
                serialized.FindProperty("targetFrameRate").intValue = 120;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(config.TargetFrameRate, Is.EqualTo(120), "Higher targets remain data driven.");
            }
            finally { Object.DestroyImmediate(config); }
        }

        private static void Capture(Camera camera, string path)
        {
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var rt = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                rt.Create();
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = active;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(image);
            }
        }
    }
}
#endif
