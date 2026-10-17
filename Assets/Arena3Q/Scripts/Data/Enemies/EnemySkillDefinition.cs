using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// A skill of an elite or a boss (spec §8 ✅, docs/QuanDich): used on its own when the cooldown is over and a valid
    /// target is in range, never wasted. Area skills show a fixed red warning for <see cref="TelegraphSeconds"/> first;
    /// a single-target skill marks its target. Stunned or killed during the warning = cancelled, no cooldown. It may
    /// also call in soldiers or copies of the caster.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Enemy Skill Definition", fileName = "EnemySkill")]
    public sealed class EnemySkillDefinition : SkillAreaDefinition
    {
        [Header("Enemy")]
        [SerializeField] private EnemySkillAim aim = EnemySkillAim.CurrentTarget;
        [Tooltip("AroundPlayerUnits: how many areas (each on a different player unit while there are enough).")]
        [SerializeField, Min(1)] private int zoneCount = 1;
        [Tooltip("Extra areas beyond the player units in range land this far from one of them.")]
        [SerializeField, Min(0f)] private float zoneSpread = 1.5f;
        [Tooltip("Red warning before the effect (spec §8 🟡: 1 s for areas, 0.5 s mark for single targets); 0 = none.")]
        [SerializeField, Min(0f)] private float telegraphSeconds = 1f;
        [Tooltip("Cooldown left when the battle starts (the first use).")]
        [SerializeField, Min(0f)] private float initialCooldown = 3f;
        [Tooltip("The caster stands still this long after the effect (spinning, holding the shield).")]
        [SerializeField, Min(0f)] private float channelSeconds;

        [Header("Summon")]
        [Tooltip("Soldiers called in (they are normal enemies, spec §8).")]
        [SerializeField] private TroopDefinition summonUnit;
        [SerializeField, Min(0)] private int summonCount;
        [Tooltip("Spec §8 🟡: at most this many of this skill's summons alive at once.")]
        [SerializeField, Min(0)] private int summonLimit = 8;
        [Tooltip("Summon copies of the caster instead (Huyễn Ảnh): no skills, no boss bar.")]
        [SerializeField] private bool summonClones;
        [SerializeField, Range(0.01f, 1f)] private float cloneHealthFactor = 0.1f;
        [SerializeField, Range(0.01f, 1f)] private float cloneDamageFactor = 0.3f;
        [SerializeField, Min(0f)] private float summonRadius = 1.5f;

        public EnemySkillAim Aim => aim;
        public int ZoneCount => Mathf.Max(1, zoneCount);
        public float ZoneSpread => zoneSpread;
        public float TelegraphSeconds => telegraphSeconds;
        public float InitialCooldown => initialCooldown;
        public float ChannelSeconds => channelSeconds;
        public TroopDefinition SummonUnit => summonUnit;
        public int SummonCount => summonCount;
        public int SummonLimit => summonLimit;
        public bool SummonClones => summonClones;
        public float CloneHealthFactor => cloneHealthFactor;
        public float CloneDamageFactor => cloneDamageFactor;
        public float SummonRadius => summonRadius;
        public bool Summons => summonCount > 0 && (summonClones || summonUnit != null);

        /// <summary>Single-unit aims (warning mark on the target instead of a red area).</summary>
        public bool AimsAtUnit => aim == EnemySkillAim.CurrentTarget || aim == EnemySkillAim.NearestHero ||
                                  aim == EnemySkillAim.RandomHero;

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            var owner = $"Enemy skill '{name}'";
            if (!HasEffects && !Summons) errors.Add($"{owner}: no effects and nothing summoned.");
            if (Shape == SkillShape.Single && !AimsAtUnit)
                errors.Add($"{owner}: a single-target skill must aim at a unit.");
            if (summonCount > 0 && !summonClones && summonUnit == null) errors.Add($"{owner}: summonCount without a summonUnit.");
            if (summonUnit is EnemyDefinition summoned && summoned.Tier != EnemyTier.Normal)
                errors.Add($"{owner}: summons are normal enemies (spec §8).");
        }
    }
}
