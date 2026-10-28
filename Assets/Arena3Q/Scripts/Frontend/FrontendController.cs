using System;
using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using Arena3Q.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// Runs the game flow from launch to the first battle (spec §2 "Luồng vào game", docs/KeHoach/VaoGame.md): shows one
    /// screen at a time in the order of <see cref="FrontendConfig"/>, skips what the player does not need
    /// (<see cref="FrontendFlowPlanner"/>), keeps the profile and hands the battle to the Battle scene.
    /// </summary>
    public sealed class FrontendController : MonoBehaviour
    {
        private const float MessageSeconds = 2.5f;

        [SerializeField] private FrontendConfig config;

        /// <summary>Screen to open on the next load instead of the first one (coming back from a battle).</summary>
        public static FrontendStep? ResumeAt { get; set; }

        private FrontendFlowPlanner _planner;
        private RectTransform _screens;
        private Text _message;
        private float _messageTimer;

        public FrontendConfig Config => config;
        public FrontendUi Ui { get; private set; }
        public StartupUi StartupUi { get; private set; }
        public StatusBar StatusBar { get; private set; }
        public FrontendMusic Music { get; private set; }
        public PlayerProfile Profile { get; private set; }
        public ProgressionRules Rules { get; private set; }
        public IReadOnlyList<ServerInfo> Servers { get; private set; } = Array.Empty<ServerInfo>();
        public event Action ServersChanged;
        public ServerInfo Server { get; private set; }
        public CharacterRecord Character => Server != null ? Profile.FindCharacter(Server.Id) : null;
        public FrontendStep Step { get; private set; } = FrontendStep.Done;
        public FrontendScreen Screen { get; private set; }
        public IAccountService Accounts => FrontendServiceLocator.Accounts;
        public AccountRules AccountRules { get; private set; }
        public float VerificationReadyAt { get; set; }

        /// <summary>The last message shown to the player (for tests and logs).</summary>
        public string LastMessage { get; private set; } = "";

        public FrontendState State => new FrontendState(
            Profile != null && Profile.IsSignedIn,
            Server != null,
            Character != null && Character.HasLord,
            Character != null && Character.HasName,
            Character != null && Character.storySeen,
            Character != null && Character.tutorialCleared);

        /// <summary>Sets up a controller created from code (editor tooling and tests).</summary>
        public void Configure(FrontendConfig frontendConfig) => config = frontendConfig;

        private void Awake()
        {
            UiFactory.EnsureEventSystem();
            var canvas = UiFactory.CreateCanvas(transform, "FrontendCanvas", 10);
            if (config != null && config.StartupUi != null)
            {
                canvas.GetComponent<CanvasScaler>().referenceResolution = config.StartupUi.referenceSize;
                canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = .5f;
                var audio = gameObject.AddComponent<UIAudio>(); audio.Configure(config.StartupUi);
                StartupUi = new StartupUi(config.StartupUi, audio);
                SceneTransition.Configure(config.StartupUi.screenSeconds);
                var music = new GameObject("FrontendMusic"); music.transform.SetParent(transform, false);
                Music = music.AddComponent<FrontendMusic>(); Music.Configure(config.StartupUi);
            }
            var root = (RectTransform)canvas.transform;
            Ui = new FrontendUi(config != null ? config.Theme : null);
            Ui.Fill(root, "Background", Ui.Theme.background);
            _screens = UiFactory.Stretch("Screens", root);
            _message = Ui.Label(root, "Message", "", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 150f),
                new Vector2(1400f, 120f), Ui.Theme.accent);
            _message.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            _message.enabled = false;
            if (StartupUi != null)
            {
                // Corner chrome stays above every screen and popup, inside the actual device safe area.
                var statusRoot = UiFactory.Stretch("StatusSafeArea", root);
                statusRoot.gameObject.AddComponent<SafeAreaFitter>().Apply();
                StatusBar = statusRoot.gameObject.AddComponent<StatusBar>(); StatusBar.Configure(StartupUi);
                _message.fontSize = 18;
                _message.rectTransform.sizeDelta = new Vector2(700, 74);
                _message.rectTransform.anchoredPosition = new Vector2(0, 80);
                _message.transform.SetAsLastSibling();
            }
        }

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("[Arena3Q] FrontendController has no FrontendConfig.", this);
                return;
            }
            var errors = new List<string>();
            config.Validate(errors);
            foreach (var error in errors)
                Debug.LogError($"[Arena3Q] {error}", config);
            Begin();
        }

        /// <summary>Loads the profile and opens the first screen (or <see cref="ResumeAt"/>).</summary>
        public void Begin()
        {
            Profile = FrontendServiceLocator.Profiles.Load();
            Rules = config.CreateProgressionRules();
            AccountRules = config.CreateAccountRules();
            _planner = new FrontendFlowPlanner(config.Steps);
            var resume = ResumeAt;
            ResumeAt = null;
            FrontendServiceLocator.ServersFor(config).Fetch(list =>
            {
                if (this == null) return;
                Servers = list ?? Array.Empty<ServerInfo>(); ServersChanged?.Invoke();
                if (resume.HasValue && Profile.IsSignedIn)
                {
                    foreach (var server in Servers)
                        if (server.Id == Profile.lastServerId) { Server = server; break; }
                    Show(resume == FrontendStep.Lobby && (Character == null || !Character.HasLord || !Character.HasName)
                        ? FrontendStep.ServerSelect : resume.Value);
                }
                else Show(_planner.First(State));
            });
        }

        /// <summary>Called by a screen when the player is done with it.</summary>
        public void Complete()
        {
            if (Step == FrontendStep.Done)
                return;
            Show(_planner.Next(Step, State));
        }

        private void Show(FrontendStep step)
        {
            Screen?.Hide(transition: step != FrontendStep.Done);
            Screen = null;
            Step = step;
            if (step != FrontendStep.Intro && step != FrontendStep.Done && Music != null) Music.Play();
            switch (step)
            {
                case FrontendStep.Intro: Screen = new IntroScreen(); break;
                case FrontendStep.Loading: Screen = new LoadingScreen(); break;
                case FrontendStep.Login: Screen = new LoginScreen(); break;
                case FrontendStep.ServerSelect: Screen = new ServerSelectScreen(); break;
                case FrontendStep.LordSelect: Screen = new LordSelectScreen(); break;
                case FrontendStep.NameEntry: Screen = new NameEntryScreen(); break;
                case FrontendStep.Story: Screen = new StoryScreen(); break;
                case FrontendStep.Lobby: Screen = new LobbyScreen(); break;
                case FrontendStep.TutorialBattle:
                case FrontendStep.Battle: StartBattle(); return;
                default: return;
            }
            Screen.Show(this, _screens);
        }

        public void ReturnToLogin() => Show(FrontendStep.Login);
        public void EnterBattleFromLobby()
        {
            if (Step != FrontendStep.Lobby) return;
            if (!string.IsNullOrEmpty(config.ChapterMapSceneName))
            {
                Save(); ResumeAt = FrontendStep.Lobby; SceneTransition.Load(config.ChapterMapSceneName);
            }
            else Show(FrontendStep.Battle);
        }

        private void OnDestroy() { Screen?.Hide(); Screen = null; }

        private void Update()
        {
            Screen?.Tick(Time.unscaledDeltaTime);
            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.unscaledDeltaTime;
                if (_messageTimer <= 0f)
                    _message.enabled = false;
            }
        }

        public void ShowMessage(string text)
        {
            LastMessage = text ?? "";
            _message.text = LastMessage;
            _message.enabled = !string.IsNullOrEmpty(LastMessage);
            _messageTimer = MessageSeconds;
        }

        public void Save() => FrontendServiceLocator.Profiles.Save(Profile);

        // ---- What the screens change ----

        public void SignIn(SignInResult result)
        {
            Profile.accountId = result.AccountId;
            Profile.accountKind = result.Kind;
            Profile.accountLabel = result.Label;
            Save();
        }

        /// <summary>Forgets the account on this device (for a guest this deletes its progress).</summary>
        public void SignOut()
        {
            Profile.Clear();
            Server = null;
            Save();
        }

        public void SelectServer(ServerInfo server)
        {
            Server = server;
            Profile.lastServerId = server.Id;
            Save();
        }

        /// <summary>Creates the character of this server with the chosen lord and the starter heroes (fixed for the game, spec §2).</summary>
        public void ChooseLord(LordRoster.Entry lord)
        {
            var character = Profile.GetOrCreateCharacter(Server.Id);
            character.lordHeroId = lord.lord.HeroId;
            character.lordLevel = Math.Max(1, character.lordLevel);
            foreach (var gift in config.StarterGifts)
            {
                if (gift?.Hero != null)
                    character.AddHero(gift.Hero.HeroId, gift.Level, gift.Stars);
            }
            Save();
        }

        public void SetLordName(string lordName)
        {
            Character.name = lordName;
            Save();
        }

        public void MarkStorySeen()
        {
            Character.storySeen = true;
            Save();
        }

        public LordRoster.Entry ChosenLord => Character != null && config.LordRoster != null ? config.LordRoster.Find(Character.lordHeroId) : null;

        // ---- Battle ----

        /// <summary>The player's cards: the lord at its level, then the owned heroes with their level and stars.</summary>
        public IReadOnlyList<LineupEntry> BuildLineup()
        {
            var lineup = new List<LineupEntry>();
            var character = Character;
            var lord = ChosenLord;
            if (character == null || lord == null)
                return lineup;
            // Development players (not the Editor, which has Play Combat Test) may raise the lord to a test level
            // (FrontendConfig); the saved profile is untouched.
            var lordLevel = !Application.isEditor && Debug.isDebugBuild && config.DevelopmentLordLevel > 0
                ? Mathf.Max(character.lordLevel, config.DevelopmentLordLevel) : character.lordLevel;
            lineup.Add(new LineupEntry(lord.lord, lordLevel, 1, character.lordSkillLevels));
            foreach (var owned in character.heroes)
            {
                var hero = config.FindHero(owned.heroId);
                if (hero != null && hero.Kind != HeroKind.Lord)
                    lineup.Add(new LineupEntry(hero, owned.level, owned.stars));
            }
            return lineup;
        }

        private void StartBattle()
        {
            var character = Character;
            if (character == null || ChosenLord == null)
            {
                Debug.LogError("[Arena3Q] Cannot start a battle without a character.", this);
                return;
            }

            var tutorial = !character.tutorialCleared;
            var stage = tutorial ? config.TutorialStage : config.DefaultStage;
            // The callback outlives this scene: it only captures plain data, never this MonoBehaviour.
            var store = FrontendServiceLocator.Profiles;
            var profile = Profile;
            var tutorialStage = config.TutorialStage;
            var rewardLevel = config.TutorialRewardLevel;
            var rules = Rules;
            var lord = ChosenLord;
            BattleLaunch.Request(new BattleLaunchRequest
            {
                Stage = stage,
                Lineup = BuildLineup(),
                ReturnScene = config.FrontendSceneName,
                OnFinished = (playedStage, decision) =>
                    BattleRewards.Apply(store, profile, character, playedStage == tutorialStage, decision, rewardLevel, rules, lord),
                OnReturn = () => ResumeAt = FrontendStep.Lobby,
                // Like the lord level: Development players on a device only.
                FullRageAtStart = !Application.isEditor && Debug.isDebugBuild && config.DevelopmentFullRage
            });
            SceneTransition.Load(config.BattleSceneName);
        }
    }

    /// <summary>What a battle gives the player (prototype: the tutorial's level-up; stage rewards come later ⏳).</summary>
    public static class BattleRewards
    {
        /// <returns>A note for the result screen, or null.</returns>
        public static string Apply(IPlayerProfileStore store, PlayerProfile profile, CharacterRecord character, bool tutorial,
            OutcomeDecision decision, int tutorialRewardLevel, ProgressionRules rules, LordRoster.Entry lord)
        {
            if (!tutorial || decision.Outcome != BattleOutcome.Victory)
                return null;

            var before = character.lordLevel;
            character.tutorialCleared = true;
            character.lordLevel = rules.ClampLevel(Math.Max(character.lordLevel, tutorialRewardLevel));
            store.Save(profile);
            if (character.lordLevel == before)
                return null;

            var note = $"Chủ tướng lên cấp {character.lordLevel}!";
            foreach (SkillSlot slot in Enum.GetValues(typeof(SkillSlot)))
            {
                var unlockedNow = rules.IsSkillUnlocked(HeroKind.Lord, slot, character.lordLevel, 1);
                var unlockedBefore = rules.IsSkillUnlocked(HeroKind.Lord, slot, before, 1);
                if (unlockedNow && !unlockedBefore)
                    note += $" Học được {SkillSlotNames.Of(slot)}: {lord?.SkillName(slot)}.";
            }
            return note;
        }
    }

    /// <summary>Vietnamese names of the skill slots (spec §3).</summary>
    public static class SkillSlotNames
    {
        public static string Of(SkillSlot slot)
        {
            switch (slot)
            {
                case SkillSlot.Passive: return "kỹ năng nội tại";
                case SkillSlot.Skill2: return "kỹ năng 2";
                case SkillSlot.Skill3: return "kỹ năng 3";
                default: return "tuyệt kỹ";
            }
        }
    }
}
