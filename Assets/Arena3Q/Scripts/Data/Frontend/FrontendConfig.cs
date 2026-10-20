using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;
using UnityEngine.Video;

namespace Arena3Q.Data
{
    /// <summary>Colours and optional art of the menu screens; assign sprites when the UI kit art arrives.</summary>
    [Serializable]
    public sealed class UiTheme
    {
        public Color background = new Color(0.07f, 0.06f, 0.06f);
        public Color panel = new Color(0.16f, 0.12f, 0.1f, 0.95f);
        public Color primaryButton = new Color(0.7f, 0.17f, 0.12f);
        public Color secondaryButton = new Color(0.36f, 0.26f, 0.17f);
        public Color disabledButton = new Color(0.32f, 0.32f, 0.32f);
        public Color text = new Color(0.98f, 0.94f, 0.86f);
        public Color accent = new Color(1f, 0.82f, 0.35f);
        public Color error = new Color(1f, 0.5f, 0.45f);
        [Tooltip("Optional 9-sliced sprites (ui_kit_sheet.png pieces) and font (needs Vietnamese diacritics).")]
        public Sprite buttonSprite;
        public Sprite panelSprite;
        public Font font;
    }

    /// <summary>
    /// The game flow from launch to the first battle (spec §2 "Luồng vào game", docs/KeHoach/VaoGame.md): screen
    /// order, intro, look, account rules, content and the tutorial battle. Everything here is meant to be edited.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Frontend/Frontend Config", fileName = "FrontendConfig")]
    public sealed class FrontendConfig : ScriptableObject
    {
        [Header("Flow")]
        [Tooltip("Screen order. Screens the player does not need (lord select for a returning player…) are skipped.")]
        [SerializeField] private FrontendStep[] steps =
        {
            FrontendStep.Intro, FrontendStep.Login, FrontendStep.ServerSelect, FrontendStep.LordSelect,
            FrontendStep.NameEntry, FrontendStep.Story, FrontendStep.Battle
        };

        [Header("Intro")]
        [SerializeField] private string gameTitle = "Arena3Q";
        [SerializeField, Min(0.5f)] private float introSeconds = 3f;
        [Tooltip("intro.mp4 (§5). Empty = an animated logo placeholder.")]
        [SerializeField] private VideoClip introVideo;
        [SerializeField] private Sprite logo;
        [Tooltip("banner_title.png: end of the intro and background of the login and server screens.")]
        [SerializeField] private Sprite banner;

        [Header("Look")]
        [SerializeField] private UiTheme theme = new UiTheme();

        [Header("Accounts")]
        [SerializeField, Min(1)] private int minUsernameLength = 6;
        [SerializeField, Min(1)] private int maxUsernameLength = 20;
        [SerializeField, Min(1)] private int minPasswordLength = 8;
        [SerializeField] private string termsUrl = "";
        [SerializeField] private string privacyUrl = "";

        [Header("Content")]
        [SerializeField] private LordRoster lordRoster;
        [SerializeField] private ServerList serverList;
        [SerializeField] private LordNameConfig names;
        [SerializeField] private StoryScript openingStory;
        [Tooltip("Every non-lord hero a player can own, to rebuild lineups from saved hero ids.")]
        [SerializeField] private HeroDefinition[] heroCatalog = Array.Empty<HeroDefinition>();
        [SerializeField] private ProgressionConfig progression;

        [Header("First battle")]
        [Tooltip("Played once, right after the opening story (ải hướng dẫn 1-1).")]
        [SerializeField] private StageConfig tutorialStage;
        [Tooltip("Heroes every new character receives (Giáp Sĩ 3★ teaches the ultimate, spec decision 2026-10-17).")]
        [SerializeField] private LineupEntry[] starterGifts = Array.Empty<LineupEntry>();
        [Tooltip("Lord level after winning the tutorial (level 2 unlocks the passive).")]
        [SerializeField, Min(1)] private int tutorialRewardLevel = 2;
        [Tooltip("Stage for returning players until there is a lobby and a chapter map.")]
        [SerializeField] private StageConfig defaultStage;
        [SerializeField] private string battleSceneName = "Battle";
        [SerializeField] private string frontendSceneName = "Frontend";
        [Header("Development builds only")]
        [Tooltip("Lord level used in battle by Development players on a device, so every lord skill can be tested " +
                 "(skills unlock at levels 2/5/10/15). 0 = off. The Editor (use Play Combat Test) and release builds " +
                 "always use the saved level.")]
        [SerializeField, Min(0)] private int developmentLordLevel = 15;

        public IReadOnlyList<FrontendStep> Steps => steps;
        public int DevelopmentLordLevel => developmentLordLevel;
        public string GameTitle => gameTitle;
        public float IntroSeconds => introSeconds;
        public VideoClip IntroVideo => introVideo;
        public Sprite Logo => logo;
        public Sprite Banner => banner;
        public UiTheme Theme => theme ?? (theme = new UiTheme());
        public string TermsUrl => termsUrl;
        public string PrivacyUrl => privacyUrl;
        public LordRoster LordRoster => lordRoster;
        public ServerList ServerList => serverList;
        public LordNameConfig Names => names;
        public StoryScript OpeningStory => openingStory;
        public IReadOnlyList<HeroDefinition> HeroCatalog => heroCatalog;
        public ProgressionConfig Progression => progression;
        public StageConfig TutorialStage => tutorialStage;
        public IReadOnlyList<LineupEntry> StarterGifts => starterGifts;
        public int TutorialRewardLevel => tutorialRewardLevel;
        public StageConfig DefaultStage => defaultStage != null ? defaultStage : tutorialStage;
        public string BattleSceneName => battleSceneName;
        public string FrontendSceneName => frontendSceneName;

        public AccountRules CreateAccountRules() => new AccountRules
        {
            MinUsernameLength = minUsernameLength,
            MaxUsernameLength = maxUsernameLength,
            MinPasswordLength = minPasswordLength
        };

        public ProgressionRules CreateProgressionRules() =>
            progression != null ? progression.CreateRules() : new ProgressionRules(new HeroProgressionSettings());

        /// <summary>A hero the player can own, by id (lords come from the roster).</summary>
        public HeroDefinition FindHero(string heroId)
        {
            foreach (var hero in heroCatalog)
            {
                if (hero != null && hero.HeroId == heroId)
                    return hero;
            }
            return lordRoster != null ? lordRoster.Find(heroId)?.lord : null;
        }

        public void Validate(List<string> errors)
        {
            var owner = $"Frontend '{name}'";
            if (steps == null || Array.IndexOf(steps, FrontendStep.Battle) < 0)
                errors.Add($"{owner}: the flow must end in a battle.");
            if (lordRoster == null) errors.Add($"{owner}: lordRoster is not set.");
            else lordRoster.Validate(errors);
            if (serverList == null) errors.Add($"{owner}: serverList is not set.");
            else serverList.Validate(errors);
            if (names == null) errors.Add($"{owner}: names is not set.");
            else names.Validate(errors);
            if (openingStory != null) openingStory.Validate(errors);
            if (tutorialStage == null) errors.Add($"{owner}: tutorialStage is not set.");
            foreach (var gift in starterGifts)
            {
                if (gift?.Hero == null) errors.Add($"{owner}: a starter gift has no hero.");
                else if (gift.Hero.Kind == HeroKind.Lord) errors.Add($"{owner}: a lord cannot be a gift.");
                else if (Array.IndexOf(heroCatalog, gift.Hero) < 0) errors.Add($"{owner}: gift '{gift.Hero.name}' is not in the hero catalog.");
            }
        }
    }
}
