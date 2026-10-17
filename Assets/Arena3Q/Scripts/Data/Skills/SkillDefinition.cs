using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// One skill of a hero (spec §3 "Kỹ năng trên thẻ"), built from shared blocks: how it aims, the area it covers,
    /// how long it lasts and a list of effects. A new skill or a retune is a data change; the battle code only knows the
    /// blocks. Damage is a fraction of the caster's Công and grows with the skill level (ProgressionConfig).
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Skill Definition", fileName = "Skill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Effect
        {
            public SkillEffectKind kind = SkillEffectKind.Damage;
            public SkillAffects affects = SkillAffects.Enemies;
            [Tooltip("Damage/Poison: fraction of Công (Poison per second). Shield: fraction of the caster's max health. " +
                     "Slow, Weaken, …Up/…Down: fraction. Knockback: distance. RefillSquad: soldiers.")]
            public float value;
            [Tooltip("Seconds, for statuses.")]
            public float duration;
        }

        [Serializable]
        public sealed class Passive
        {
            public PassiveKind kind;
            public float value;
            public float secondValue;
            public float threshold;
            public float radius;
            public int count;
        }

        [Header("Identity")]
        [SerializeField] private string skillId = "skill";
        [SerializeField] private string displayName = "Skill";
        [SerializeField] private SkillSlot slot = SkillSlot.Skill2;
        [SerializeField, TextArea] private string description = "";
        [Tooltip("Card icon (icon_<hero>_<slot>.png); empty = a coloured placeholder.")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Color iconColor = new Color(0.6f, 0.6f, 0.7f);

        [Header("Use")]
        [Tooltip("Seconds between two auto casts (skill 2 and 3). Ultimates use rage instead.")]
        [SerializeField, Min(0f)] private float cooldown = 10f;
        [Tooltip("Wind-up before the effect (the animation's Cast mark). Stunned or dead before it = not used.")]
        [SerializeField, Min(0f)] private float castTime = 0.3f;
        [SerializeField] private SkillTargeting targeting = SkillTargeting.CurrentTarget;
        [Tooltip("How far the skill reaches when aiming; 0 = the hero's attack range.")]
        [SerializeField, Min(0f)] private float range;
        [SerializeField] private CastCondition condition = CastCondition.HasTarget;
        [SerializeField] private CasterMove casterMove = CasterMove.None;
        [SerializeField, Min(0f)] private float moveDistance;
        [Tooltip("Skills that move the hero: also auto-cast while it walks to a move order (spec §3: off = settled rule, the hero keeps walking).")]
        [SerializeField] private bool autoCastWhileMoving;

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
        [Tooltip("The area moves with the hero (spinning blades, auras).")]
        [SerializeField] private bool followCaster;
        [Tooltip("The hit repeats this many times (3 arrows, 6 slashes); the hero keeps still meanwhile.")]
        [SerializeField, Min(1)] private int repeatCount = 1;
        [SerializeField, Min(0f)] private float repeatInterval = 0.25f;
        [Tooltip("Damage multiplier of the last repeat (a finishing blow).")]
        [SerializeField, Min(0f)] private float finalHitMultiplier = 1f;

        [Header("Effects")]
        [SerializeField] private Effect[] effects = Array.Empty<Effect>();
        [Tooltip("Passive (kỹ năng 1) only.")]
        [SerializeField] private Passive[] passives = Array.Empty<Passive>();

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public SkillSlot Slot => slot;
        public string Description => description;
        public Sprite Icon => icon;
        public Color IconColor => iconColor;
        public float Cooldown => cooldown;
        public float CastTime => castTime;
        public SkillTargeting Targeting => targeting;
        public float Range => range;
        public CastCondition Condition => condition;
        public CasterMove CasterMove => casterMove;
        public float MoveDistance => moveDistance;
        public bool AutoCastWhileMoving => autoCastWhileMoving;
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
        public IReadOnlyList<Passive> Passives => passives;

        public bool IsPassive => slot == SkillSlot.Passive;
        public bool IsUltimate => slot == SkillSlot.Ultimate;

        public void Validate(List<string> errors)
        {
            var owner = $"Skill '{name}'";
            if (string.IsNullOrWhiteSpace(skillId)) errors.Add($"{owner}: skillId is empty.");
            if (IsPassive)
            {
                if (passives == null || passives.Length == 0) errors.Add($"{owner}: a passive needs passive effects.");
                return;
            }
            if (effects == null || effects.Length == 0) errors.Add($"{owner}: no effects.");
            if (slot != SkillSlot.Ultimate && cooldown <= 0f) errors.Add($"{owner}: skills 2 and 3 need a cooldown.");
            if (shape == SkillShape.Single && targeting != SkillTargeting.CurrentTarget && targeting != SkillTargeting.AllyNeedingSoldiers)
                errors.Add($"{owner}: a single-target skill must aim at a unit (the current target or an ally).");
        }
    }
}
