using System;

namespace Arena3Q.Core
{
    /// <summary>The three kinds of heroes (spec §2 "Cấp, sao và phe").</summary>
    public enum HeroKind
    {
        /// <summary>A starter hero: no faction, low base stats, cheap stars, skills unlocked by stars.</summary>
        Starter,
        /// <summary>The player's lord: no faction, no stars; learns skills by level and breaks through at set levels.</summary>
        Lord,
        /// <summary>A named general obtained later (Lữ Bố…): belongs to a faction, skills unlocked by stars.</summary>
        Named
    }

    /// <summary>The four skills every hero has, in unlock order (spec §3 "Kỹ năng trên thẻ").</summary>
    public enum SkillSlot
    {
        Passive = 0,
        Skill2 = 1,
        Skill3 = 2,
        Ultimate = 3
    }

    /// <summary>Per-level growth of a hero's Công and Máu, compounding: 0.033 = +3.3 % per level.</summary>
    public readonly struct GrowthRates
    {
        public GrowthRates(float attack, float health)
        {
            Attack = attack;
            Health = health;
        }

        public float Attack { get; }
        public float Health { get; }

        /// <summary>No growth at all (heroes without a growth profile).</summary>
        public static GrowthRates None => new GrowthRates(0f, 0f);
    }

    /// <summary>Multipliers applied to a hero's base Công and Máu.</summary>
    public readonly struct StatScale
    {
        public StatScale(float attack, float health)
        {
            Attack = attack;
            Health = health;
        }

        public float Attack { get; }
        public float Health { get; }
    }

    /// <summary>One row of the star table (docs/KeHoach/HeroProgression.md §7).</summary>
    public sealed class StarRule
    {
        public StarRule(int stars, float statBonus, int unlockedSkillCount, int skillLevel)
        {
            Stars = stars;
            StatBonus = statBonus;
            UnlockedSkillCount = unlockedSkillCount;
            SkillLevel = skillLevel;
        }

        public int Stars { get; }

        /// <summary>Added to Máu and Công: 0.1 = +10 %.</summary>
        public float StatBonus { get; }

        /// <summary>How many skills are available, in slot order (passive, skill 2, skill 3, ultimate).</summary>
        public int UnlockedSkillCount { get; }

        /// <summary>Level of every unlocked skill at this star count.</summary>
        public int SkillLevel { get; }
    }

    /// <summary>
    /// Final combat numbers of one unit in battle, after level, stars, breakthroughs and faction bonuses. Built by the
    /// Data layer from the unit's definition; the gameplay layer only reads it.
    /// </summary>
    public struct CombatNumbers
    {
        public float MaxHealth;
        public float Damage;
        public float AttackInterval;
        public float MoveSpeed;
        public float AttackRange;
        public float ReactionRange;
        public float AggroRange;
        public float AttackerMemorySeconds;

        /// <summary>Share of incoming damage blocked: 0.1 = 10 %.</summary>
        public float DamageReduction;

        /// <summary>Chance (0–1) that an attack is a critical hit.</summary>
        public float CritChance;

        /// <summary>Damage multiplier of a critical hit.</summary>
        public float CritMultiplier;

        public float AttacksPerSecond => AttackInterval > 0f ? 1f / AttackInterval : 0f;

        /// <summary>Average damage per second against a target without damage reduction, including crits.</summary>
        public float DamagePerSecond =>
            Damage * AttacksPerSecond * (1f + Math.Max(0f, Math.Min(1f, CritChance)) * Math.Max(0f, CritMultiplier - 1f));

        /// <summary>Health counting damage reduction.</summary>
        public float EffectiveHealth => MaxHealth / Math.Max(0.01f, 1f - DamageReduction);
    }
}
