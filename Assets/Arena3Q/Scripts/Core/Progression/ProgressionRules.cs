using System;
using System.Collections.Generic;

namespace Arena3Q.Core
{
    /// <summary>
    /// Plain progression settings, built from <c>ProgressionConfig</c> by the Data layer. The defaults are the numbers of
    /// docs/KeHoach/HeroProgression.md, so code that runs without the asset behaves like the design.
    /// </summary>
    public sealed class HeroProgressionSettings
    {
        public int MaxLevel { get; set; } = 60;

        /// <summary>Squad soldiers grow with their hero's level at this rate (Máu and Công).</summary>
        public float TroopGrowthPerLevel { get; set; } = 0.03f;

        /// <summary>Enemies grow with the stage's enemy level at this rate (Máu and Công).</summary>
        public float EnemyGrowthPerLevel { get; set; } = 0.03f;

        /// <summary>Index 0 = 1 star.</summary>
        public IReadOnlyList<StarRule> Stars { get; set; } = DefaultStars;

        /// <summary>Lord level that unlocks each slot, in <see cref="SkillSlot"/> order.</summary>
        public IReadOnlyList<int> LordSkillUnlockLevels { get; set; } = new[] { 2, 5, 10, 15 };

        public int SkillPointsFromLevel { get; set; } = 5;
        public int SkillPointsPerLevel { get; set; } = 1;
        /// <summary>A lord skill cannot go above lord level ÷ this (rounded up).</summary>
        public int SkillLevelCapDivisor { get; set; } = 4;
        public int MaxSkillLevel { get; set; } = 15;
        /// <summary>Each skill level above 1 adds this much to the skill's damage / heal / shield.</summary>
        public float SkillValueBonusPerLevel { get; set; } = 0.05f;

        public IReadOnlyList<int> BreakthroughLevels { get; set; } = new[] { 15, 30, 45, 60 };
        public float BreakthroughStatBonus { get; set; } = 0.1f;
        /// <summary>True: a lord breaks through as soon as the level is reached; false: breakthroughs are earned separately.</summary>
        public bool AutomaticBreakthroughs { get; set; } = true;

        public float MaxDamageReduction { get; set; } = 0.75f;
        public float HeroBaseCritChance { get; set; } = 0.05f;
        public float CritMultiplier { get; set; } = 1.5f;
        /// <summary>Anti-lock (spec §8 🟡): a hero stunned without a break longer than this gets immunity…</summary>
        public float CrowdControlLockSeconds { get; set; } = 2f;
        /// <summary>…for this long.</summary>
        public float CrowdControlBreakSeconds { get; set; } = 2f;

        public float CombatPowerScale { get; set; } = 10f;
        public float CombatPowerPerSkillLevel { get; set; } = 0.004f;

        public static IReadOnlyList<StarRule> DefaultStars { get; } = new[]
        {
            new StarRule(1, 0f, 2, 1),
            new StarRule(2, 0.1f, 3, 4),
            new StarRule(3, 0.2f, 4, 7),
            new StarRule(4, 0.3f, 4, 11),
            new StarRule(5, 0.4f, 4, 15)
        };
    }

    /// <summary>
    /// Levels, stars, lord breakthroughs, skill unlocks and skill points, stat scaling and damage rules
    /// (spec §2 "Cấp, sao và phe", docs/KeHoach/HeroProgression.md). Pure functions over the settings.
    /// </summary>
    public sealed class ProgressionRules
    {
        public ProgressionRules(HeroProgressionSettings settings)
        {
            Settings = settings ?? new HeroProgressionSettings();
            if (Settings.Stars == null || Settings.Stars.Count == 0)
                Settings.Stars = HeroProgressionSettings.DefaultStars;
        }

        public HeroProgressionSettings Settings { get; }

        public int MaxStars => Settings.Stars.Count;

        public int ClampLevel(int level) => Math.Max(1, Math.Min(level, Math.Max(1, Settings.MaxLevel)));

        public int ClampStars(int stars) => Math.Max(1, Math.Min(stars, MaxStars));

