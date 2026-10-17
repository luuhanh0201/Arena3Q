using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// Every number of the level / star / skill-point / breakthrough system (spec §2 "Cấp, sao và phe",
    /// docs/KeHoach/HeroProgression.md). Field defaults are the design numbers; edit the asset to retune.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Progression/Progression Config", fileName = "ProgressionConfig")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        [Serializable]
        public sealed class StarRow
        {
            [Min(1)] public int stars = 1;
            [Tooltip("Added to Máu and Công: 0.1 = +10 %.")]
            [Min(0f)] public float statBonus;
            [Tooltip("Skills available in slot order: 2 = passive + skill 2, 4 = all four.")]
            [Range(0, 4)] public int unlockedSkillCount = 2;
            [Tooltip("Level of every unlocked skill.")]
            [Min(1)] public int skillLevel = 1;
            [Tooltip("Shards needed to reach this star for a named hero (shop / rewards, not used in battle).")]
            [Min(0)] public int shardsNamed;
            [Tooltip("Shards needed for a starter hero.")]
            [Min(0)] public int shardsStarter;

            public StarRow()
            {
            }

            public StarRow(int stars, float statBonus, int unlockedSkillCount, int skillLevel, int shardsNamed, int shardsStarter)
            {
                this.stars = stars;
                this.statBonus = statBonus;
                this.unlockedSkillCount = unlockedSkillCount;
                this.skillLevel = skillLevel;
                this.shardsNamed = shardsNamed;
                this.shardsStarter = shardsStarter;
            }
        }

        [Header("Levels")]
        [SerializeField, Min(1)] private int maxLevel = 60;
        [Tooltip("Squad soldiers grow with their hero's level (Máu and Công, compounding).")]
        [SerializeField, Range(0f, 0.2f)] private float troopGrowthPerLevel = 0.03f;
        [Tooltip("Enemies grow with the stage's enemy level (Máu and Công, compounding).")]
        [SerializeField, Range(0f, 0.2f)] private float enemyGrowthPerLevel = 0.03f;

        [Header("Stars (starter and named heroes)")]
        [SerializeField] private StarRow[] stars =
        {
            new StarRow(1, 0f, 2, 1, 10, 5),
            new StarRow(2, 0.1f, 3, 4, 20, 10),
            new StarRow(3, 0.2f, 4, 7, 40, 20),
            new StarRow(4, 0.3f, 4, 11, 80, 40),
            new StarRow(5, 0.4f, 4, 15, 160, 80)
        };

        [Header("Lord skills")]
        [Tooltip("Lord level that unlocks the passive, skill 2, skill 3 and the ultimate.")]
        [SerializeField] private int[] lordSkillUnlockLevels = { 2, 5, 10, 15 };
        [SerializeField, Min(1)] private int skillPointsFromLevel = 5;
        [SerializeField, Min(0)] private int skillPointsPerLevel = 1;
        [Tooltip("A lord skill cannot go above lord level ÷ this, rounded up.")]
        [SerializeField, Min(1)] private int skillLevelCapDivisor = 4;
        [SerializeField, Min(1)] private int maxSkillLevel = 15;
        [Tooltip("Each skill level above 1 adds this to the skill's damage / heal / shield.")]
        [SerializeField, Min(0f)] private float skillValueBonusPerLevel = 0.05f;

        [Header("Lord breakthroughs")]
        [SerializeField] private int[] breakthroughLevels = { 15, 30, 45, 60 };
        [Tooltip("Added to Máu and Công per breakthrough.")]
        [SerializeField, Min(0f)] private float breakthroughStatBonus = 0.1f;
        [Tooltip("On: a lord breaks through as soon as it reaches the level. Off: breakthroughs must be earned (item, quest).")]
        [SerializeField] private bool automaticBreakthroughs = true;

        [Header("Combat")]
        [SerializeField, Range(0f, 0.95f)] private float maxDamageReduction = 0.75f;
        [Tooltip("Crit chance every hero starts with (heroes can add their own bonus).")]
        [SerializeField, Range(0f, 1f)] private float heroBaseCritChance = 0.05f;
        [SerializeField, Min(1f)] private float critMultiplier = 1.5f;
        [Tooltip("Spec §8: a hero stunned without a break for longer than this gets crowd-control immunity (0 = off).")]
        [SerializeField, Min(0f)] private float crowdControlLockSeconds = 2f;
        [Tooltip("Length of that immunity.")]
        [SerializeField, Min(0f)] private float crowdControlBreakSeconds = 2f;

        [Header("Combat power (menus)")]
        [SerializeField, Min(0f)] private float combatPowerScale = 10f;
        [SerializeField, Min(0f)] private float combatPowerPerSkillLevel = 0.004f;

        [Header("Balance reference (used by balance checks, not in battle)")]
        [Tooltip("The 'standard hero' every class is compared with: Máu, Công, attacks per second.")]
        [SerializeField, Min(1f)] private float referenceHealth = 900f;
        [SerializeField, Min(0.1f)] private float referenceDamage = 45f;
        [SerializeField, Min(0.1f)] private float referenceAttacksPerSecond = 1f;
        [Tooltip("Growth of Công and Máu of the standard hero; every growth profile should match its Công × Máu growth.")]
        [SerializeField, Range(0f, 0.2f)] private float referenceGrowthPerLevel = 0.03f;

        public int MaxLevel => maxLevel;
        public float ReferenceHealth => referenceHealth;
        public float ReferenceDamage => referenceDamage;
        public float ReferenceAttacksPerSecond => referenceAttacksPerSecond;
        public float ReferenceGrowthPerLevel => referenceGrowthPerLevel;
        public IReadOnlyList<StarRow> Stars => stars;

        /// <summary>√(DPS × máu) of the standard hero at a level: the yardstick of the balance checks.</summary>
        public float ReferenceStrength(int level) =>
            Mathf.Sqrt(referenceDamage * referenceAttacksPerSecond * referenceHealth) *
            ProgressionRules.GrowthMultiplier(referenceGrowthPerLevel, level);

        public HeroProgressionSettings CreateSettings()
        {
            var starRules = new List<StarRule>();
            if (stars != null)
            {
                foreach (var row in stars)
                {
                    if (row != null)
                        starRules.Add(new StarRule(row.stars, row.statBonus, row.unlockedSkillCount, row.skillLevel));
                }
            }

            return new HeroProgressionSettings
            {
                MaxLevel = maxLevel,
                TroopGrowthPerLevel = troopGrowthPerLevel,
                EnemyGrowthPerLevel = enemyGrowthPerLevel,
                Stars = starRules.Count > 0 ? starRules : HeroProgressionSettings.DefaultStars,
                LordSkillUnlockLevels = lordSkillUnlockLevels != null ? (int[])lordSkillUnlockLevels.Clone() : Array.Empty<int>(),
                SkillPointsFromLevel = skillPointsFromLevel,
                SkillPointsPerLevel = skillPointsPerLevel,
                SkillLevelCapDivisor = skillLevelCapDivisor,
                MaxSkillLevel = maxSkillLevel,
                SkillValueBonusPerLevel = skillValueBonusPerLevel,
                BreakthroughLevels = breakthroughLevels != null ? (int[])breakthroughLevels.Clone() : Array.Empty<int>(),
                BreakthroughStatBonus = breakthroughStatBonus,
                AutomaticBreakthroughs = automaticBreakthroughs,
                MaxDamageReduction = maxDamageReduction,
                HeroBaseCritChance = heroBaseCritChance,
                CritMultiplier = critMultiplier,
                CrowdControlLockSeconds = crowdControlLockSeconds,
                CrowdControlBreakSeconds = crowdControlBreakSeconds,
                CombatPowerScale = combatPowerScale,
                CombatPowerPerSkillLevel = combatPowerPerSkillLevel
            };
        }

        public ProgressionRules CreateRules() => new ProgressionRules(CreateSettings());

        public void Validate(List<string> errors)
        {
            var owner = $"Progression '{name}'";
            if (maxLevel < 1) errors.Add($"{owner}: maxLevel must be >= 1.");
            if (stars == null || stars.Length == 0)
                errors.Add($"{owner}: needs at least one star row.");
            else
            {
                for (var i = 0; i < stars.Length; i++)
                {
                    var row = stars[i];
                    if (row == null || row.stars != i + 1)
                        errors.Add($"{owner}: star row {i} must describe {i + 1} star(s).");
                    else if (i > 0 && (row.statBonus < stars[i - 1].statBonus || row.skillLevel < stars[i - 1].skillLevel ||
                                       row.unlockedSkillCount < stars[i - 1].unlockedSkillCount))
                        errors.Add($"{owner}: {row.stars} stars must not be weaker than {row.stars - 1}.");
                }
            }

            if (lordSkillUnlockLevels == null || lordSkillUnlockLevels.Length != 4)
                errors.Add($"{owner}: lordSkillUnlockLevels needs one level per skill slot (4).");
            else
            {
                for (var i = 1; i < lordSkillUnlockLevels.Length; i++)
                {
                    if (lordSkillUnlockLevels[i] < lordSkillUnlockLevels[i - 1])
                        errors.Add($"{owner}: lord skills must unlock in slot order.");
                }
            }

            if (breakthroughLevels == null)
                errors.Add($"{owner}: breakthroughLevels is missing.");
            if (maxSkillLevel < 1) errors.Add($"{owner}: maxSkillLevel must be >= 1.");
        }
    }
}
