using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Device benchmark of a full 3D battle (docs/KeHoach/ChuyenSang3D.md §2, spec §1: at least 60 FPS). Development
    /// builds only, started with an Android intent extra (or a PC command-line argument):
    /// <c>adb shell am start -n com.Arena3Q.Arena3Q/&lt;activity&gt; --es arena3q_bench 1 [--es seconds 60]
    /// [--es warmup 8] [--es render_scale 0.75] [--es target_fps 120] [--es tag name]</c>.
    /// It opens the Battle scene, summons every card again and again, fires ultimates every few seconds (effects),
    /// restarts the battle when it ends, measures frame times, render counters and memory, writes
    /// <c>persistentDataPath/arena3q_bench_&lt;tag&gt;.json</c>, logs a <c>[Arena3Q] BENCH</c> line and quits.
    /// </summary>
    public sealed class BattleBenchmark : MonoBehaviour
    {
        private const string Flag = "arena3q_bench";
        private const float AverageFpsGate = 60f;   // spec §1
        private const float P95MsGate = 18f;        // plan §2

        [Serializable]
        private sealed class Result
        {
            public string tag, utc, device, gpu, graphicsApi, unity;
            public int width, height, frames, battleRestarts, heroes, troops, enemies;
            public int targetFps;
            public float renderScale, measuredSeconds, averageFps, medianMs, p95Ms, p99Ms, worstMs, framesOver16_7msPercent;
            public float drawCallsAverage, batchesAverage, setPassAverage, trianglesAverage;
            public long allocatedBytes, reservedBytes, graphicsDriverBytes;
            public float batteryCelsiusStart, batteryCelsiusEnd;
            public bool passedAverageFps, passedP95;
        }

        private Dictionary<string, string> _args;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
                return;
            var args = ReadArguments();
            if (!args.ContainsKey(Flag))
                return;
            var go = new GameObject("Battle benchmark");
            DontDestroyOnLoad(go);
            go.AddComponent<BattleBenchmark>()._args = args;
        }

        private float Number(string key, float fallback) =>
            _args.TryGetValue(key, out var text) && float.TryParse(text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : fallback;

        private IEnumerator Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // "target_fps" lifts the 60 cap to see the headroom (the game itself always runs at its configured rate).
            if (_args.ContainsKey("target_fps"))
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = Mathf.RoundToInt(Number("target_fps", 60f));
            }
            var result = new Result
            {
                tag = _args.TryGetValue("tag", out var tag) && !string.IsNullOrEmpty(tag) ? tag : "battle",
                utc = DateTime.UtcNow.ToString("o"), device = SystemInfo.deviceModel, gpu = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(), unity = Application.unityVersion,
                batteryCelsiusStart = BatteryCelsius(), targetFps = Application.targetFrameRate
            };
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                if (_args.ContainsKey("render_scale"))
                    pipeline.renderScale = Mathf.Clamp(Number("render_scale", 1f), 0.25f, 2f);
                result.renderScale = pipeline.renderScale;
            }

            SceneManager.LoadScene("Battle");
            BattleController battle = null;
            while (battle == null || battle.Stage == null)
            {
                yield return null;
                battle = FindAnyObjectByType<BattleController>();
            }

            // The battle scene applies the configured frame rate again on load; the benchmark's own cap wins.
            if (_args.ContainsKey("target_fps"))
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = Mathf.RoundToInt(Number("target_fps", 60f));
                result.targetFps = Application.targetFrameRate;
            }
            var warmup = Number("warmup", 8f);
            var seconds = Number("seconds", 60f);
            var frameMs = new List<float>(4096);
            using var drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            using var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            double sumDraw = 0, sumBatch = 0, sumSetPass = 0, sumTriangles = 0;
            var counted = 0;
            var elapsed = 0f;
            var nextSummon = 0f;
            var nextUltimate = 4f;
            var lastOver = false;
            while (elapsed < warmup + seconds)
            {
                yield return null;
                var dt = Time.unscaledDeltaTime;
                elapsed += dt;
                if (battle.IsOver)
                {
                    // A battle that ends is started again so the workload stays a full battle.
                    if (!lastOver)
                        result.battleRestarts++;
                    lastOver = true;
                    battle.Restart();
                    continue;
                }
                lastOver = false;
                if (elapsed >= nextSummon)
                {
                    nextSummon = elapsed + 1f;
                    for (var card = 0; card < battle.Cards.Count; card++)
                        battle.TrySummon(SummonRequest.Quick(card));
                }
                if (elapsed >= nextUltimate)
                {
                    // Effects in the workload: every hero with an ultimate fires it.
                    nextUltimate = elapsed + 6f;
                    foreach (var hero in battle.LivingHeroes.ToList())
                    {
                        if (!hero.Skills.HasUltimate)
                            continue;
                        hero.Skills.Rage.Add(hero.Skills.Rage.Max);
                        hero.Skills.TryUltimate(null);
                    }
                }
                if (elapsed < warmup)
                    continue;
                frameMs.Add(dt * 1000f);
                if (drawCalls.Valid && drawCalls.Count > 0)
                {
                    sumDraw += drawCalls.LastValue;
                    sumBatch += batches.Valid ? batches.LastValue : 0;
                    sumSetPass += setPass.Valid ? setPass.LastValue : 0;
                    sumTriangles += triangles.Valid ? triangles.LastValue : 0;
                    counted++;
                }
            }

            frameMs.Sort();
            result.frames = frameMs.Count;
            result.measuredSeconds = frameMs.Sum() / 1000f;
            result.averageFps = result.frames / Mathf.Max(0.001f, result.measuredSeconds);
            result.medianMs = Percentile(frameMs, 0.5f);
            result.p95Ms = Percentile(frameMs, 0.95f);
            result.p99Ms = Percentile(frameMs, 0.99f);
            result.worstMs = frameMs.Count > 0 ? frameMs[frameMs.Count - 1] : 0f;
            result.framesOver16_7msPercent = 100f * frameMs.Count(ms => ms > 1000f / 60f + 0.5f) / Mathf.Max(1, frameMs.Count);
            if (counted > 0)
            {
                result.drawCallsAverage = (float)(sumDraw / counted);
                result.batchesAverage = (float)(sumBatch / counted);
                result.setPassAverage = (float)(sumSetPass / counted);
                result.trianglesAverage = (float)(sumTriangles / counted);
            }
            result.width = Screen.width;
            result.height = Screen.height;
            result.heroes = battle.Registry.Units.Count(u => u is HeroUnit);
            result.troops = battle.Registry.Units.Count(u => u is TroopUnit);
            result.enemies = battle.Registry.Units.Count(u => u is EnemyUnit);
            result.allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            result.reservedBytes = Profiler.GetTotalReservedMemoryLong();
            result.graphicsDriverBytes = Profiler.GetAllocatedMemoryForGraphicsDriver();
            result.batteryCelsiusEnd = BatteryCelsius();
            result.passedAverageFps = result.averageFps >= AverageFpsGate - 0.5f;
            result.passedP95 = result.p95Ms <= P95MsGate;

            var json = JsonUtility.ToJson(result, true);
            var path = Path.Combine(Application.persistentDataPath, $"arena3q_bench_{result.tag}.json");
            File.WriteAllText(path, json);
            Debug.Log("[Arena3Q] BENCH " + JsonUtility.ToJson(result));
            Debug.Log("[Arena3Q] BENCH_DONE " + path);
            yield return null;
            Application.Quit();
        }

        private static float Percentile(List<float> sorted, float q) =>
            sorted.Count == 0 ? 0f : sorted[Mathf.Clamp(Mathf.CeilToInt(q * sorted.Count) - 1, 0, sorted.Count - 1)];

        private static Dictionary<string, string> ReadArguments()
        {
            var args = new Dictionary<string, string>();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = activity.Call<AndroidJavaObject>("getIntent");
                foreach (var key in new[] { Flag, "seconds", "warmup", "render_scale", "tag", "target_fps" })
                {
                    var value = intent.Call<string>("getStringExtra", key);
                    if (value != null)
                        args[key] = value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Arena3Q] Benchmark arguments unreadable: " + e.Message);
            }
#else
            var line = Environment.GetCommandLineArgs();
            for (var i = 0; i < line.Length; i++)
            {
                if (!line[i].StartsWith("-arena3q_", StringComparison.Ordinal))
                    continue;
                var key = line[i].Substring(1) == "arena3q_bench" ? Flag : line[i].Substring("-arena3q_".Length);
                args[key] = i + 1 < line.Length && !line[i + 1].StartsWith("-", StringComparison.Ordinal) ? line[i + 1] : "1";
            }
#endif
            return args;
        }

        private static float BatteryCelsius()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var filter = new AndroidJavaObject("android.content.IntentFilter", "android.intent.action.BATTERY_CHANGED");
                using var status = activity.Call<AndroidJavaObject>("registerReceiver", null, filter);
                return status != null ? status.Call<int>("getIntExtra", "temperature", 0) / 10f : 0f;
            }
            catch
            {
                return 0f;
            }
#else
            return 0f;
#endif
        }
    }
}