        public StarRule GetStarRule(int stars) => Settings.Stars[ClampStars(stars) - 1];

        /// <summary>(1 + rate)^(level − 1): level 1 = ×1.</summary>
        public static float GrowthMultiplier(float growthPerLevel, int level) =>
            (float)Math.Pow(1.0 + growthPerLevel, Math.Max(1, level) - 1);

        // ---- Stat scaling ----

        /// <summary>
        /// Lord breakthroughs counted at this level: every breakthrough level reached when they are automatic, otherwise
        /// at most <paramref name="earnedBreakthroughs"/>.
        /// </summary>
        public int BreakthroughsReached(int level, int earnedBreakthroughs = 0)
        {
            level = ClampLevel(level);
            var possible = 0;
            if (Settings.BreakthroughLevels != null)
            {
                foreach (var breakthroughLevel in Settings.BreakthroughLevels)
                {
                    if (level >= breakthroughLevel)
                        possible++;
                }
            }
            return Settings.AutomaticBreakthroughs ? possible : Math.Min(possible, Math.Max(0, earnedBreakthroughs));
        }

        /// <summary>
        /// Multipliers for a hero's base Công and Máu: growth by level for its class profile, times the star bonus
        /// (starter and named heroes) or the breakthrough bonus (lords).
        /// </summary>
        public StatScale Scale(HeroKind kind, GrowthRates growth, int level, int stars, int earnedBreakthroughs = 0)
        {
            level = ClampLevel(level);
            var bonus = kind == HeroKind.Lord
                ? BreakthroughsReached(level, earnedBreakthroughs) * Settings.BreakthroughStatBonus
                : GetStarRule(stars).StatBonus;
            return new StatScale(
                GrowthMultiplier(growth.Attack, level) * (1f + bonus),
                GrowthMultiplier(growth.Health, level) * (1f + bonus));
        }

        /// <summary>Squad soldiers: level growth of their hero times Thống suất / 100.</summary>
        public float TroopMultiplier(int heroLevel, float leadership) =>
            GrowthMultiplier(Settings.TroopGrowthPerLevel, ClampLevel(heroLevel)) * Math.Max(0f, leadership) / 100f;

        /// <summary>Enemies of a stage with this enemy level.</summary>
        public float EnemyMultiplier(int enemyLevel) => GrowthMultiplier(Settings.EnemyGrowthPerLevel, Math.Max(1, enemyLevel));

        /// <summary>Applies the caps: damage reduction ≤ <see cref="HeroProgressionSettings.MaxDamageReduction"/>, crit 0–1.</summary>
        public void Clamp(ref CombatNumbers numbers)
        {
            numbers.DamageReduction = Math.Max(0f, Math.Min(numbers.DamageReduction, Settings.MaxDamageReduction));
            numbers.CritChance = Math.Max(0f, Math.Min(1f, numbers.CritChance));
            numbers.CritMultiplier = Math.Max(1f, numbers.CritMultiplier);
            numbers.AttackInterval = Math.Max(0.05f, numbers.AttackInterval);
        }

        /// <summary>Damage left after the target's damage reduction (capped).</summary>
        public float Mitigate(float damage, float damageReduction) =>
            damage * (1f - Math.Max(0f, Math.Min(damageReduction, Settings.MaxDamageReduction)));

        // ---- Skills ----

        /// <summary>Lords unlock skills by level; starter and named heroes by stars.</summary>
        public bool IsSkillUnlocked(HeroKind kind, SkillSlot slot, int level, int stars)
        {
            var index = (int)slot;
            if (kind != HeroKind.Lord)
                return index < GetStarRule(stars).UnlockedSkillCount;
            var levels = Settings.LordSkillUnlockLevels;
            return levels != null && index < levels.Count && ClampLevel(level) >= levels[index];
        }

        /// <summary>Lord level that unlocks a slot, or −1 if the slot never unlocks.</summary>
        public int LordUnlockLevel(SkillSlot slot)
        {
            var levels = Settings.LordSkillUnlockLevels;
            var index = (int)slot;
            return levels != null && index < levels.Count ? levels[index] : -1;
        }

