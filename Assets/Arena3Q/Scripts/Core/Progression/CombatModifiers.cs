using System;
using System.Collections.Generic;

namespace Arena3Q.Core
{
    /// <summary>
    /// Bonuses applied on top of a unit's own numbers, e.g. faction synergy (spec §2 "Cấp, sao và phe").
    /// Percentages are fractions: 0.06 = +6 %.
    /// </summary>
    public struct CombatModifiers
    {
        public float AttackBonus;
        public float HealthBonus;
        /// <summary>Added to the unit's damage reduction (still capped by the progression settings).</summary>
        public float DamageReduction;
        public float AttackSpeedBonus;
        /// <summary>Faster rage gain for ultimates (stored for the skill system).</summary>
        public float RageGainBonus;
        /// <summary>Shorter skill cooldowns (stored for the skill system).</summary>
        public float CooldownReduction;
        /// <summary>Added to the crit chance.</summary>
        public float CritChance;
        /// <summary>Crit damage multiplier to use if higher than the unit's own; 0 = keep the unit's.</summary>
        public float CritMultiplier;

        public static CombatModifiers None => default;

        public bool IsNone =>
            AttackBonus == 0f && HealthBonus == 0f && DamageReduction == 0f && AttackSpeedBonus == 0f &&
            RageGainBonus == 0f && CooldownReduction == 0f && CritChance == 0f && CritMultiplier == 0f;

        public static CombatModifiers operator +(CombatModifiers a, CombatModifiers b) => new CombatModifiers
        {
            AttackBonus = a.AttackBonus + b.AttackBonus,
            HealthBonus = a.HealthBonus + b.HealthBonus,
            DamageReduction = a.DamageReduction + b.DamageReduction,
            AttackSpeedBonus = a.AttackSpeedBonus + b.AttackSpeedBonus,
            RageGainBonus = a.RageGainBonus + b.RageGainBonus,
            CooldownReduction = a.CooldownReduction + b.CooldownReduction,
            CritChance = a.CritChance + b.CritChance,
            CritMultiplier = Math.Max(a.CritMultiplier, b.CritMultiplier)
        };

        /// <summary>Applies the bonuses to a unit's numbers (caps are applied by <see cref="ProgressionRules.Clamp"/>).</summary>
        public void ApplyTo(ref CombatNumbers numbers)
        {
            numbers.MaxHealth *= 1f + HealthBonus;
            numbers.Damage *= 1f + AttackBonus;
            numbers.AttackInterval /= Math.Max(0.05f, 1f + AttackSpeedBonus);
            numbers.DamageReduction += DamageReduction;
            numbers.CritChance += CritChance;
            if (CritMultiplier > numbers.CritMultiplier)
                numbers.CritMultiplier = CritMultiplier;
        }
    }

    /// <summary>A faction bonus reached when the lineup holds at least <see cref="MinHeroes"/> heroes of that faction.</summary>
    public sealed class SynergyTier
    {
        public SynergyTier(int minHeroes, CombatModifiers bonus)
        {
            MinHeroes = minHeroes;
            Bonus = bonus;
        }

        public int MinHeroes { get; }
        public CombatModifiers Bonus { get; }
    }

    /// <summary>Faction synergy (spec §2): each faction gives its highest tier reached; bonuses of different factions add up.</summary>
    public static class SynergyRules
    {
        /// <param name="lineupFactionIds">Faction id of every card in the lineup; null or empty = no faction (lords, starters).</param>
        /// <param name="tiersOf">Synergy tiers of a faction id (any order); null or empty = no synergy.</param>
        public static CombatModifiers Evaluate(IEnumerable<string> lineupFactionIds,
            Func<string, IReadOnlyList<SynergyTier>> tiersOf)
        {
            var counts = new Dictionary<string, int>();
            if (lineupFactionIds != null)
            {
                foreach (var id in lineupFactionIds)
                {
                    if (string.IsNullOrEmpty(id))
                        continue;
                    counts.TryGetValue(id, out var count);
                    counts[id] = count + 1;
                }
            }

            var total = CombatModifiers.None;
            foreach (var pair in counts)
            {
                var best = BestTier(tiersOf?.Invoke(pair.Key), pair.Value);
                if (best != null)
                    total += best.Bonus;
            }
            return total;
        }

        /// <summary>The tier with the highest requirement that <paramref name="heroCount"/> meets, or null.</summary>
        public static SynergyTier BestTier(IReadOnlyList<SynergyTier> tiers, int heroCount)
        {
            SynergyTier best = null;
            if (tiers == null)
                return null;
            foreach (var tier in tiers)
            {
                if (tier != null && tier.MinHeroes <= heroCount && (best == null || tier.MinHeroes > best.MinHeroes))
                    best = tier;
            }
            return best;
        }
    }
}
