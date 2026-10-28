using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// Look and timing of the army stance button (spec §5 "Cơ chế mới", mockup NghiemThu/UI/NutLenhToanQuan_20261028):
    /// one button in the bottom-right corner shows the active stance; a tap or an upward swipe opens the other two
    /// stacked above it. Sizes are HUD units (reference 1920×1080). Loaded from Resources.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Stance Button")]
    public sealed class StanceButtonConfig : ScriptableObject
    {
        public const string ResourceName = "StanceButton";

        [Header("Names (in-game text, docs/QuyTac/ThietKeUI.md)")]
        [SerializeField] private string holdName = "Giữ";
        [SerializeField] private string attackName = "Tấn công";
        [SerializeField] private string defendName = "Phòng thủ";
        [Tooltip("Short line under the timer after a change; {0} = the stance name.")]
        [SerializeField] private string changedMessage = "Toàn quân: {0}";

        [Header("Layout (HUD units)")]
        [SerializeField] private Vector2 buttonSize = new Vector2(112f, 104f);
        [Tooltip("Gap between the main button and the options above it.")]
        [SerializeField, Min(0f)] private float spacing = 10f;
        [Tooltip("Distance from the bottom-right corner of the safe area (x from the right, y from the bottom).")]
        [SerializeField] private Vector2 cornerMargin = new Vector2(30f, 16f);
        [SerializeField, Min(8f)] private float iconSize = 40f;
        [SerializeField, Min(8)] private int labelSize = 24;
        [SerializeField, Min(4f)] private float expandHintSize = 18f;

        [Header("Colours")]
        [SerializeField] private Color mainColour = new Color(0.91f, 0.72f, 0.31f, 1f);
        [SerializeField] private Color mainTextColour = new Color(0.1f, 0.06f, 0f, 1f);
        [SerializeField] private Color optionColour = new Color(0.055f, 0.086f, 0.157f, 0.86f);
        [SerializeField] private Color optionBorderColour = new Color(0.84f, 0.7f, 0.37f, 0.45f);
        [SerializeField] private Color optionTextColour = new Color(0.94f, 0.95f, 0.97f, 0.92f);
        [SerializeField] private Color optionHighlightColour = new Color(0.2f, 0.25f, 0.36f, 0.95f);
        [SerializeField] private Color messageColour = new Color(0.95f, 0.82f, 0.48f, 1f);

        [Header("Timing (seconds, real time)")]
        [Tooltip("Options slide and fade in / out (effects never pop).")]
        [SerializeField, Min(0.01f)] private float expandSeconds = 0.15f;
        [Tooltip("Open options close by themselves after this long without a choice.")]
        [SerializeField, Min(0.5f)] private float autoCollapseSeconds = 3f;
        [SerializeField, Min(0.1f)] private float messageSeconds = 1.2f;
        [SerializeField, Min(0.01f)] private float messageFadeSeconds = 0.4f;
        [Tooltip("Upward drag on the main button that counts as a swipe to open, in HUD units.")]
        [SerializeField, Min(1f)] private float swipeOpenDistance = 24f;

        public Vector2 ButtonSize => buttonSize;
        public float Spacing => spacing;
        public Vector2 CornerMargin => cornerMargin;
        public float IconSize => iconSize;
        public int LabelSize => labelSize;
        public float ExpandHintSize => expandHintSize;
        public Color MainColour => mainColour;
        public Color MainTextColour => mainTextColour;
        public Color OptionColour => optionColour;
        public Color OptionBorderColour => optionBorderColour;
        public Color OptionTextColour => optionTextColour;
        public Color OptionHighlightColour => optionHighlightColour;
        public Color MessageColour => messageColour;
        public float ExpandSeconds => expandSeconds;
        public float AutoCollapseSeconds => autoCollapseSeconds;
        public float MessageSeconds => messageSeconds;
        public float MessageFadeSeconds => messageFadeSeconds;
        public float SwipeOpenDistance => swipeOpenDistance;

        public string NameOf(ArmyStance stance)
        {
            switch (stance)
            {
                case ArmyStance.Attack: return attackName;
                case ArmyStance.Defend: return defendName;
                default: return holdName;
            }
        }

        public string ChangedMessage(ArmyStance stance) => string.Format(changedMessage, NameOf(stance));

        private static StanceButtonConfig _loaded;

        /// <summary>The project's stance button settings, or the defaults when the asset is missing.</summary>
        public static StanceButtonConfig Load()
        {
            if (_loaded == null)
                _loaded = Resources.Load<StanceButtonConfig>(ResourceName);
            if (_loaded == null)
            {
                _loaded = CreateInstance<StanceButtonConfig>();
                _loaded.hideFlags = HideFlags.DontSave;
            }
            return _loaded;
        }
    }
}