        /// <summary>Skill points a lord has earned at this level (one per level from level 5 by default).</summary>
        public int SkillPointsEarned(int lordLevel)
        {
            var level = ClampLevel(lordLevel);
            if (level < Settings.SkillPointsFromLevel)
                return 0;
            return (level - Settings.SkillPointsFromLevel + 1) * Math.Max(0, Settings.SkillPointsPerLevel);
        }

        /// <summary>Highest level a lord skill can have at this lord level: lord level ÷ divisor, rounded up.</summary>
        public int SkillLevelCap(int lordLevel)
        {
            var divisor = Math.Max(1, Settings.SkillLevelCapDivisor);
            return Math.Max(1, Math.Min(Settings.MaxSkillLevel, (ClampLevel(lordLevel) + divisor - 1) / divisor));
        }

        /// <summary>Points spent on lord skills: a skill starts at level 1 for free when unlocked.</summary>
        public int SkillPointsSpent(IReadOnlyList<int> skillLevels)
        {
            var spent = 0;
            if (skillLevels == null)
                return 0;
            foreach (var skillLevel in skillLevels)
                spent += Math.Max(0, skillLevel - 1);
            return spent;
        }

        /// <summary>
        /// Level a lord skill actually has: 0 while locked, at least 1 once unlocked, never above the level cap.
        /// <paramref name="storedLevel"/> is what the player bought (0 = not upgraded yet).
        /// </summary>
        public int EffectiveLordSkillLevel(int lordLevel, SkillSlot slot, int storedLevel)
        {
            if (!IsSkillUnlocked(HeroKind.Lord, slot, lordLevel, 1))
                return 0;
            return Math.Max(1, Math.Min(storedLevel, SkillLevelCap(lordLevel)));
        }

        /// <summary>True if one more point can go into this lord skill (unlocked, below the cap, a point is free).</summary>
        public bool CanUpgradeLordSkill(int lordLevel, IReadOnlyList<int> skillLevels, SkillSlot slot)
        {
            var index = (int)slot;
            if (!IsSkillUnlocked(HeroKind.Lord, slot, lordLevel, 1))
                return false;
            var current = skillLevels != null && index < skillLevels.Count ? Math.Max(1, skillLevels[index]) : 1;
            return current < SkillLevelCap(lordLevel) && SkillPointsSpent(skillLevels) < SkillPointsEarned(lordLevel);
        }

        /// <summary>Skill level of a starter or named hero, set by its stars.</summary>
        public int StarSkillLevel(int stars) => Math.Max(1, Math.Min(Settings.MaxSkillLevel, GetStarRule(stars).SkillLevel));

        /// <summary>Multiplier of a skill's damage / heal / shield at this skill level (0 for a locked skill).</summary>
        public float SkillValueMultiplier(int skillLevel)
        {
            if (skillLevel <= 0)
                return 0f;
            return 1f + (Math.Min(skillLevel, Settings.MaxSkillLevel) - 1) * Settings.SkillValueBonusPerLevel;
        }

        // ---- Combat power ----

        /// <summary>
        /// Strength used for balancing: √(Công × attacks per second × effective health). Two heroes with the same value
        /// draw a duel (docs/KeHoach/HeroProgression.md §3). Crits are left out: every hero has the same base chance.
        /// </summary>
        public static float Strength(CombatNumbers numbers) =>
            (float)Math.Sqrt(Math.Max(0f, numbers.Damage * numbers.AttacksPerSecond * numbers.EffectiveHealth));

        /// <summary>Combat power shown in menus: equal values mean equally strong heroes (docs/KeHoach/HeroProgression.md §11).</summary>
        public float CombatPower(CombatNumbers numbers, float roleBonus, int totalSkillLevels) =>
            Settings.CombatPowerScale * Strength(numbers) * (1f + Math.Max(0f, roleBonus)) *
            (1f + Settings.CombatPowerPerSkillLevel * Math.Max(0, totalSkillLevels));
    }
}
