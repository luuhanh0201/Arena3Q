using Arena3Q.Data;
using UnityEngine;
using UnityEngine.Scripting;

namespace Arena3Q.UI
{
    /// <summary>The web wrapper supplies a device-like safe area and dpi without changing saved profiles.</summary>
    [Preserve]
    public sealed class WebDisplayPreview : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var root = new GameObject(nameof(WebDisplayPreview));
            root.AddComponent<WebDisplayPreview>(); DontDestroyOnLoad(root);
#endif
        }
        [Preserve]
        public void Apply(string json)
        {
            try
            {
                var value = JsonUtility.FromJson<DevicePreviewSettings>(json);
                if (DisplayMetrics.TrySetPreview(value) && value != null && value.frameRate > 0)
                {
                    int refresh = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
                    // An unknown display cannot establish support for the high preset.
                    var fallback = Resources.Load<RuntimePerformanceConfig>(RuntimePerformanceConfig.ResourceName);
                    int ceiling = refresh > 0 ? refresh : fallback != null ? fallback.TargetFrameRate : value.frameRate;
                    Application.targetFrameRate = Mathf.Min(value.frameRate, ceiling);
                }
            }
            catch (System.ArgumentException) { /* Invalid wrapper data must not disturb native metrics. */ }
        }
    }
}
