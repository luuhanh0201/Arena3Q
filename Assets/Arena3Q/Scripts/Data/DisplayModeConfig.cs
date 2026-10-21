using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// How the game uses the phone screen (user request 2026-10-21): full screen while playing; the system bars
    /// (status bar, navigation buttons) show only when the player swipes for them and hide again after a moment, also
    /// after coming back from Home or the recent-apps screen. Loaded from Resources at startup.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Display Mode")]
    public sealed class DisplayModeConfig : ScriptableObject
    {
        public const string ResourceName = "DisplayMode";

        [Tooltip("Keep the game full screen (Android immersive mode).")]
        [SerializeField] private bool immersive = true;
        [Tooltip("Seconds the system bars may stay after the player swiped them in, before the game hides them again.")]
        [SerializeField, Min(0.5f)] private float hideBarsAfterSeconds = 3f;
        [Tooltip("How often the game checks whether the bars are showing.")]
        [SerializeField, Min(0.2f)] private float checkIntervalSeconds = 1f;

        public bool Immersive => immersive;
        public float HideBarsAfterSeconds => hideBarsAfterSeconds;
        public float CheckIntervalSeconds => checkIntervalSeconds;
    }
}
