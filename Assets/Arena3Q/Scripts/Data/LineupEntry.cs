using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>Checks shared by every lineup source (test loadout, the player's profile).</summary>
    public static class LineupValidation
    {
        /// <summary>A lineup is the lord first, then other heroes, each hero once, every hero valid.</summary>
        public static void Validate(IReadOnlyList<LineupEntry> lineup, string owner, List<string> errors)
        {
            if (lineup == null || lineup.Count == 0)
            {
                errors.Add($"{owner}: the lineup is empty.");
                return;
            }

            var seen = new HashSet<HeroDefinition>();
            for (var i = 0; i < lineup.Count; i++)
            {
                var hero = lineup[i]?.Hero;
                if (hero == null)
                {
                    errors.Add($"{owner}: card {i} has no hero.");
                    continue;
                }
                if (i == 0 && hero.Kind != HeroKind.Lord)
                    errors.Add($"{owner}: the first card must be the lord, not '{hero.name}'.");
                if (i > 0 && hero.Kind == HeroKind.Lord)
                    errors.Add($"{owner}: card {i} is a second lord ('{hero.name}'); a lineup has exactly one lord.");
                if (!seen.Add(hero))
                    errors.Add($"{owner}: hero '{hero.name}' appears more than once.");
                else
                    hero.Validate(errors);
            }
        }
    }

    /// <summary>One card of a battle lineup: a hero with its level and stars (stars are ignored for lords).</summary>
    [Serializable]
    public sealed class LineupEntry
    {
        [SerializeField] private HeroDefinition hero;
        [SerializeField, Min(1)] private int level = 1;
        [Tooltip("Starter and named heroes only; lords break through by level instead.")]
        [SerializeField, Min(1)] private int stars = 1;
        [Tooltip("Lords only: skill levels bought with skill points (passive, skill 2, skill 3, ultimate); 0 = not upgraded.")]
        [SerializeField] private int[] lordSkillLevels = new int[4];

        public LineupEntry()
        {
        }

        public LineupEntry(HeroDefinition hero, int level = 1, int stars = 1, int[] lordSkillLevels = null)
        {
            this.hero = hero;
            this.level = Mathf.Max(1, level);
            this.stars = Mathf.Max(1, stars);
            this.lordSkillLevels = lordSkillLevels != null ? (int[])lordSkillLevels.Clone() : new int[4];
        }

        public HeroDefinition Hero => hero;
        public int Level => Mathf.Max(1, level);
        public int Stars => Mathf.Max(1, stars);

        public int StoredLordSkillLevel(SkillSlot slot) =>
            lordSkillLevels != null && (int)slot < lordSkillLevels.Length ? lordSkillLevels[(int)slot] : 0;

        /// <summary>Level of a skill in battle: 0 = locked; lords by level and skill points, others by stars.</summary>
        public int SkillLevel(SkillSlot slot, ProgressionRules rules)
        {
            if (hero == null || hero.GetSkill(slot) == null)
                return 0;
            if (hero.Kind == HeroKind.Lord)
                return rules.EffectiveLordSkillLevel(Level, slot, StoredLordSkillLevel(slot));
            return rules.IsSkillUnlocked(hero.Kind, slot, Level, Stars) ? rules.StarSkillLevel(Stars) : 0;
        }

        public override string ToString() => hero != null ? $"{hero.name} Lv{Level} {Stars}★" : "(empty)";
    }
}
