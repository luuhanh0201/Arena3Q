using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// What a skill does once it lands, shared by hero skills (<see cref="SkillDefinition"/>) and enemy skills
    /// (<see cref="EnemySkillDefinition"/>): the area, how long it stays, repeats, the caster's movement and a list of
    /// effects. The battle code only knows these blocks, so a new skill or a retune is a data change. Damage is a
    /// fraction of the caster's Công (heroes: grows with the skill level).
    /// </summary>
    public abstract class SkillAreaDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Effect
        {
            public SkillEffectKind kind = SkillEffectKind.Damage;
            [Tooltip("Relative to the caster: Enemies = the other side, Allies = its own side (castles excluded), Self.")]
            public SkillAffects affects = SkillAffects.Enemies;
            [Tooltip("Damage/Poison: fraction of Công (Poison per second). Shield: fraction of the caster's max health. " +
                     "Heal: fraction of the target's max health. Slow, Weaken, …Up/…Down: fraction. Knockback/Pull: " +
                     "distance. RefillSquad: soldiers.")]
            public float value;
            [Tooltip("Seconds, for statuses.")]
            public float duration;
        }

        [Header("Identity")]
        [SerializeField] private string skillId = "skill";
        [SerializeField] private string displayName = "Skill";
        [SerializeField, TextArea] private string description = "";
        [Tooltip("Card icon (icon_<hero>_<slot>.png); empty = a coloured placeholder. Also tints the placeholder area.")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Color iconColor = new Color(0.6f, 0.6f, 0.7f);

        [Header("Use")]
        [Tooltip("Seconds between two automatic uses. Hero ultimates use rage instead.")]
        [SerializeField, Min(0f)] private float cooldown = 10f;
        [Tooltip("Wind-up before the effect (the animation's Cast mark). Stunned or dead before it = not used.")]
        [SerializeField, Min(0f)] private float castTime = 0.3f;
        [Tooltip("How far the skill reaches when aiming; 0 = the caster's attack range.")]
        [SerializeField, Min(0f)] private float range;
        [SerializeField] private CastCondition condition = CastCondition.HasTarget;
        [SerializeField] private CasterMove casterMove = CasterMove.None;
        [SerializeField, Min(0f)] private float moveDistance;

        [Header("Area")]
        [SerializeField] private SkillShape shape = SkillShape.Single;
        [SerializeField, Min(0f)] private float radius = 1.5f;
        [SerializeField, Min(0f)] private float length = 5f;
        [SerializeField, Min(0f)] private float width = 1f;
        [SerializeField, Range(1f, 360f)] private float angle = 60f;
        [Tooltip("Line: stop at the first enemy hit.")]
        [SerializeField] private bool firstHitOnly;
        [Tooltip("Damage multiplier against castles (e.g. ×3 for the fire arrow).")]
        [SerializeField, Min(0f)] private float castleMultiplier = 1f;

        [Header("Over time")]
        [Tooltip("Seconds the area stays on the ground, hitting every tick; 0 = one hit.")]
        [SerializeField, Min(0f)] private float zoneDuration;
        [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;
        [Tooltip("The area moves with the caster (spinning blades, auras).")]
        [SerializeField] private bool followCaster;
        [Tooltip("The hit repeats this many times (3 arrows, 6 slashes); the caster keeps still meanwhile.")]
        [SerializeField, Min(1)] private int repeatCount = 1;
        [SerializeField, Min(0f)] private float repeatInterval = 0.25f;
        [Tooltip("Damage multiplier of the last repeat (a finishing blow).")]
        [SerializeField, Min(0f)] private float finalHitMultiplier = 1f;

        [Header("Effects")]
        [SerializeField] private Effect[] effects = Array.Empty<Effect>();

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color IconColor => iconColor;
        public float Cooldown => cooldown;
        public float CastTime => castTime;
        public float Range => range;
        public CastCondition Condition => condition;
        public CasterMove CasterMove => casterMove;
        public float MoveDistance => moveDistance;
        public SkillShape Shape => shape;
        public float Radius => radius;
        public float Length => length;
        public float Width => width;
        public float Angle => angle;
        public bool FirstHitOnly => firstHitOnly;
        public float CastleMultiplier => castleMultiplier;
        public float ZoneDuration => zoneDuration;
        public float TickInterval => tickInterval;
        public bool FollowCaster => followCaster;
        public int RepeatCount => Mathf.Max(1, repeatCount);
        public float RepeatInterval => repeatInterval;
        public float FinalHitMultiplier => finalHitMultiplier;
        public IReadOnlyList<Effect> Effects => effects;

        public virtual void Validate(List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(skillId)) errors.Add($"Skill '{name}': skillId is empty.");
        }

        protected bool HasEffects => effects != null && effects.Length > 0;
    }
}
