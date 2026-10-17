using System.Collections.Generic;
using Arena3Q.Core;

namespace Arena3Q.Data
{
    /// <summary>
    /// Turns definitions into the combat numbers a unit fights with: level, stars, breakthroughs, Thống suất and faction
    /// synergy applied through <see cref="ProgressionRules"/> (docs/KeHoach/HeroProgression.md). The one place where
    /// definitions meet the progression rules, so the battle code never repeats the formulas.
    /// </summary>
    public static class CombatNumbersFactory
    {
        /// <summary>The definition's own numbers, unscaled (level 1, 1 star, no bonus, no crit).</summary>
        public static CombatNumbers FromStats(CombatStats stats) => new CombatNumbers
        {
            MaxHealth = stats.MaxHealth,
            Damage = stats.Damage,
            AttackInterval = stats.AttackInterval,
            MoveSpeed = stats.MoveSpeed,
            AttackRange = stats.AttackRange,
            ReactionRange = stats.ReactionRange,
            AggroRange = stats.AggroRange,
            AttackerMemorySeconds = stats.AttackerMemorySeconds,
            DamageReduction = 0f,
            CritChance = 0f,
            CritMultiplier = 1f
        };

        public static CombatNumbers ForHero(HeroDefinition hero, int level, int stars, ProgressionRules rules,
            CombatModifiers modifiers)
        {
            var numbers = FromStats(hero.Stats);
            var scale = rules.Scale(hero.Kind, hero.GrowthRates, level, stars);
            numbers.MaxHealth *= scale.Health;
            numbers.Damage *= scale.Attack;
            numbers.DamageReduction = hero.DamageReduction;
            numbers.CritChance = rules.Settings.HeroBaseCritChance + hero.CritChanceBonus;
            numbers.CritMultiplier = rules.Settings.CritMultiplier;
            modifiers.ApplyTo(ref numbers);
            rules.Clamp(ref numbers);
            return numbers;
        }

        public static CombatNumbers ForHero(LineupEntry entry, ProgressionRules rules, CombatModifiers modifiers) =>
            ForHero(entry.Hero, entry.Level, entry.Stars, rules, modifiers);

        /// <summary>A squad soldier of a hero at <paramref name="heroLevel"/> with Thống suất <paramref name="leadership"/>.</summary>
        public static CombatNumbers ForTroop(TroopDefinition troop, int heroLevel, float leadership, ProgressionRules rules,
            CombatModifiers modifiers)
        {
            var numbers = FromStats(troop.Stats);
            var multiplier = rules.TroopMultiplier(heroLevel, leadership);
            numbers.MaxHealth *= multiplier;
            numbers.Damage *= multiplier;
            // Soldiers have no crit of their own, but a faction bonus (Quần) can give them some.
            numbers.CritMultiplier = rules.Settings.CritMultiplier;
            modifiers.ApplyTo(ref numbers);
            rules.Clamp(ref numbers);
            return numbers;
        }

        /// <summary>An enemy soldier of a stage with enemy level <paramref name="enemyLevel"/>.</summary>
        public static CombatNumbers ForEnemy(TroopDefinition troop, int enemyLevel, ProgressionRules rules)
        {
            var numbers = FromStats(troop.Stats);
            var multiplier = rules.EnemyMultiplier(enemyLevel);
            numbers.MaxHealth *= multiplier;
            numbers.Damage *= multiplier;
            rules.Clamp(ref numbers);
            return numbers;
        }

        /// <summary>Faction synergy of a lineup: named heroes count for their faction, lords and starters for none.</summary>
        public static CombatModifiers SynergyOf(IEnumerable<LineupEntry> lineup)
        {
            var factions = new Dictionary<string, FactionDefinition>();
            var ids = new List<string>();
            if (lineup != null)
            {
                foreach (var entry in lineup)
                {
                    var faction = entry?.Hero != null && entry.Hero.Kind == HeroKind.Named ? entry.Hero.Faction : null;
                    if (faction == null)
                        continue;
                    factions[faction.FactionId] = faction;
                    ids.Add(faction.FactionId);
                }
            }
            return SynergyRules.Evaluate(ids, id => factions.TryGetValue(id, out var faction) ? faction.CreateSynergyTiers() : null);
        }

        /// <summary>Combat power of a hero card as shown in menus.</summary>
        public static float CombatPower(LineupEntry entry, ProgressionRules rules, int totalSkillLevels = 0) =>
            rules.CombatPower(ForHero(entry, rules, CombatModifiers.None), entry.Hero.RoleBonus, totalSkillLevels);
    }
}
