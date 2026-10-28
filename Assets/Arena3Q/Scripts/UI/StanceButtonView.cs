using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arena3Q.UI
{
    /// <summary>
    /// The army stance button (spec §5 "Cơ chế mới"): one button in the bottom-right corner shows the active stance
    /// (Giữ at the start). A tap, or a swipe up from it, opens the other two stances stacked above; tapping one (or
    /// sliding onto it and letting go) switches every hero and closes the list. Tapping the button again, tapping
    /// elsewhere or waiting <see cref="StanceButtonConfig.AutoCollapseSeconds"/> closes it without a change. The list
    /// slides and fades (never pops); the battle keeps running meanwhile. A short "Toàn quân: ..." line under the timer
    /// confirms a change. Look and timing: <see cref="StanceButtonConfig"/>.
    /// </summary>
    public sealed class StanceButtonView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private static readonly ArmyStance[] Order = { ArmyStance.Hold, ArmyStance.Attack, ArmyStance.Defend };

        private sealed class Option
        {
            public RectTransform Rect;
            public Image Background;
            public Image Icon;
            public Text Label;
            public ArmyStance Stance;
        }

        private BattleController _battle;
        private StanceButtonConfig _config;
        private UiIconSet _icons;
        private RectTransform _root;
        private RectTransform _main;
        private Image _mainIcon;
        private Text _mainLabel;
        private RectTransform _list;
        private CanvasGroup _listGroup;
        private Image _blocker;
        private readonly List<Option> _options = new List<Option>();
        private Text _message;
        private CanvasGroup _messageGroup;
        private float _messageTimer;
        private float _open;          // 0 closed .. 1 open (animated)
        private bool _wantOpen;
        private float _idleOpenSeconds;
        private Vector2 _pressPosition;
        private bool _dragging;
        private Option _hovered;

        /// <summary>Whether the other two stances are shown (or opening).</summary>
        public bool IsOpen => _wantOpen;

        /// <summary>The stances offered while open, bottom (next to the button) to top.</summary>
        public IReadOnlyList<ArmyStance> OfferedStances
        {
            get
            {
                var list = new List<ArmyStance>(_options.Count);
                foreach (var option in _options)
                    list.Add(option.Stance);
                return list;
            }
        }

        public string MainLabel => _mainLabel != null ? _mainLabel.text : "";
        public string MessageText => _message != null && _messageGroup.alpha > 0f ? _message.text : "";

        /// <summary>
        /// Builds the button in the HUD's safe area <paramref name="root"/>; <paramref name="messageRoot"/> holds the
        /// confirmation line. The component lives on the main button, which receives the taps and swipes.
        /// </summary>
        public static StanceButtonView Create(RectTransform root, RectTransform messageRoot)
        {
            var config = StanceButtonConfig.Load();
            var corner = new Vector2(1f, 0f);
            var main = UiFactory.CreateRect("StanceButton", root, corner, corner, corner,
                new Vector2(-config.CornerMargin.x, config.CornerMargin.y), config.ButtonSize);
            var view = main.gameObject.AddComponent<StanceButtonView>();
            view.Build(root, messageRoot, main);
            return view;
        }

        private void Build(RectTransform root, RectTransform messageRoot, RectTransform main)
        {
            _config = StanceButtonConfig.Load();
            _icons = UiIconSet.Load();
            _root = root;
            var size = _config.ButtonSize;
            var corner = new Vector2(1f, 0f);

            // Full-screen catcher behind the list while it is open: a tap elsewhere closes the list (and gives no order).
            var blocker = UiFactory.Stretch("StanceBlocker", root);
            _blocker = UiFactory.AddImage(blocker, new Color(0f, 0f, 0f, 0f));
            var blockerButton = blocker.gameObject.AddComponent<Button>();
            blockerButton.transition = Selectable.Transition.None;
            blockerButton.onClick.AddListener(Close);
            blocker.gameObject.SetActive(false);

            _list = UiFactory.CreateRect("StanceOptions", root, corner, corner, corner,
                new Vector2(-_config.CornerMargin.x, _config.CornerMargin.y + size.y + _config.Spacing),
                new Vector2(size.x, size.y * 2f + _config.Spacing));
            _listGroup = _list.gameObject.AddComponent<CanvasGroup>();
            for (var i = 0; i < 2; i++)
                _options.Add(BuildOption(i, size));

            // Draw order: catcher, then the list, then the main button on top.
            _main = main;
            _main.SetAsLastSibling();
            UiFactory.AddImage(_main, _config.MainColour);
            var shine = _main.gameObject.AddComponent<Outline>();
            shine.effectColor = new Color(1f, 0.92f, 0.65f, 0.8f);
            shine.effectDistance = new Vector2(1.5f, -1.5f);
            _mainIcon = UiFactory.AddIcon("Icon", _main, null, new Vector2(0.5f, 1f), new Vector2(0f, -14f),
                _config.IconSize, _config.MainTextColour);
            _mainLabel = AddLabel("Label", _main, _config.MainTextColour);
            UiFactory.AddIcon("ExpandHint", _main, _icons != null ? _icons.ExpandUp : null, new Vector2(1f, 1f),
                new Vector2(-6f, -6f), _config.ExpandHintSize, new Color(_config.MainTextColour.r, _config.MainTextColour.g,
                    _config.MainTextColour.b, 0.6f));

            _message = UiFactory.CreateLabel("StanceMessage", messageRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -96f), new Vector2(600f, 40f), "", 26, TextAnchor.MiddleCenter, _config.MessageColour);
            _messageGroup = _message.gameObject.AddComponent<CanvasGroup>();
            _messageGroup.alpha = 0f;
            _messageGroup.blocksRaycasts = false;

            SetOpenVisual(0f);
        }

        private Option BuildOption(int slot, Vector2 size)
        {
            // Slot 0 sits right above the main button, slot 1 above that.
            var rect = UiFactory.CreateRect($"Option {slot}", _list, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, slot * (size.y + _config.Spacing)), size);
            var background = UiFactory.AddImage(rect, _config.OptionColour);
            var border = rect.gameObject.AddComponent<Outline>();
            border.effectColor = _config.OptionBorderColour;
            border.effectDistance = new Vector2(1.5f, -1.5f);
            var option = new Option
            {
                Rect = rect,
                Background = background,
                Icon = UiFactory.AddIcon("Icon", rect, null, new Vector2(0.5f, 1f), new Vector2(0f, -14f), _config.IconSize,
                    _config.OptionTextColour),
                Label = AddLabel("Label", rect, _config.OptionTextColour)
            };
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => Choose(option));
            return option;
        }

        private Text AddLabel(string name, RectTransform parent, Color colour)
        {
            var rect = UiFactory.CreateRect(name, parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(0f, _config.LabelSize + 8f));
            var text = UiFactory.AddText(rect, "", _config.LabelSize, TextAnchor.MiddleCenter, colour);
            // Regular weight: the HUD font has no bold face, a synthetic bold blurs at this size.
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        public void Bind(BattleController battle)
        {
            if (_battle != null)
                _battle.StanceChanged -= OnStanceChanged;
            _battle = battle;
            if (_battle != null)
                _battle.StanceChanged += OnStanceChanged;
        }

        private void OnDestroy()
        {
            if (_battle != null)
                _battle.StanceChanged -= OnStanceChanged;
        }

        /// <summary>A new battle: back to the stance it starts in, list closed, no message.</summary>
        public void ResetForBattle()
        {
            _wantOpen = false;
            _open = 0f;
            _messageTimer = 0f;
            if (_messageGroup != null)
                _messageGroup.alpha = 0f;
            Sync();
            SetOpenVisual(0f);
        }

        /// <summary>Opens or closes the list (the main button's tap).</summary>
        public void Toggle()
        {
            if (_wantOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (_battle == null || _battle.IsOver)
                return;
            Sync();
            _wantOpen = true;
            _idleOpenSeconds = 0f;
            _blocker.gameObject.SetActive(true);
        }

        public void Close()
        {
            _wantOpen = false;
            _hovered = null;
            _blocker.gameObject.SetActive(false);
            HighlightHovered();
        }

        /// <summary>Picks one of the offered stances (tests and the option buttons).</summary>
        public void Choose(ArmyStance stance)
        {
            foreach (var option in _options)
            {
                if (option.Stance == stance)
                {
                    Choose(option);
                    return;
                }
            }
        }

        private void Choose(Option option)
        {
            if (option == null || !_wantOpen)
                return;
            _battle?.SetStance(option.Stance);
            Close();
        }

        private void OnStanceChanged(ArmyStance stance)
        {
            Sync();
            _message.text = _config.ChangedMessage(stance);
            _messageTimer = _config.MessageSeconds + _config.MessageFadeSeconds;
            _messageGroup.alpha = 1f;
        }

        // Main button: the active stance; options: the other two in the fixed cycle Giữ → Tấn công → Phòng thủ,
        // starting right above the button (e.g. Giữ shows Tấn công, then Phòng thủ on top).
        private void Sync()
        {
            var current = _battle != null ? _battle.Stance : ArmyStance.Hold;
            _mainLabel.text = _config.NameOf(current);
            SetIcon(_mainIcon, current);
            var start = System.Array.IndexOf(Order, current);
            for (var i = 0; i < _options.Count; i++)
            {
                var stance = Order[(start + 1 + i) % Order.Length];
                _options[i].Stance = stance;
                _options[i].Label.text = _config.NameOf(stance);
                SetIcon(_options[i].Icon, stance);
            }
        }

        private void SetIcon(Image image, ArmyStance stance)
        {
            if (_icons == null)
                return;
            image.sprite = stance == ArmyStance.Attack ? _icons.Attack : stance == ArmyStance.Defend ? _icons.Defend : _icons.Hold;
            image.enabled = image.sprite != null;
        }

        // ---- Main button gestures: tap toggles, a swipe up opens and picks the option the finger is released on ----

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressPosition = eventData.position;
            _dragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var scale = _root.lossyScale.y > 0f ? _root.lossyScale.y : 1f;
            if (!_dragging && (eventData.position.y - _pressPosition.y) / scale >= _config.SwipeOpenDistance)
            {
                _dragging = true;
                if (!_wantOpen)
                    Open();
            }
            if (_dragging)
            {
                _hovered = OptionAt(eventData.position, eventData.pressEventCamera);
                HighlightHovered();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_dragging)
            {
                var option = OptionAt(eventData.position, eventData.pressEventCamera);
                _dragging = false;
                if (option != null)
                    Choose(option);
                _hovered = null;
                HighlightHovered();
                return;
            }
            if (eventData.dragging)
                return;
            Toggle();
        }

        private Option OptionAt(Vector2 screen, Camera eventCamera)
        {
            foreach (var option in _options)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(option.Rect, screen, eventCamera))
                    return option;
            }
            return null;
        }

        private void HighlightHovered()
        {
            foreach (var option in _options)
                option.Background.color = option == _hovered ? _config.OptionHighlightColour : _config.OptionColour;
        }

        private void Update()
        {
            if (_config == null)
                return;
            var dt = Time.unscaledDeltaTime;
            if (_wantOpen && _battle != null && _battle.IsOver)
                Close();
            if (_wantOpen && !_dragging)
            {
                _idleOpenSeconds += dt;
                if (_idleOpenSeconds >= _config.AutoCollapseSeconds)
                    Close();
            }
            var target = _wantOpen ? 1f : 0f;
            if (!Mathf.Approximately(_open, target))
            {
                _open = Mathf.MoveTowards(_open, target, dt / _config.ExpandSeconds);
                SetOpenVisual(_open);
            }

            if (_messageTimer > 0f)
            {
                _messageTimer -= dt;
                _messageGroup.alpha = Mathf.Clamp01(_messageTimer / _config.MessageFadeSeconds);
            }
        }

        // Slide up from behind the button while fading in; closed = invisible and not clickable.
        private void SetOpenVisual(float open)
        {
            if (_listGroup == null)
                return;
            var eased = open * open * (3f - 2f * open);
            _listGroup.alpha = eased;
            _listGroup.interactable = open >= 1f;
            _listGroup.blocksRaycasts = open > 0f && _wantOpen;
            var size = _config.ButtonSize;
            _list.anchoredPosition = new Vector2(-_config.CornerMargin.x,
                _config.CornerMargin.y + size.y + _config.Spacing - (1f - eased) * size.y * 0.5f);
        }
    }
}
