using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// A faction of named heroes – Thục, Ngụy, Ngô, Quần (spec §2 "Cấp, sao và phe"). The player never picks a faction;
    /// a lineup with several heroes of one faction gets that faction's synergy bonus for the whole army.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Faction Definition", fileName = "Faction")]
    public sealed class FactionDefinition : ScriptableObject
    {
        /// <summary>One synergy tier. Fractions: 0.06 = +6 %.</summary>
        [Serializable]
        public sealed class Tier
        {
            [Tooltip("Heroes of this faction needed in the lineup.")]
            [Min(1)] public int minHeroes = 2;
            public float attackBonus;
            public float healthBonus;
            public float damageReduction;
            public float attackSpeedBonus;
            [Tooltip("Faster rage gain (used once skills exist).")]
            public float rageGainBonus;
            [Tooltip("Shorter cooldowns (used once skills exist).")]
            public float cooldownReduction;
            public float critChance;
            [Tooltip("Crit damage multiplier while this tier is active; 0 = unchanged.")]
            public float critMultiplier;

            public CombatModifiers ToModifiers() => new CombatModifiers
            {
                AttackBonus = attackBonus,
                HealthBonus = healthBonus,
                DamageReduction = damageReduction,
                AttackSpeedBonus = attackSpeedBonus,
                RageGainBonus = rageGainBonus,
                CooldownReduction = cooldownReduction,
                CritChance = critChance,
                CritMultiplier = critMultiplier
            };
        }

        [SerializeField] private string factionId = "faction";
        [SerializeField] private string displayName = "Faction";
        [SerializeField] private Color color = new Color(0.25f, 0.55f, 1f);
        [Tooltip("What the faction is good at, shown in menus.")]
        [SerializeField] private string strength = "";
        [SerializeField] private Tier[] synergyTiers = Array.Empty<Tier>();

        public string FactionId => factionId;
        public string DisplayName => displayName;
        public Color Color => color;
        public string Strength => strength;
        public IReadOnlyList<Tier> SynergyTiers => synergyTiers;

        public IReadOnlyList<SynergyTier> CreateSynergyTiers()
        {
            var tiers = new List<SynergyTier>();
            if (synergyTiers != null)
            {
                foreach (var tier in synergyTiers)
                {
                    if (tier != null)
                        tiers.Add(new SynergyTier(tier.minHeroes, tier.ToModifiers()));
                }
            }
            return tiers;
        }

        public void Validate(List<string> errors)
        {
            var owner = $"Faction '{name}'";
            if (string.IsNullOrWhiteSpace(factionId)) errors.Add($"{owner}: factionId is empty.");
            if (synergyTiers == null)
                return;
            var seen = new HashSet<int>();
            foreach (var tier in synergyTiers)
            {
                if (tier == null)
                    continue;
                if (!seen.Add(tier.minHeroes))
                    errors.Add($"{owner}: two synergy tiers need {tier.minHeroes} heroes.");
            }
        }
    }
}
