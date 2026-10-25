using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arena3Q.UI
{
    /// <summary>
    /// Battle HUD built from code: card bar, timer / wave / hero count, rejection toasts, off-screen hero icons and the
    /// result screen with stars and replay.
    /// </summary>
    public sealed class BattleHud : MonoBehaviour
    {
        private const float ToastSeconds = 1.6f;

        [SerializeField] private BattleController battle;
        [SerializeField] private BattleCameraController cameraController;

        private readonly List<HeroCardView> _cards = new List<HeroCardView>();
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private Canvas _canvas;
        private RectTransform _cardBar;
        private Text _stageLabel;
        private Text _timerLabel;
        private Text _infoLabel;
        private Text _toast;
        private float _toastTimer;
        private float _resultDelay;
        private OffscreenHeroIndicators _indicators;
        private GameObject _resultPanel;
        private Text _resultTitle;
        private Text _resultReason;
        private Image[] _resultStars;
        private RectTransform _resultButtons;
        private Slider _tiltSlider;
        private Text _tiltLabel;
        private RectTransform _bossBar;
        private Image _bossFill;
        private Text _bossLabel;
        private Text _formationLabel;
        private Text _formationWarning;

        public BattleController Battle => battle;
        public BattleCameraController Camera => cameraController;
        public RectTransform OverlayRoot { get; private set; }
        public bool IsResultVisible => _resultPanel != null && _resultPanel.activeSelf;
        public IReadOnlyList<HeroCardView> CardViews => _cards;
        public Slider TiltSlider => _tiltSlider;

        /// <summary>The boss health bar (spec §8 ✅): shown while a boss is alive.</summary>
        public bool IsBossBarVisible => _bossBar != null && _bossBar.gameObject.activeSelf;
        public float BossBarFraction => _bossFill != null ? _bossFill.fillAmount : 0f;
        public string BossBarText => _bossLabel != null ? _bossLabel.text : "";

        public void Bind(BattleController controller, BattleCameraController cameraRig)
        {
            Unsubscribe();
            battle = controller;
            cameraController = cameraRig;
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void Awake()
        {
            UiFactory.EnsureEventSystem();
            BuildLayout();
        }

        private void OnEnable()
        {
            Subscribe();
            if (battle != null && battle.State != null)
                OnBattleStarted();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (battle == null)
                return;
            battle.BattleStarted += OnBattleStarted;
            battle.Changed += Refresh;
            battle.ActionRejected += OnActionRejected;
            battle.Finished += OnFinished;
            battle.ViewChanged += SyncTiltSlider;
        }

        private void Unsubscribe()
        {
            if (battle == null)
                return;
            battle.BattleStarted -= OnBattleStarted;
            battle.Changed -= Refresh;
            battle.ActionRejected -= OnActionRejected;
            battle.Finished -= OnFinished;
            battle.ViewChanged -= SyncTiltSlider;
        }

        private void SyncTiltSlider()
        {
            if (_tiltSlider == null || battle == null)
                return;
            _tiltSlider.SetValueWithoutNotify(battle.ViewTiltDegrees);
            _tiltLabel.text = $"Góc nhìn: {Mathf.RoundToInt(battle.ViewTiltDegrees)}°";
        }

        // 2.5D view tilt, 0° (top-down) to 45°, for debugging only (map spec §8.3): shown in the Editor and in
        // Development builds, hidden in release builds where the angle stays at the art's angle. Session only.
        private void BuildTiltSlider(RectTransform root)
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            var topLeft = new Vector2(0f, 1f);
            _tiltLabel = UiFactory.CreateLabel("TiltLabel", root, topLeft, topLeft, new Vector2(30f, -74f),
                new Vector2(320f, 40f), "Góc nhìn: 0°", 28, TextAnchor.MiddleLeft, Color.white);

            var sliderRect = UiFactory.CreateRect("TiltSlider", root, topLeft, topLeft, topLeft, new Vector2(30f, -116f),
                new Vector2(320f, 34f));
            var background = UiFactory.CreateRect("Background", sliderRect, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UiFactory.AddImage(background, new Color(0f, 0f, 0f, 0.55f));

            var fillArea = UiFactory.CreateRect("Fill Area", sliderRect, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-34f, 0f));
            var fill = UiFactory.CreateRect("Fill", fillArea, Vector2.zero, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            UiFactory.AddImage(fill, new Color(1f, 0.85f, 0.35f, 0.9f), null, false);

            var handleArea = UiFactory.CreateRect("Handle Slide Area", sliderRect, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-34f, 0f));
            var handle = UiFactory.CreateRect("Handle", handleArea, Vector2.zero, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(34f, 0f));
            var handleImage = UiFactory.AddImage(handle, Color.white, PlaceholderArt.Circle);

            _tiltSlider = sliderRect.gameObject.AddComponent<Slider>();
            _tiltSlider.fillRect = fill;
            _tiltSlider.handleRect = handle;
            _tiltSlider.targetGraphic = handleImage;
            _tiltSlider.direction = Slider.Direction.LeftToRight;
            _tiltSlider.minValue = 0f;
            _tiltSlider.maxValue = BattleController.MaxViewTiltDegrees;
            _tiltSlider.wholeNumbers = true;
            _tiltSlider.onValueChanged.AddListener(value =>
            {
                if (battle != null)
                    battle.ViewTiltDegrees = value;
                _tiltLabel.text = $"Góc nhìn: {Mathf.RoundToInt(value)}°";
            });
            // Spec §9 / MapSpecification 8.3: the view angle is a debugging aid (Editor and Development builds);
            // release builds keep the art's angle and do not show the slider.
            var debugTools = Application.isEditor || Debug.isDebugBuild;
            sliderRect.gameObject.SetActive(debugTools);
            _tiltLabel.gameObject.SetActive(debugTools);
        }

        public void FocusSelectedHero()
        {
            var hero = battle.SelectedHero;
            if (hero != null && cameraController != null)
                cameraController.FocusOn(hero.Position);
        }

        /// <summary>Top edge of the card bar as a viewport fraction (0 = bottom of the screen).</summary>
        public float CardBarTopViewport
        {
            get
            {
                if (_cardBar == null || Screen.height <= 0)
                    return 0f;
                var corners = new Vector3[4];
                _cardBar.GetWorldCorners(corners); // Screen Space Overlay: world corners are screen pixels.
                return Mathf.Clamp01(corners[1].y / Screen.height);
            }
        }

        public bool IsOverUi(Vector2 screen)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;
            _uiHits.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screen }, _uiHits);
            return _uiHits.Count > 0;
        }

        // ---- Layout ----

        private void BuildLayout()
        {
            _canvas = UiFactory.CreateCanvas(transform, "HUD Canvas", 10);
            var root = (RectTransform)_canvas.transform;
            root = UiFactory.Stretch("Safe HUD", root);
            root.gameObject.AddComponent<UiSafeArea>();

            // Off-screen icons sit under everything else so cards stay tappable.
            var indicatorRoot = UiFactory.Stretch("EdgeIcons", root);
            _indicators = indicatorRoot.gameObject.AddComponent<OffscreenHeroIndicators>();
            _indicators.Init(this, indicatorRoot);

            var top = new Vector2(0.5f, 1f);
            _stageLabel = UiFactory.CreateLabel("Stage", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -20f),
                new Vector2(600f, 50f), "", 34, TextAnchor.UpperLeft, Color.white);
            _timerLabel = UiFactory.CreateLabel("Timer", root, top, top, new Vector2(0f, -16f), new Vector2(400f, 70f), "",
                56, TextAnchor.UpperCenter, Color.white);
            _infoLabel = UiFactory.CreateLabel("Info", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -20f),
                new Vector2(600f, 100f), "", 32, TextAnchor.UpperRight, Color.white);
            _toast = UiFactory.CreateLabel("Toast", root, top, top, new Vector2(0f, -150f), new Vector2(1000f, 60f), "",
                38, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.4f));
            _toast.enabled = false;

            // Bát Quái Trận: time to the next rotation and who holds the altar; a warning before each rotation.
            _formationLabel = UiFactory.CreateLabel("Formation", root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -160f), new Vector2(560f, 36f), "", 24, TextAnchor.UpperLeft, new Color(0.9f, 0.9f, 0.8f));
            _formationWarning = UiFactory.CreateLabel("FormationWarning", root, top, top, new Vector2(0f, -210f),
                new Vector2(1000f, 60f), "", 40, TextAnchor.MiddleCenter, new Color(1f, 0.75f, 0.3f));

            // Boss health bar under the timer (spec §8 ✅ "thanh máu riêng trên HUD").
            _bossBar = UiFactory.CreateRect("BossBar", root, top, top, top, new Vector2(0f, -92f), new Vector2(640f, 46f));
            _bossLabel = UiFactory.CreateLabel("BossName", _bossBar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
                new Vector2(640f, 26f), "", 22, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.75f));
            var barBack = UiFactory.CreateRect("Back", _bossBar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 16f));
            UiFactory.AddImage(barBack, new Color(0f, 0f, 0f, 0.7f), null, false);
            _bossFill = UiFactory.AddImage(UiFactory.Stretch("Fill", barBack), new Color(0.85f, 0.15f, 0.12f), PlaceholderArt.Square, false);
            _bossFill.type = Image.Type.Filled;
            _bossFill.fillMethod = Image.FillMethod.Horizontal;
            _bossBar.gameObject.SetActive(false);

            _cardBar = UiFactory.CreateRect("CardBar", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1000f, HeroCardView.CardSize.y));
            var layout = _cardBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            BuildTiltSlider(root);
            BuildResultPanel(root);

            OverlayRoot = UiFactory.Stretch("DragOverlay", root);
        }

        private void BuildResultPanel(RectTransform root)
        {
            var panel = UiFactory.Stretch("Result", root);
            UiFactory.AddImage(panel, new Color(0f, 0f, 0f, 0.6f));
            _resultPanel = panel.gameObject;

            var center = new Vector2(0.5f, 0.5f);
            var box = UiFactory.CreateRect("Box", panel, center, center, center, Vector2.zero, new Vector2(900f, 620f));
            UiFactory.AddImage(box, new Color(0.1f, 0.12f, 0.18f, 0.97f));

            _resultTitle = UiFactory.CreateLabel("Title", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(800f, 90f), "", 72, TextAnchor.MiddleCenter, Color.white);
            _resultReason = UiFactory.CreateLabel("Reason", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -140f), new Vector2(800f, 50f), "", 34, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.9f));

            _resultStars = new Image[StarRating.MaxStars];
            for (var i = 0; i < _resultStars.Length; i++)
            {
                var star = UiFactory.CreateRect($"Star{i}", box, center, center, center,
                    new Vector2((i - 1) * 170f, 30f), new Vector2(150f, 150f));
                _resultStars[i] = UiFactory.AddImage(star, Color.gray, PlaceholderArt.Star, false);
            }

            _resultButtons = UiFactory.CreateRect("Buttons", box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(860f, 110f));
            _resultPanel.SetActive(false);
        }

        // ---- Battle events ----

        private void OnBattleStarted()
        {
            foreach (var card in _cards)
                Destroy(card.gameObject);
            _cards.Clear();
            _indicators.ClearAll();
            _resultPanel.SetActive(false);
            _resultDelay = 0f;

            var lineup = battle.Lineup;
            _cardBar.sizeDelta = new Vector2(lineup.Count * (HeroCardView.CardSize.x + 18f), HeroCardView.CardSize.y);
            for (var i = 0; i < lineup.Count; i++)
            {
                var rect = UiFactory.CreateRect($"Card {i}", _cardBar, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f),
                    Vector2.zero, HeroCardView.CardSize);
                var view = rect.gameObject.AddComponent<HeroCardView>();
                view.Build(this, i, lineup[i], IsLord(i));
                _cards.Add(view);
            }

            var stage = battle.Stage;
            _stageLabel.text = $"{(stage.StageType == StageType.Attack ? "Ải Công" : "Ải Thủ")} · {stage.StageId}";
            SyncTiltSlider();
            Refresh();
        }

        private bool IsLord(int cardIndex) =>
            cardIndex >= 0 && cardIndex < battle.Lineup.Count && battle.Lineup[cardIndex].Hero.Kind == HeroKind.Lord;

        private void Refresh()
        {
            var state = battle.State;
            if (state == null)
                return;
            for (var i = 0; i < _cards.Count && i < state.Cards.Count; i++)
            {
                var card = state.Cards[i];
                _cards[i].Refresh(card.State, card.Hero == state.SelectedHero && card.Hero.IsValid, IsLord(i));
            }
            UpdateInfo();
        }

        private void UpdateInfo()
        {
            var state = battle.State;
            var info = $"Tướng {state.SummonedCount}/{SummonRules.MaxHeroesPerBattle} · Mất {state.HeroesLost}";
            if (state.StageType == StageType.Defense && battle.WaveCount > 0)
                info += $"\nĐợt {Mathf.Max(0, state.CurrentWave + 1)}/{battle.WaveCount}";
            _infoLabel.text = info;
        }

        private void OnActionRejected(RejectReason reason)
        {
            var message = Describe(reason);
            if (string.IsNullOrEmpty(message))
                return;
            _toast.text = message;
            _toast.enabled = true;
            _toastTimer = ToastSeconds;
        }

        private static string Describe(RejectReason reason)
        {
            switch (reason)
            {
                case RejectReason.HeroLimitReached: return "Đã đạt tối đa 5 tướng";
                case RejectReason.HeroDeadCannotResummon: return "Tướng đã chết, không dùng lại được";
                case RejectReason.OutsideSummonZone: return "Chỉ thả tướng trong vùng triệu hồi";
                case RejectReason.NoQuickSummonPoint: return "Ải không có điểm triệu hồi";
                case RejectReason.OutsideBattlefield: return "Ngoài chiến trường";
                case RejectReason.InForbiddenZone: return "Không đi vào vùng cấm được";
                case RejectReason.TargetNotHostile: return "Chỉ tấn công được quân địch";
                case RejectReason.SkillNotReady: return "Kỹ năng chưa thể sử dụng";
                case RejectReason.NoSkillTarget: return "Không có mục tiêu cho kỹ năng";
                default: return null;
            }
        }

        private void OnFinished(OutcomeDecision decision)
        {
            var victory = decision.Outcome == BattleOutcome.Victory;
            _resultTitle.text = victory ? "CHIẾN THẮNG" : "THẤT BẠI";
            _resultTitle.color = victory ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.35f);
            _resultReason.text = DescribeOutcome(decision.Reason) + $" · Mất {battle.State.HeroesLost} tướng";
            // Rewards from the game flow (e.g. the lord's level-up after the tutorial).
            if (!string.IsNullOrEmpty(battle.LastResultNote))
                _resultReason.text += "\n" + battle.LastResultNote;
            for (var i = 0; i < _resultStars.Length; i++)
                _resultStars[i].color = i < battle.State.Stars ? new Color(1f, 0.82f, 0.2f) : new Color(0.3f, 0.3f, 0.35f);

            foreach (Transform child in _resultButtons)
                Destroy(child.gameObject);
            var stages = battle.Stages;
            var canReturn = !string.IsNullOrEmpty(battle.ReturnSceneName);
            var count = 1 + stages.Count + (canReturn ? 1 : 0);
            var width = 860f / count - 16f;
            var replay = UiFactory.CreateButton("Replay", _resultButtons, new Vector2(0f, 0.5f),
                new Vector2(width * 0.5f + 8f, 0f), new Vector2(width, 100f), "Chơi lại", new Color(0.2f, 0.55f, 0.3f));
            replay.onClick.AddListener(() => battle.Restart());
            for (var i = 0; i < stages.Count; i++)
            {
                var index = i;
                var stage = stages[i];
                var label = stage.StageType == StageType.Attack ? "Ải Công" : "Ải Thủ";
                var button = UiFactory.CreateButton($"Stage {i}", _resultButtons, new Vector2(0f, 0.5f),
                    new Vector2((i + 1) * (width + 16f) + width * 0.5f + 8f, 0f), new Vector2(width, 100f),
                    $"{label}: {stage.StageId}", new Color(0.25f, 0.35f, 0.6f));
                button.GetComponentInChildren<Text>().fontSize = 26;
                button.onClick.AddListener(() => battle.StartBattle(index));
            }
            if (canReturn)
            {
                var back = UiFactory.CreateButton("ReturnToMenu", _resultButtons, new Vector2(0f, 0.5f),
                    new Vector2((count - 1) * (width + 16f) + width * 0.5f + 8f, 0f), new Vector2(width, 100f),
                    "Về màn chính", new Color(0.45f, 0.3f, 0.15f));
                back.GetComponentInChildren<Text>().fontSize = 26;
                back.onClick.AddListener(() => SceneTransition.Run(battle.ReturnToMenu));
            }
            // A falling castle is shown first (its animation runs behind an empty screen, not behind the panel).
            _resultDelay = battle.ResultDelaySeconds;
            _resultPanel.SetActive(_resultDelay <= 0f);
            Refresh();
        }

        private static string DescribeOutcome(OutcomeReason reason)
        {
            switch (reason)
            {
                case OutcomeReason.EnemyCastleDestroyed: return "Đã phá thành địch";
                case OutcomeReason.TimeUp: return "Hết giờ";
                case OutcomeReason.AllHeroesLost: return "Toàn bộ tướng đã chết";
                case OutcomeReason.PlayerCastleDestroyed: return "Thành ta bị phá";
                case OutcomeReason.SurvivedTimeLimit: return "Trụ vững tới hết giờ";
                case OutcomeReason.AllWavesCleared: return "Đã đánh tan mọi đợt tấn công";
                default: return "";
            }
        }

        private void UpdateBossBar()
        {
            EnemyUnit boss = null;
            foreach (var candidate in battle.LivingBosses)
            {
                boss = candidate;
                break;
            }
            _bossBar.gameObject.SetActive(boss != null);
            if (boss == null)
                return;
            _bossFill.fillAmount = boss.Health / boss.MaxHealth;
            _bossLabel.text = boss.InPhaseTwo ? $"{boss.DisplayName} · Pha 2" : boss.DisplayName;
        }

        /// <summary>The Bát Quái Trận warning line (docs/BanDo/BatQuaiTran.md §3), shown before a rotation.</summary>
        public string FormationWarningText => _formationWarning != null && _formationWarning.enabled ? _formationWarning.text : "";

        private void UpdateFormation()
        {
            var formation = battle.Formation;
            _formationLabel.enabled = formation != null;
            _formationWarning.enabled = formation != null && formation.IsWarning && !battle.IsOver;
            if (formation == null)
                return;
            var holder = formation.AltarOwner switch
            {
                Team.Player => "Ta",
                Team.Enemy => "Địch",
                _ => "Chưa có"
            };
            var blessed = formation.BlessedTeam == Team.Player ? " · Trận nhãn phù hộ quân ta"
                : formation.BlessedTeam == Team.Enemy ? " · Trận nhãn phù hộ quân địch" : "";
            _formationLabel.text = $"Trận đồ xoay sau {Mathf.CeilToInt(formation.SecondsToRotation)} s · Trận nhãn: {holder}{blessed}";
            if (_formationWarning.enabled)
                _formationWarning.text = $"Bát Quái Trận sắp xoay: {Mathf.CeilToInt(formation.SecondsToRotation)}";
        }

        private void Update()
        {
            if (battle == null || battle.State == null)
                return;
            var state = battle.State;
            var seconds = state.HasTimeLimit ? state.RemainingTime : state.ElapsedTime;
            var whole = Mathf.CeilToInt(seconds);
            _timerLabel.text = $"{whole / 60:00}:{whole % 60:00}";
            _timerLabel.color = state.HasTimeLimit && seconds <= 20f ? new Color(1f, 0.45f, 0.4f) : Color.white;
            if (state.StageType == StageType.Defense)
                UpdateInfo();
            UpdateBossBar();
            UpdateFormation();

            if (_resultDelay > 0f)
            {
                _resultDelay -= Time.unscaledDeltaTime;
                if (_resultDelay <= 0f && battle.IsOver)
                    _resultPanel.SetActive(true);
            }

            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                if (_toastTimer <= 0f)
                    _toast.enabled = false;
            }
        }
    }
}
