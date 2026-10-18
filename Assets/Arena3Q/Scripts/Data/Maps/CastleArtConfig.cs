using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// Pictures of a castle and its destruction: the intact sprite while it stands, then a short animation (shake and
    /// flash, dust and debris, swap to the ruined sprite, smoke) when it falls; the ruin stays on the field. Castles
    /// reference it from <see cref="CastleConfig"/>; both sides can share one.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Castle Art", fileName = "CastleArt")]
    public sealed class CastleArtConfig : ScriptableObject
    {
        [Tooltip("Standing castle (pivot on the ground, real size).")]
        [SerializeField] private Sprite intactSprite;
        [Tooltip("What is left after it falls (same pivot).")]
        [SerializeField] private Sprite destroyedSprite;

        [Header("Destruction animation (seconds, logic units)")]
        [SerializeField, Min(0f)] private float shakeSeconds = 0.8f;
        [SerializeField, Min(0f)] private float shakeAmplitude = 0.12f;
        [Tooltip("The ruin drops this far and settles back after the swap.")]
        [SerializeField, Min(0f)] private float collapseDrop = 0.25f;
        [SerializeField, Min(0.05f)] private float collapseSeconds = 0.4f;
        [SerializeField, Min(0)] private int dustPuffs = 14;
        [SerializeField, Min(0)] private int debrisPieces = 12;
        [SerializeField, Min(0f)] private float smokeSeconds = 6f;
        [SerializeField, Min(0.05f)] private float smokeInterval = 0.35f;
        [SerializeField] private Color flashColor = new Color(1f, 0.55f, 0.45f);
        [SerializeField] private Color dustColor = new Color(0.78f, 0.66f, 0.48f, 0.85f);
        [SerializeField] private Color debrisColor = new Color(0.42f, 0.27f, 0.15f);
        [SerializeField] private Color smokeColor = new Color(0.25f, 0.23f, 0.22f, 0.55f);
        [Tooltip("The result screen waits this long after the castle falls, so the animation can be seen.")]
        [SerializeField, Min(0f)] private float resultDelaySeconds = 1.8f;

        public Sprite IntactSprite => intactSprite;
        public Sprite DestroyedSprite => destroyedSprite;
        public float ShakeSeconds => shakeSeconds;
        public float ShakeAmplitude => shakeAmplitude;
        public float CollapseDrop => collapseDrop;
        public float CollapseSeconds => collapseSeconds;
        public int DustPuffs => dustPuffs;
        public int DebrisPieces => debrisPieces;
        public float SmokeSeconds => smokeSeconds;
        public float SmokeInterval => smokeInterval;
        public Color FlashColor => flashColor;
        public Color DustColor => dustColor;
        public Color DebrisColor => debrisColor;
        public Color SmokeColor => smokeColor;
        public float ResultDelaySeconds => resultDelaySeconds;
    }
}
