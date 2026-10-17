using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>The shipped game-flow content (docs/KeHoach/VaoGame.md) and the decisions it encodes.</summary>
    public class FrontendContentTests
    {
        private static FrontendConfig Config()
        {
            var guids = AssetDatabase.FindAssets("t:FrontendConfig", new[] { "Assets/Arena3Q/Content" });
            Assert.That(guids, Has.Length.EqualTo(1), "exactly one FrontendConfig");
            return AssetDatabase.LoadAssetAtPath<FrontendConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        [Test]
        public void ShippedFrontendConfig_IsValid()
        {
            var config = Config();
            var errors = new List<string>();
            config.Validate(errors);
            config.TutorialStage.Validate(errors);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Roster_OffersTheFourLords_WithFourNamedSkills()
        {
            var roster = Config().LordRoster;
            var ids = roster.Lords.Select(l => l.lord.HeroId).ToList();
            Assert.That(ids, Is.EquivalentTo(new[] { "lord_archer", "lord_swordsman", "lord_strategist", "lord_marshal" }));
            foreach (var entry in roster.Lords)
                Assert.That(entry.skillNames.All(n => !string.IsNullOrWhiteSpace(n)), Is.True, entry.lord.name);
        }

        [Test]
        public void StarterGifts_IncludeTheGuardianAtThreeStars_ToTeachTheUltimate()
        {
            // Decision 2026-10-17: the tutorial needs a hero that already has its ultimate.
            var config = Config();
            var rules = config.CreateProgressionRules();
            Assert.That(config.StarterGifts.Any(g => g.Hero.HeroId == "guardian" && g.Stars == 3), Is.True);
            Assert.That(config.StarterGifts.Any(g => rules.IsSkillUnlocked(g.Hero.Kind, SkillSlot.Ultimate, g.Level, g.Stars)),
                Is.True, "some gifted hero can use an ultimate in the tutorial");
        }

        [Test]
        public void TutorialStage_IsAShortDefenseStage()
        {
            var stage = Config().TutorialStage;
            Assert.That(stage.StageType, Is.EqualTo(StageType.Defense));
            Assert.That(stage.TimeLimit, Is.LessThanOrEqualTo(90f));
        }

        [Test]
        public void TutorialReward_UnlocksTheLordsPassive()
        {
            var config = Config();
            var rules = config.CreateProgressionRules();
            Assert.That(rules.IsSkillUnlocked(HeroKind.Lord, SkillSlot.Passive, 1, 1), Is.False, "a lord starts without skills");
            Assert.That(rules.IsSkillUnlocked(HeroKind.Lord, SkillSlot.Passive, config.TutorialRewardLevel, 1), Is.True);
        }

        [Test]
        public void ShippedNameDice_MakesValidNames()
        {
            var names = Config().Names;
            var rules = names.CreateRules();
            var generator = names.CreateGenerator();
            var random = new System.Random(3);
            for (var i = 0; i < 200; i++)
            {
                var name = generator.Next(random, rules);
                Assert.That(rules.Validate(name), Is.EqualTo(NameProblem.None), $"'{name}'");
            }
        }
    }
}
