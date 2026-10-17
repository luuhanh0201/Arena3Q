using System.Collections.Generic;
using Arena3Q.Core;
using NUnit.Framework;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>Levels, stars, breakthroughs, skill unlocks and points (docs/KeHoach/HeroProgression.md) with the design defaults.</summary>
    public class ProgressionRulesTests
    {
        private const float Tolerance = 0.005f;
        private static readonly GrowthRates Offense = new GrowthRates(0.036f, 0.024f);

        private static ProgressionRules Rules(System.Action<HeroProgressionSettings> edit = null)
        {
            var settings = new HeroProgressionSettings();
            edit?.Invoke(settings);
            return new ProgressionRules(settings);
        }

        [Test]
        public void GrowthMultiplier_IsOneAtLevelOne_ThenCompounds()
        {
            Assert.That(ProgressionRules.GrowthMultiplier(0.03f, 1), Is.EqualTo(1f));
            Assert.That(ProgressionRules.GrowthMultiplier(0.03f, 60), Is.EqualTo(5.72f).Within(0.01f));
            Assert.That(ProgressionRules.GrowthMultiplier(0.036f, 60), Is.EqualTo(8.06f).Within(0.01f));
        }

        [Test]
        public void Scale_StarterHero_UsesClassGrowthAndStarBonus()
        {
            var rules = Rules();

            var oneStar = rules.Scale(HeroKind.Starter, Offense, 1, 1);
            Assert.That(oneStar.Attack, Is.EqualTo(1f));
            Assert.That(oneStar.Health, Is.EqualTo(1f));

            var threeStars = rules.Scale(HeroKind.Starter, Offense, 1, 3);
            Assert.That(threeStars.Attack, Is.EqualTo(1.2f).Within(Tolerance));

            var level60 = rules.Scale(HeroKind.Named, Offense, 60, 1);
            Assert.That(level60.Attack, Is.EqualTo(8.06f).Within(0.01f), "strength (Công) grows fast");
            Assert.That(level60.Health, Is.EqualTo(4.05f).Within(0.01f), "weakness (Máu) grows slowly");
        }

        [Test]
        public void Scale_Lord_IgnoresStars_AndBreaksThroughAt15_30_45_60()
        {
            var rules = Rules();
            float Bonus(int level) => rules.Scale(HeroKind.Lord, GrowthRates.None, level, 5).Attack;

            Assert.That(Bonus(14), Is.EqualTo(1f), "stars do nothing for a lord");
            Assert.That(Bonus(15), Is.EqualTo(1.1f).Within(Tolerance));
            Assert.That(Bonus(30), Is.EqualTo(1.2f).Within(Tolerance));
            Assert.That(Bonus(59), Is.EqualTo(1.3f).Within(Tolerance));
            Assert.That(Bonus(60), Is.EqualTo(1.4f).Within(Tolerance), "same as a 5-star named hero");
        }

        [Test]
        public void Breakthroughs_WhenNotAutomatic_MustBeEarned()
        {
            var rules = Rules(s => s.AutomaticBreakthroughs = false);

            Assert.That(rules.BreakthroughsReached(60, 0), Is.Zero);
            Assert.That(rules.BreakthroughsReached(60, 2), Is.EqualTo(2));
            Assert.That(rules.BreakthroughsReached(20, 3), Is.EqualTo(1), "cannot exceed the levels reached");
        }

        [Test]
        public void LevelAndStars_AreClamped()
        {
            var rules = Rules();
            Assert.That(rules.ClampLevel(0), Is.EqualTo(1));
            Assert.That(rules.ClampLevel(99), Is.EqualTo(60));
            Assert.That(rules.ClampStars(0), Is.EqualTo(1));
            Assert.That(rules.ClampStars(9), Is.EqualTo(5));
        }

        [Test]
        public void LordSkills_UnlockAtLevels_2_5_10_15()
        {
            var rules = Rules();
            Assert.That(rules.IsSkillUnlocked(HeroKind.Lord, SkillSlot.Passive, 1, 1), Is.False, "a lord starts without skills");
            Assert.That(rules.IsSkillUnlocked(HeroKind.Lord, SkillSlot.Passive, 2, 1), Is.True);
            Assert.That(rules.IsSkillUnlocked(HeroKind.Lord, SkillSlot.Skill2, 4, 1), Is.False);
            Assert.That(rules.IsSkillUnlocked(HeroKind.Lord, SkillSlot.Skill2, 5, 1), Is.True);
            Assert.That(rules.IsSkillUnlocked(HeroKind.Lord, SkillSlot.Skill3, 10, 1), Is.True);
            Assert.That(rules.IsSkillUnlocked(HeroKind.Lord, SkillSlot.Ultimate, 14, 5), Is.False, "stars do not unlock lord skills");
            Assert.That(rules.IsSkillUnlocked(HeroKind.Lord, SkillSlot.Ultimate, 15, 1), Is.True);
            Assert.That(rules.LordUnlockLevel(SkillSlot.Ultimate), Is.EqualTo(15));
        }

        [Test]
        public void OtherHeroes_UnlockSkillsByStars()
        {
            var rules = Rules();
            Assert.That(rules.IsSkillUnlocked(HeroKind.Starter, SkillSlot.Passive, 1, 1), Is.True);
            Assert.That(rules.IsSkillUnlocked(HeroKind.Starter, SkillSlot.Skill2, 1, 1), Is.True);
            Assert.That(rules.IsSkillUnlocked(HeroKind.Starter, SkillSlot.Skill3, 60, 1), Is.False, "levels do not unlock star skills");
            Assert.That(rules.IsSkillUnlocked(HeroKind.Named, SkillSlot.Skill3, 1, 2), Is.True);
            Assert.That(rules.IsSkillUnlocked(HeroKind.Named, SkillSlot.Ultimate, 1, 2), Is.False);
            Assert.That(rules.IsSkillUnlocked(HeroKind.Named, SkillSlot.Ultimate, 1, 3), Is.True);
            Assert.That(new[] { 1, 2, 3, 4, 5 }, Has.All.Matches<int>(s => rules.StarSkillLevel(s) == new[] { 1, 4, 7, 11, 15 }[s - 1]));
        }

        [Test]
        public void SkillPoints_OnePerLevelFromFive_ExactlyEnoughToMaxEverySkillAtSixty()
        {
            var rules = Rules();
            Assert.That(rules.SkillPointsEarned(4), Is.Zero);
            Assert.That(rules.SkillPointsEarned(5), Is.EqualTo(1));
            Assert.That(rules.SkillPointsEarned(60), Is.EqualTo(56));
            Assert.That(rules.SkillLevelCap(5), Is.EqualTo(2));
            Assert.That(rules.SkillLevelCap(60), Is.EqualTo(15));
            Assert.That(4 * (rules.SkillLevelCap(60) - 1), Is.EqualTo(rules.SkillPointsEarned(60)));
        }

        [TestCase(10)]
        [TestCase(20)]
        [TestCase(40)]
        [TestCase(60)]
        public void SkillPoints_AtMilestones_FillEveryUnlockedSkillToTheCap(int level)
        {
            var rules = Rules();
            var unlocked = 0;
            foreach (SkillSlot slot in System.Enum.GetValues(typeof(SkillSlot)))
            {
                if (rules.IsSkillUnlocked(HeroKind.Lord, slot, level, 1))
                    unlocked++;
            }
            Assert.That(unlocked * (rules.SkillLevelCap(level) - 1), Is.EqualTo(rules.SkillPointsEarned(level)));
        }

        [Test]
        public void CanUpgradeLordSkill_NeedsUnlockFreePointAndRoomUnderTheCap()
        {
            var rules = Rules();
            var fresh = new List<int> { 1, 1, 0, 0 };
            Assert.That(rules.CanUpgradeLordSkill(5, fresh, SkillSlot.Passive), Is.True);
            Assert.That(rules.CanUpgradeLordSkill(5, fresh, SkillSlot.Skill3), Is.False, "locked");

            var spent = new List<int> { 2, 1, 0, 0 };
            Assert.That(rules.CanUpgradeLordSkill(5, spent, SkillSlot.Skill2), Is.False, "no free point");
            Assert.That(rules.CanUpgradeLordSkill(8, spent, SkillSlot.Passive), Is.False, "level cap 2 at lord level 8");
            Assert.That(rules.CanUpgradeLordSkill(9, spent, SkillSlot.Passive), Is.True);
        }

        [Test]
        public void EffectiveLordSkillLevel_IsZeroWhileLocked_OneWhenUnlocked_CappedByLevel()
        {
            var rules = Rules();
            Assert.That(rules.EffectiveLordSkillLevel(1, SkillSlot.Passive, 5), Is.Zero);
            Assert.That(rules.EffectiveLordSkillLevel(2, SkillSlot.Passive, 0), Is.EqualTo(1));
            Assert.That(rules.EffectiveLordSkillLevel(8, SkillSlot.Passive, 9), Is.EqualTo(2));
        }

        [Test]
        public void SkillValueMultiplier_AddsFivePercentPerLevel()
        {
            var rules = Rules();
            Assert.That(rules.SkillValueMultiplier(0), Is.Zero);
            Assert.That(rules.SkillValueMultiplier(1), Is.EqualTo(1f));
            Assert.That(rules.SkillValueMultiplier(15), Is.EqualTo(1.7f).Within(Tolerance));
        }

        [Test]
        public void TroopAndEnemyMultipliers_FollowLevelAndLeadership()
        {
            var rules = Rules();
            Assert.That(rules.TroopMultiplier(1, 100f), Is.EqualTo(1f));
            Assert.That(rules.TroopMultiplier(1, 110f), Is.EqualTo(1.1f).Within(Tolerance));
            Assert.That(rules.TroopMultiplier(10, 100f), Is.EqualTo(1.3048f).Within(0.001f));
            Assert.That(rules.EnemyMultiplier(10), Is.EqualTo(1.3048f).Within(0.001f), "about one chapter = +30 %");
        }

        [Test]
        public void Mitigate_AppliesDamageReductionUpToTheCap()
        {
            var rules = Rules();
            Assert.That(rules.Mitigate(100f, 0.1f), Is.EqualTo(90f).Within(Tolerance));
            Assert.That(rules.Mitigate(100f, 0.9f), Is.EqualTo(25f).Within(Tolerance), "capped at 75 %");
        }

        [Test]
        public void CombatPower_OfTheStandardHero_IsAbout2010()
        {
            var rules = Rules();
            var numbers = new CombatNumbers { MaxHealth = 900f, Damage = 45f, AttackInterval = 1f, CritMultiplier = 1.5f, CritChance = 0.05f };
            Assert.That(ProgressionRules.Strength(numbers), Is.EqualTo(201.25f).Within(0.01f));
            Assert.That(rules.CombatPower(numbers, 0f, 0), Is.EqualTo(2012.5f).Within(0.1f));
            Assert.That(rules.CombatPower(numbers, 0.5f, 10), Is.EqualTo(2012.5f * 1.5f * 1.04f).Within(0.5f));
        }
    }

    public class SynergyRulesTests
    {
        private static IReadOnlyList<SynergyTier> Tiers(params (int heroes, CombatModifiers bonus)[] tiers)
        {
            var list = new List<SynergyTier>();
            foreach (var (heroes, bonus) in tiers)
                list.Add(new SynergyTier(heroes, bonus));
            return list;
        }

        private static readonly Dictionary<string, IReadOnlyList<SynergyTier>> Factions = new Dictionary<string, IReadOnlyList<SynergyTier>>
        {
            ["shu"] = Tiers((2, new CombatModifiers { AttackBonus = 0.06f }), (3, new CombatModifiers { AttackBonus = 0.12f })),
            ["wei"] = Tiers((2, new CombatModifiers { HealthBonus = 0.03f, DamageReduction = 0.03f })),
            ["qun"] = Tiers((2, new CombatModifiers { CritChance = 0.06f, CritMultiplier = 1.75f }))
        };

        private static CombatModifiers Evaluate(params string[] factionIds) =>
            SynergyRules.Evaluate(factionIds, id => Factions.TryGetValue(id, out var tiers) ? tiers : null);

        [Test]
        public void Evaluate_EachFactionGivesItsHighestTier_AndFactionsAddUp()
        {
            var bonus = Evaluate("shu", "shu", "shu", "wei", "wei", null);
            Assert.That(bonus.AttackBonus, Is.EqualTo(0.12f).Within(1e-5f), "3 Thục: the 3-hero tier, not 2 + 3");
            Assert.That(bonus.HealthBonus, Is.EqualTo(0.03f).Within(1e-5f));
            Assert.That(bonus.DamageReduction, Is.EqualTo(0.03f).Within(1e-5f));
        }

        [Test]
        public void Evaluate_BelowTheFirstTier_OrWithoutFaction_GivesNothing()
        {
            Assert.That(Evaluate("shu").IsNone, Is.True);
            Assert.That(Evaluate(null, "", null).IsNone, Is.True, "lords and starter heroes have no faction");
            Assert.That(Evaluate("unknown", "unknown").IsNone, Is.True);
        }

        [Test]
        public void ApplyTo_ScalesTheNumbers_AndKeepsTheHigherCritMultiplier()
        {
            var numbers = new CombatNumbers
            {
                MaxHealth = 100f, Damage = 10f, AttackInterval = 1f, DamageReduction = 0.1f, CritChance = 0.05f, CritMultiplier = 1.5f
            };
            var bonus = new CombatModifiers
            {
                HealthBonus = 0.1f, AttackBonus = 0.2f, AttackSpeedBonus = 0.25f, DamageReduction = 0.05f, CritChance = 0.1f,
                CritMultiplier = 1.75f
            };

            bonus.ApplyTo(ref numbers);

            Assert.That(numbers.MaxHealth, Is.EqualTo(110f).Within(1e-3f));
            Assert.That(numbers.Damage, Is.EqualTo(12f).Within(1e-3f));
            Assert.That(numbers.AttackInterval, Is.EqualTo(0.8f).Within(1e-3f));
            Assert.That(numbers.DamageReduction, Is.EqualTo(0.15f).Within(1e-5f));
            Assert.That(numbers.CritChance, Is.EqualTo(0.15f).Within(1e-5f));
            Assert.That(numbers.CritMultiplier, Is.EqualTo(1.75f));
            Assert.That(Evaluate("qun", "qun").CritMultiplier, Is.EqualTo(1.75f));
        }
    }
}
