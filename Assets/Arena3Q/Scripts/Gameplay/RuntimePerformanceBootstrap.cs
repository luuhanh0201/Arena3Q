using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    public static class RuntimePerformanceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var config = Resources.Load<RuntimePerformanceConfig>(RuntimePerformanceConfig.ResourceName);
            if (config == null)
                Debug.LogError("[Arena3Q] Missing RuntimePerformance config; using the required minimum frame rate.");
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = config != null ? config.TargetFrameRate : RuntimePerformanceConfig.MinimumFrameRate;
        }
    }
}
