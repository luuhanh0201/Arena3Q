using System;
using System.Collections.Generic;
using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>Account and name rules, the screen planner, the story cursor and the profile (spec §2 "Luồng vào game").</summary>
    public class FrontendRulesTests
    {
        [Test]
        public void AccountRules_CheckUsernameAndPassword()
        {
            var rules = new AccountRules();
            Assert.That(rules.ValidateUsername("abc"), Is.EqualTo(CredentialProblem.UsernameLength));
            Assert.That(rules.ValidateUsername("lam_tu_yen"), Is.EqualTo(CredentialProblem.None));
            Assert.That(rules.ValidateUsername("lâm tử yên"), Is.EqualTo(CredentialProblem.UsernameCharacters));
            Assert.That(rules.ValidatePassword("1234567"), Is.EqualTo(CredentialProblem.PasswordLength));
            Assert.That(rules.ValidatePassword("12345678"), Is.EqualTo(CredentialProblem.None));
            Assert.That(rules.ValidatePassword("12345678", "12345679"), Is.EqualTo(CredentialProblem.PasswordMismatch));
        }

        [TestCase("Lâm Tử Yên", NameProblem.None)]
        [TestCase("  Đặng   Văn  ", NameProblem.None)]
        [TestCase("Tô Phong 2", NameProblem.None)]
        [TestCase("A", NameProblem.TooShort)]
        [TestCase("Thượng Quan Thanh Vân", NameProblem.TooLong)]
        [TestCase("Lâm@Yên", NameProblem.InvalidCharacters)]
        [TestCase("Ông Admin", NameProblem.Banned)]
        [TestCase("Hệ Thống", NameProblem.Banned)]
        public void LordNameRules_AllowVietnameseLettersOnly(string name, NameProblem expected)
        {
            var rules = new LordNameRules { BannedWords = new[] { "admin", "hệ thống" } };
            Assert.That(rules.Validate(name), Is.EqualTo(expected));
        }

        [Test]
        public void LordNameRules_NormalizeSpacesAndDiacritics_AndCheckTakenNames()
        {
            // "ê" typed as e + combining circumflex must be stored like the precomposed letter.
            Assert.That(LordNameRules.Normalize("  Tiên   Dông "), Is.EqualTo("Tiên Dông"));
            Assert.That(LordNameRules.Fold("Đặng Văn"), Is.EqualTo("dangvan"));
            var rules = new LordNameRules();
            Assert.That(rules.Validate("Lâm Tử Yên", name => name == "Lâm Tử Yên"), Is.EqualTo(NameProblem.Taken));
        }

        [Test]
        public void RandomNames_AreValid_AndNeverAFamousGeneral()
        {
            var generator = new RandomNameGenerator(new[] { "Triệu", "Lâm" }, new[] { "Vân", "Tử Yên" }, new[] { "Triệu Vân" });
            var rules = new LordNameRules();
            var random = new System.Random(7);
            for (var i = 0; i < 100; i++)
            {
                var name = generator.Next(random, rules);
                Assert.That(rules.Validate(name), Is.EqualTo(NameProblem.None), name);
                Assert.That(name, Is.Not.EqualTo("Triệu Vân"));
            }
        }

        [Test]
        public void Planner_NewPlayer_SeesEveryScreen()
        {
            var planner = new FrontendFlowPlanner(FrontendFlowPlanner.DefaultOrder);
            var state = new FrontendState(false, false, false, false, false);
            var seen = new List<FrontendStep>();
            for (var step = planner.First(state); step != FrontendStep.Done; step = planner.Next(step, state))
                seen.Add(step);
            Assert.That(seen, Is.EqualTo(FrontendFlowPlanner.DefaultOrder));
        }

        [Test]
        public void Planner_ReturningPlayer_SkipsLordNameAndStory()
        {
            var planner = new FrontendFlowPlanner(FrontendFlowPlanner.DefaultOrder);
            var state = new FrontendState(true, true, true, true, true);
            var seen = new List<FrontendStep>();
            for (var step = planner.First(state); step != FrontendStep.Done; step = planner.Next(step, state))
                seen.Add(step);
            Assert.That(seen, Is.EqualTo(new[] { FrontendStep.Intro, FrontendStep.Login, FrontendStep.ServerSelect, FrontendStep.Battle }));
        }

        [Test]
        public void Planner_FollowsTheOrderFromData()
        {
            var planner = new FrontendFlowPlanner(new[] { FrontendStep.Login, FrontendStep.Intro, FrontendStep.Battle });
            var state = new FrontendState(false, false, false, false, false);
            Assert.That(planner.First(state), Is.EqualTo(FrontendStep.Login));
            Assert.That(planner.Next(FrontendStep.Login, state), Is.EqualTo(FrontendStep.Intro));
            Assert.That(planner.Next(FrontendStep.Battle, state), Is.EqualTo(FrontendStep.Done));
        }

        [Test]
        public void StoryCursor_WalksLinesThenPanels_AndSkips()
        {
            var cursor = new StoryCursor(new[] { 1, 2, 0 });
            Assert.That((cursor.Panel, cursor.Line), Is.EqualTo((0, 0)));
            Assert.That(cursor.Advance(), Is.True);
            Assert.That((cursor.Panel, cursor.Line), Is.EqualTo((1, 0)));
            cursor.Advance();
            Assert.That((cursor.Panel, cursor.Line), Is.EqualTo((1, 1)));
            Assert.That(cursor.Advance(), Is.True, "a panel without lines still takes one tap");
            Assert.That(cursor.Panel, Is.EqualTo(2));
            Assert.That(cursor.Advance(), Is.False);
            Assert.That(cursor.IsFinished, Is.True);

            var skipped = new StoryCursor(new[] { 3, 3 });
            skipped.Skip();
            Assert.That(skipped.IsFinished, Is.True);
            Assert.That(StoryCursor.Format("Ta là {Tên}!", "Lâm Tử Yên"), Is.EqualTo("Ta là Lâm Tử Yên!"));
        }

        [Test]
        public void Profile_OneCharacterPerServer_SurvivesJson()
        {
            var profile = new PlayerProfile { accountId = "guest-1", accountKind = AccountKind.Guest, accountLabel = "Khách" };
            var character = profile.GetOrCreateCharacter("s3");
            character.lordHeroId = "lord_strategist";
            character.name = "Lâm Tử Yên";
            character.AddHero("guardian", 1, 3);
            character.AddHero("guardian", 1, 2);
            Assert.That(profile.GetOrCreateCharacter("s3"), Is.SameAs(character));
            Assert.That(character.FindHero("guardian").stars, Is.EqualTo(3), "a lower-star copy never lowers the stars");

            var copy = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            Assert.That(copy.IsSignedIn, Is.True);
            Assert.That(copy.FindCharacter("s3").name, Is.EqualTo("Lâm Tử Yên"));
            Assert.That(copy.FindCharacter("s3").heroes, Has.Count.EqualTo(1));
            Assert.That(copy.FindCharacter("s1"), Is.Null);
        }

        [Test]
        public void OfflineAccounts_OnlyGuestWorks()
        {
            var accounts = new OfflineAccountService();
            SignInResult guest = null, google = null;
            accounts.SignIn(AccountKind.Guest, null, null, r => guest = r);
            accounts.SignIn(AccountKind.Google, null, null, r => google = r);
            Assert.That(guest.Success, Is.True);
            Assert.That(guest.AccountId, Does.StartWith("guest-"));
            Assert.That(google.Success, Is.False);
            Assert.That(google.Message, Is.Not.Empty);
        }

        [Test]
        public void DefaultServer_IsTheLastOne_ElseTheRecommendedOpenOne()
        {
            var servers = new[]
            {
                new ServerInfo("s1", "S1", ServerStatus.Maintenance, true),
                new ServerInfo("s2", "S2", ServerStatus.Busy, false),
                new ServerInfo("s3", "S3", ServerStatus.New, true)
            };
            Assert.That(StaticServerDirectory.PickDefault(servers, "").Id, Is.EqualTo("s3"), "recommended but open");
            Assert.That(StaticServerDirectory.PickDefault(servers, "s2").Id, Is.EqualTo("s2"));
            Assert.That(StaticServerDirectory.PickDefault(servers, "s1").Id, Is.EqualTo("s3"), "the last one is closed");
            Assert.That(StaticServerDirectory.PickDefault(Array.Empty<ServerInfo>(), ""), Is.Null);
        }
    }
}
