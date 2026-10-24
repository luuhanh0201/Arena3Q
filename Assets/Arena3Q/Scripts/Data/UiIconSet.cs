using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// Icons the in-game UI shows in place of Unicode symbols (docs/QuyTac/ThietKeUI.md: no emoji or symbol characters,
    /// icons come from a dedicated icon library). Sprites are white monochrome so the UI tints them with
    /// <c>Image.color</c>. Loaded from Resources.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/UI Icon Set")]
    public sealed class UiIconSet : ScriptableObject
    {
        public const string ResourceName = "UiIcons";

        [Tooltip("Star: hero star rank, stars needed to unlock a skill, recommended server (Lucide 'star', ISC).")]
        [SerializeField] private Sprite star;

        public Sprite Star => star;

        private static UiIconSet _loaded;

        /// <summary>The project's icon set, or null when the asset is missing (the UI then shows no icon).</summary>
        public static UiIconSet Load()
        {
            if (_loaded == null)
                _loaded = Resources.Load<UiIconSet>(ResourceName);
            return _loaded;
        }
    }
}
