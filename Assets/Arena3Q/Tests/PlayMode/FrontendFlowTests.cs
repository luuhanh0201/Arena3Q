using System.Collections;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Frontend;
using Arena3Q.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Arena3Q.Tests.PlayMode
{
    /// <summary>
    /// The game flow in the real Frontend scene (spec §2 "Luồng vào game"): screens, profile, and the hand-over to the
    /// Battle scene. The profile lives in memory: tests never write files (docs/QuyTac/NhatKySuaLoi.md).
    /// </summary>
    public class FrontendFlowTests
    {
        private InMemoryProfileStore _store;
        private FrontendController _flow;

        [TearDown]
        public void TearDown()
        {
            var transition = Object.FindAnyObjectByType<Arena3Q.UI.SceneTransition>();
            if (transition != null) Object.Destroy(transition.gameObject);
            FrontendServiceLocator.Reset();
            BattleLaunch.Clear();
            FrontendController.ResumeAt = null;
        }

        private IEnumerator LoadFrontend(PlayerProfile profile = null)
        {
            _store = new InMemoryProfileStore(profile);
            FrontendServiceLocator.Override(profiles: _store);
            yield return SceneManager.LoadSceneAsync("Frontend", LoadSceneMode.Single);
            yield return null;
            _flow = Object.FindAnyObjectByType<FrontendController>();
            Assert.That(_flow, Is.Not.Null, "Frontend scene has a FrontendController");
        }

        private T ScreenAs<T>() where T : FrontendScreen
        {
            Assert.That(_flow.Screen, Is.InstanceOf<T>(), $"expected {typeof(T).Name} at step {_flow.Step}");
            return (T)_flow.Screen;
        }

        private void FinishIntro()
        {
            ScreenAs<IntroScreen>().Finish();
            FinishLoading();
        }

        private void FinishLoading()
        {
            if (_flow.Screen is LoadingScreen loading) loading.Tick(_flow.Config.StartupUi.localPreparationSeconds + 1);
        }

        private void GoToNameEntry(int lordIndex)
        {
            FinishIntro();
            ScreenAs<LoginScreen>().PlayAsGuest();
            ScreenAs<ServerSelectScreen>().Enter();
            var lords = ScreenAs<LordSelectScreen>();
            lords.SelectLord(lordIndex);
            lords.Choose();
            Assert.That(lords.IsConfirming, Is.True, "choosing asks for confirmation (the lord is permanent)");
            lords.Confirm();
        }

        private static IEnumerator WaitForBattle(System.Action<BattleController> found)
        {
            BattleController battle = null;
            var deadline = Time.realtimeSinceStartup + 10;
            while (Time.realtimeSinceStartup < deadline && (battle == null || battle.State == null))
            {
                yield return null;
                battle = Object.FindAnyObjectByType<BattleController>();
            }
            Assert.That(battle, Is.Not.Null, "the Battle scene loaded");
            Assert.That(battle.State, Is.Not.Null, "the launched battle started");
            found(battle);
        }

        [UnityTest]
        public IEnumerator NewPlayer_GoesThroughEveryScreen_AndStartsTheTutorialWithTheChosenLord()
        {
            yield return LoadFrontend();
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.Intro));
            Assert.That(ScreenAs<IntroScreen>().CanSkip, Is.False, "the first intro plays to the end");

            GoToNameEntry(2);
            var character = _flow.Character;
            Assert.That(_flow.Profile.accountKind, Is.EqualTo(AccountKind.Guest));
            Assert.That(_flow.Server.Id, Is.EqualTo("s3"), "the recommended open server is preselected");
            Assert.That(character.lordHeroId, Is.EqualTo("lord_strategist"));
            Assert.That(character.FindHero("guardian").stars, Is.EqualTo(3));
            Assert.That(character.FindHero("archer"), Is.Not.Null);

            var naming = ScreenAs<NameEntryScreen>();
            naming.Name = "  Lâm   Tử Yên ";
            Assert.That(naming.Submit(), Is.True);
            Assert.That(character.name, Is.EqualTo("Lâm Tử Yên"), "stored normalized");

            var story = ScreenAs<StoryScreen>();
            Assert.That(story.CurrentText, Does.StartWith("Cuối thời Đông Hán"));
            for (var i = 0; i < 10 && !story.CurrentText.Contains("Ta là"); i++)
                story.Advance();
            Assert.That(story.CurrentText, Is.EqualTo("Ta là Lâm Tử Yên, phụng mệnh dẹp loạn vùng này. Ai cần giúp, cứ nói!"));
            Assert.That(story.CurrentSpeaker, Is.EqualTo("Lâm Tử Yên"), "the lord speaks with the player's name");
            yield return new WaitForSecondsRealtime(_flow.Config.StartupUi.screenSeconds + .1f);
            var outgoingStory = story.Root.gameObject;
            story.Skip();
            Assert.That(outgoingStory.activeInHierarchy, Is.True, "the story stays visible until the scene cover is opaque");
            Assert.That(outgoingStory.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Assert.That(character.storySeen, Is.True);
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.Battle));

            BattleController battle = null;
            yield return WaitForBattle(b => battle = b);
            Assert.That(battle.Stage.StageId, Is.EqualTo("tutorial_01"));
            Assert.That(battle.gameObject.scene.name, Is.EqualTo("Battlefield_HighQuality"), "the real Frontend route must open the newest map");
            Assert.That(Object.FindAnyObjectByType<BattlefieldEnvironment>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<Arena3Q.UI.BattlefieldTestControls>().StageButton.gameObject.activeSelf, Is.False, "campaign players cannot bypass the tutorial with the map test switch");
            Assert.That(battle.Lineup[0].Hero.HeroId, Is.EqualTo("lord_strategist"));
            Assert.That(battle.Lineup[0].Level, Is.EqualTo(1));
            Assert.That(battle.Lineup.Any(c => c.Hero.HeroId == "guardian" && c.Stars == 3), Is.True);
            Assert.That(battle.ReturnSceneName, Is.EqualTo("Frontend"));
            Assert.That(_store.Load().introSeen, Is.True);
            Assert.That(_store.SaveCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator NameEntry_RejectsInvalidNames_AndTheDiceGivesAValidOne()
        {
            yield return LoadFrontend();
            GoToNameEntry(0);
            var naming = ScreenAs<NameEntryScreen>();

            naming.Name = "@@";
            Assert.That(naming.Submit(), Is.False);
            Assert.That(naming.Error, Is.Not.Empty);
            naming.Name = "A";
            Assert.That(naming.Submit(), Is.False);
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.NameEntry));

            naming.Randomize();
            Assert.That(naming.Name, Is.Not.Empty);
            Assert.That(naming.Submit(), Is.True, $"dice name '{naming.Name}'");
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.Story));
        }

        [UnityTest]
        public IEnumerator Login_UnsupportedProviders_Explain_GuestButtonWorks()
        {
            yield return LoadFrontend();
            FinishIntro();
            var login = ScreenAs<LoginScreen>();

            login.SignIn(AccountKind.Google);
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.Login));
            Assert.That(_flow.LastMessage, Does.Contain("Chưa hỗ trợ"));

            login.ShowAccountForm(register: true);
            login.SubmitAccount("abc", "12345678", "12345678");
            Assert.That(login.Error, Does.Contain("email"));

            // The real button, not just the method behind it.
            login.SwitchAccount();
            yield return null; // replaced widgets are destroyed at the end of the frame
            var playNow = login.Root.GetComponentsInChildren<Button>().Single(b => b.name == "PlayNow");
            playNow.onClick.Invoke();
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.ServerSelect));
        }

        [UnityTest]
        public IEnumerator ReturningPlayer_SkipsLordNameAndStory_AndPlaysTheDefaultStage()
        {
            var profile = new PlayerProfile
            {
                accountId = "guest-returning", accountKind = AccountKind.Guest, accountLabel = "Khách", introSeen = true,
                lastServerId = "s2"
            };
            var character = profile.GetOrCreateCharacter("s2");
            character.lordHeroId = "lord_archer";
            character.name = "Tô Phong";
            character.lordLevel = 3;
            character.storySeen = true;
            character.tutorialCleared = true;
            character.AddHero("guardian", 1, 3);
            yield return LoadFrontend(profile);

            var intro = ScreenAs<IntroScreen>();
            Assert.That(intro.CanSkip, Is.True, "a tap skips an intro already seen");
            intro.Finish();
            FinishLoading();
            var login = ScreenAs<LoginScreen>();
            Assert.That(login.IsReturning, Is.True);
            login.Enter();
            var servers = ScreenAs<ServerSelectScreen>();
            Assert.That(servers.Selected.Id, Is.EqualTo("s2"), "the last server");
            servers.Enter();
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.Battle));

            BattleController battle = null;
            yield return WaitForBattle(b => battle = b);
            Assert.That(battle.Stage.StageId, Is.EqualTo("attack_01"), "the tutorial is done: the default stage");
            Assert.That(battle.gameObject.scene.name, Is.EqualTo("Battlefield_HighQuality"), "returning players also use the newest map");
            Assert.That(battle.Lineup[0].Hero.HeroId, Is.EqualTo("lord_archer"));
            Assert.That(battle.Lineup[0].Level, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Screenshot_FrontendScreens()
        {
            // Visual check without a display: Logs/Screenshots/frontend_*.png (Logs is not in git).
            yield return LoadFrontend();
            _flow.Screen.Tick(1.5f);
            yield return null;
            Capture("frontend_1_intro.png");
            FinishIntro();
            yield return null;
            Capture("frontend_2_login.png");
            ScreenAs<LoginScreen>().PlayAsGuest();
            yield return null;
            Capture("frontend_3_servers.png");
            ScreenAs<ServerSelectScreen>().Enter();
            var lords = ScreenAs<LordSelectScreen>();
            lords.SelectLord(2);
            yield return null;
            Capture("frontend_4_lords.png");
            lords.Choose();
            yield return null;
            Capture("frontend_5_confirm.png");
            lords.Confirm();
            yield return null;
            Capture("frontend_6_name.png");
            ScreenAs<NameEntryScreen>().Submit();
            var story = ScreenAs<StoryScreen>();
            for (var i = 0; i < 10 && !story.CurrentText.Contains("Ta là"); i++)
                story.Advance();
            yield return null;
            Capture("frontend_7_story.png");
        }

        private void Capture(string fileName)
        {
            var cam = Camera.main;
            var canvas = _flow.GetComponentInChildren<Canvas>();
            const int width = 1600, height = 900;
            var rt = new RenderTexture(width, height, 24);
            var previousMode = canvas.renderMode;
            try
            {
                cam.targetTexture = rt;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                var directory = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "Screenshots");
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, fileName), texture.EncodeToPNG());
                Object.Destroy(texture);
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = null;
                canvas.renderMode = previousMode;
                rt.Release();
            }
        }

        [UnityTest]
        public IEnumerator TutorialVictory_RaisesTheLordToLevelTwo_ThenBackToTheServerScreen()
        {
            yield return LoadFrontend();
            GoToNameEntry(2);
            ScreenAs<NameEntryScreen>().Submit();
            ScreenAs<StoryScreen>().Skip();

            BattleController battle = null;
            yield return WaitForBattle(b => battle = b);
            battle.AutoSimulate = false;
            // Win the Defense tutorial quickly: every enemy dies as soon as it appears.
            for (var i = 0; i < 4000 && !battle.IsOver; i++)
            {
                foreach (var enemy in battle.Registry.Units.OfType<EnemyUnit>().ToList())
                    enemy.TakeDamage(float.MaxValue, null);
                battle.Simulate(0.05f);
            }
            Assert.That(battle.State.Outcome, Is.EqualTo(BattleOutcome.Victory));

            var character = _store.Load().FindCharacter("s3");
            Assert.That(character.lordLevel, Is.EqualTo(2));
            Assert.That(character.tutorialCleared, Is.True);
            Assert.That(battle.LastResultNote, Does.Contain("cấp 2").And.Contain("Thao Lược"), "the strategist learns her passive");

            battle.ReturnToMenu();
            _flow = null;
            for (var i = 0; i < 30 && (_flow == null || _flow.Step == FrontendStep.Done); i++)
            {
                yield return null;
                _flow = Object.FindAnyObjectByType<FrontendController>();
            }
            Assert.That(_flow, Is.Not.Null);
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.ServerSelect), "back from a battle: straight to the servers");
        }
    }
}
