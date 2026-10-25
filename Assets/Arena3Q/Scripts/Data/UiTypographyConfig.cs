using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>Explicit embedded font for browser UI, where operating system fallback fonts are unavailable.</summary>
    [CreateAssetMenu(menuName = "Arena3Q/UI/Typography")]
    public sealed class UiTypographyConfig : ScriptableObject
    {
        public const string ResourceName = "UiTypography";
        public Font browserFont;
    }
}
