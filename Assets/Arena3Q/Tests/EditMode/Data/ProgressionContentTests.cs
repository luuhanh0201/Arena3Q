using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>
    /// The shipped progression content and its balance invariants (docs/KeHoach/HeroProgression.md §3): retuning the
    /// numbers is expected, but these checks say when a change breaks the balance between classes.
    /// </summary>
    public class ProgressionContentTests
    {
        private const string ContentRoot = "Assets/Arena3Q/Content";

        private static List<T> LoadAll<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { ContentRoot })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .ToList();

        private static ProgressionConfig Config()
        {
            var configs = LoadAll<ProgressionConfig>();
            Assert.That(configs, Has.Count.EqualTo(1), "exactly one ProgressionConfig in the content");
            return configs[0];
        }

        [Test]
        public void ShippedProgressionConfig_GrowthProfilesAndFactions_AreValid()
        {
            var errors = new List<string>();
            Config().Validate(errors);
            foreach (var profile in LoadAll<GrowthProfile>())
                profile.Validate(errors);
            foreach (var faction in LoadAll<FactionDefinition>())
                faction.Validate(errors);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ShippedHeroes_AllHaveAGrowthProfile_AndOnlyNamedHeroesHaveAFaction()
        {
            var heroes = LoadAll<HeroDefinition>();
            Assert.That(heroes, Is.Not.Empty);
            foreach (var hero in heroes)
            {
                Assert.That(hero.GrowthProfile, Is.Not.Null, $"{hero.name} has no growth profile: it would not grow with level");
                Assert.That(hero.Faction == null, Is.EqualTo(hero.Kind != HeroKind.Named), $"{hero.name}: faction vs kind {hero.Kind}");
            }
        }

        [Test]
        public void ShippedGrowthProfiles_GrowCongTimesMauLikeTheStandardHero()
        {
            // Spec §2: growth favours each class's strength, yet every class must stay balanced at every level.
            var reference = Config().ReferenceGrowthPerLevel;
            var expected = (1f + reference) * (1f + reference) - 1f;
            foreach (var profile in LoadAll<GrowthProfile>())
            {
                Assert.That(profile.StrengthGrowthPerLevel, Is.EqualTo(expected).Within(0.0005f),
                    $"{profile.name}: Công × Máu grows {profile.StrengthGrowthPerLevel:P2} per level, the standard hero " +
                    $"{expected:P2}: classes would drift apart as they level up");
            }
        }

        [TestCase(1)]
        [TestCase(15)]
        [TestCase(30)]
        [TestCase(60)]
        public void ShippedLords_AreEquallyStrong_AtEveryLevel(int level)
        {
            var config = Config();
            var rules = config.CreateRules();
            var lords = LoadAll<HeroDefinition>().Where(h => h.Kind == HeroKind.Lord).ToList();
            Assert.That(lords.Count, Is.GreaterThanOrEqualTo(4), "the four lords to choose from");

            // Strength × (1 + role bonus) relative to the standard hero of the same level (breakthroughs included).
            var breakthrough = 1f + rules.BreakthroughsReached(level) * rules.Settings.BreakthroughStatBonus;
            foreach (var lord in lords)
            {
                var numbers = CombatNumbersFactory.ForHero(lord, level, 1, rules, CombatModifiers.None);
                var ratio = ProgressionRules.Strength(numbers) * (1f + lord.RoleBonus) /
                            (config.ReferenceStrength(level) * breakthrough);
                Assert.That(ratio, Is.EqualTo(1f).Within(0.05f), $"{lord.name} at level {level}: {ratio:F3} × the standard hero");
            }
        }

        [Test]
        public void ShippedLoadout_AtLevelOneAndOneStar_KeepsThePrototypeNumbers()
        {
            // Balance tuning is deferred: level 1 / 1 star must not change what the prototype fights with.
            var rules = Config().CreateRules();
            foreach (var loadout in LoadAll<TestLoadout>())
            {
                foreach (var card in loadout.GetLineup())
                {
                    var numbers = CombatNumbersFactory.ForHero(card.Hero, 1, 1, rules, CombatModifiers.None);
                    Assert.That(numbers.MaxHealth, Is.EqualTo(card.Hero.Stats.MaxHealth).Within(0.001f), card.Hero.name);
                    Assert.That(numbers.Damage, Is.EqualTo(card.Hero.Stats.Damage).Within(0.001f), card.Hero.name);
                }
            }
        }

        [Test]
        public void ShippedFactions_ThreeHeroTiers_AreWorthTheSame()
        {
            // §9: a tier of each faction adds about the same to DPS × effective health, so no faction is the best pick.
            foreach (var faction in LoadAll<FactionDefinition>())
            {
                var tier = SynergyRules.BestTier(faction.CreateSynergyTiers(), 3);
                Assert.That(tier, Is.Not.Null, $"{faction.name} has a 3-hero tier");
                var gain = Gain(tier.Bonus);
                Assert.That(gain, Is.EqualTo(0.12f).Within(0.025f), $"{faction.name}: 3 heroes add {gain:P1}");
            }
        }

        // Product of DPS and effective health gained by a bonus, for a hero with the default 5 % crit at ×1.5.
        private static float Gain(CombatModifiers bonus)
        {
            var before = new CombatNumbers { MaxHealth = 1000f, Damage = 50f, AttackInterval = 1f, CritChance = 0.05f, CritMultiplier = 1.5f };
            var after = before;
            bonus.ApplyTo(ref after);
            // Rage gain counts 20 % and cooldown reduction 30 % of their value: skills are only part of a hero's damage.
            var skillGain = 1f + 0.2f * bonus.RageGainBonus + 0.3f * bonus.CooldownReduction;
            return after.DamagePerSecond * after.EffectiveHealth * skillGain / (before.DamagePerSecond * before.EffectiveHealth) - 1f;
        }
    }
}
