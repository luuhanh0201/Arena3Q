using UnityEngine;

namespace Arena3Q.Data
{
    [CreateAssetMenu(menuName = "Arena3Q/Runtime Performance")]
    public sealed class RuntimePerformanceConfig : ScriptableObject
    {
        public const string ResourceName = "RuntimePerformance";
        public const int MinimumFrameRate = 60;

        [SerializeField, Min(MinimumFrameRate)] private int targetFrameRate = MinimumFrameRate;

        public int TargetFrameRate => Mathf.Max(MinimumFrameRate, targetFrameRate);

        private void OnValidate() => targetFrameRate = TargetFrameRate;
    }
}
