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
        private OffscreenHeroIndicators _indicators;
        private GameObject _resultPanel;
        private Text _resultTitle;
        private Text _resultReason;
        private Image[] _resultStars;
        private RectTransform _resultButtons;

        public BattleController Battle => battle;
        public BattleCameraController Camera => cameraController;
        public RectTransform OverlayRoot { get; private set; }
        public bool IsResultVisible => _resultPanel != null && _resultPanel.activeSelf;
        public IReadOnlyList<HeroCardView> CardViews => _cards;

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
        }

        private void Unsubscribe()
        {
            if (battle == null)
                return;
            battle.BattleStarted -= OnBattleStarted;
            battle.Changed -= Refresh;
            battle.ActionRejected -= OnActionRejected;
            battle.Finished -= OnFinished;
        }

        public void FocusSelectedHero()
        {
            var hero = battle.SelectedHero;
            if (hero != null && cameraController != null)
                cameraController.FocusOn(hero.Position);
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
            _toast = UiFactory.CreateLabel("Toast", root, top, top, new Vector2(0f, -110f), new Vector2(1000f, 60f), "",
                38, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.4f));
            _toast.enabled = false;

            _cardBar = UiFactory.CreateRect("CardBar", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1000f, HeroCardView.CardSize.y));
            var layout = _cardBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

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

            var heroes = battle.Cards;
            _cardBar.sizeDelta = new Vector2(heroes.Count * (HeroCardView.CardSize.x + 18f), HeroCardView.CardSize.y);
            for (var i = 0; i < heroes.Count; i++)
            {
                var rect = UiFactory.CreateRect($"Card {i}", _cardBar, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f),
                    Vector2.zero, HeroCardView.CardSize);
                var view = rect.gameObject.AddComponent<HeroCardView>();
                view.Build(this, i, heroes[i], IsLord(i));
                _cards.Add(view);
            }

            var stage = battle.Stage;
            _stageLabel.text = $"{(stage.StageType == StageType.Attack ? "Ải Công" : "Ải Thủ")} · {stage.StageId}";
            Refresh();
        }

        private bool IsLord(int cardIndex) => cardIndex == 0 && battle.Stage.TestLoadout.LordHero != null;

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
                default: return null;
            }
        }

        private void OnFinished(OutcomeDecision decision)
        {
            var victory = decision.Outcome == BattleOutcome.Victory;
            _resultTitle.text = victory ? "CHIẾN THẮNG" : "THẤT BẠI";
            _resultTitle.color = victory ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.35f);
            _resultReason.text = DescribeOutcome(decision.Reason) + $" · Mất {battle.State.HeroesLost} tướng";
            for (var i = 0; i < _resultStars.Length; i++)
                _resultStars[i].color = i < battle.State.Stars ? new Color(1f, 0.82f, 0.2f) : new Color(0.3f, 0.3f, 0.35f);

            foreach (Transform child in _resultButtons)
                Destroy(child.gameObject);
            var stages = battle.Stages;
            var count = 1 + stages.Count;
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
            _resultPanel.SetActive(true);
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

            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                if (_toastTimer <= 0f)
                    _toast.enabled = false;
            }
        }
    }
}
