using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arena3Q.UI
{
    /// <summary>
    /// A hero card (spec §3 "Kỹ năng trên thẻ", §4). UI only.
    /// Unused card: tap = quick summon, drag onto the summon zone = free summon.
    /// Deployed card: tap = ultimate (auto-aim), drag onto the battlefield = ultimate aimed there (released over UI =
    /// cancel), hold 1.5 s = select the hero. It shows the skill 2 / 3 icons with their cooldowns and the rage bar.
    /// Dead card: greyed out and inert.
    /// </summary>
    public sealed class HeroCardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private enum DragMode
        {
            None,
            Summon,
            Ultimate
        }

        private sealed class SkillIcon
        {
            public SkillSlot Slot;
            public GameObject Root;
            public Image Cooldown;
            public Text Label;
        }

        public static readonly Vector2 CardSize = new Vector2(160f, 210f);

        /// <summary>Hold a deployed card this long to select its hero (spec §3 ✅).</summary>
        public const float HoldToSelectSeconds = 1.5f;

        private BattleHud _hud;
        private LineupEntry _card;
        private Image _background;
        private Image _portrait;
        private Image _selectedFrame;
        private Text _status;
        private RectTransform _ghost;
        private Image _ghostImage;
        private Color _heroColor;
        private DragMode _dragMode;
        private SkillIcon[] _skillIcons;
        private RectTransform _rageBar;
        private Image _rageFill;
        private float _pressTime = -1f;
        private bool _holdUsed;

        public int CardIndex { get; private set; }
        public float RageShown => _rageFill != null && _rageBar.gameObject.activeSelf ? _rageFill.fillAmount : 0f;

        internal void Build(BattleHud hud, int cardIndex, LineupEntry card, bool isLord)
        {
            var hero = card.Hero;
            _hud = hud;
            _card = card;
            CardIndex = cardIndex;
            _heroColor = hero.PlaceholderColor;
            var rect = (RectTransform)transform;

            _selectedFrame = UiFactory.AddImage(UiFactory.CreateRect("Selected", rect, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f)), new Color(1f, 0.92f, 0.3f), null, false);
            _background = UiFactory.AddImage(UiFactory.Stretch("Background", rect), new Color(0.12f, 0.14f, 0.2f, 0.95f));

            var portraitRect = UiFactory.CreateRect("Portrait", rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-16f, -24f), new Vector2(86f, 86f));
            _portrait = UiFactory.AddImage(portraitRect, _heroColor, hero.Sprite != null ? hero.Sprite : PlaceholderArt.Circle, false);

            var nameRect = UiFactory.CreateRect("Name", rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(150f, 40f));
            UiFactory.AddText(nameRect, hero.DisplayName, 26, TextAnchor.MiddleCenter, Color.white);

            // Level, and stars for heroes that have them (lords break through by level instead).
            var levelRect = UiFactory.CreateRect("Level", rect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(8f, -4f), new Vector2(144f, 30f));
            var levelText = isLord ? $"Lv{card.Level}" : $"Lv{card.Level} {new string('★', card.Stars)}";
            UiFactory.AddText(levelRect, levelText, 20, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.55f));

            var statusRect = UiFactory.CreateRect("Status", rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(150f, 34f));
            _status = UiFactory.AddText(statusRect, isLord ? "Chủ tướng" : "", 22, TextAnchor.MiddleCenter,
                new Color(1f, 0.85f, 0.4f));
            _status.gameObject.name = isLord ? "Status (Lord)" : "Status";

            BuildSkills(rect, hero);
        }

        // Skill 2 and 3 icons on the right of the portrait, the rage bar under it.
        private void BuildSkills(RectTransform rect, HeroDefinition hero)
        {
            _skillIcons = new SkillIcon[2];
            var slots = new[] { SkillSlot.Skill2, SkillSlot.Skill3 };
            for (var i = 0; i < slots.Length; i++)
            {
                var skill = hero.GetSkill(slots[i]);
                var iconRect = UiFactory.CreateRect($"Skill {slots[i]}", rect, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(1f, 1f), new Vector2(-5f, -28f - i * 40f), new Vector2(34f, 34f));
                UiFactory.AddImage(iconRect, skill != null && skill.Icon == null ? skill.IconColor : Color.white,
                    skill != null ? skill.Icon : null, false);
                var cooldown = UiFactory.AddImage(UiFactory.Stretch("Cooldown", iconRect), new Color(0f, 0f, 0f, 0.7f), PlaceholderArt.Square, false);
                cooldown.type = Image.Type.Filled;
                cooldown.fillMethod = Image.FillMethod.Radial360;
                cooldown.fillOrigin = (int)Image.Origin360.Top;
                cooldown.fillClockwise = false;
                var label = UiFactory.AddText(UiFactory.Stretch("Label", iconRect), "", 18, TextAnchor.MiddleCenter, Color.white);
                label.gameObject.AddComponent<Outline>().effectColor = Color.black;
                _skillIcons[i] = new SkillIcon { Slot = slots[i], Root = iconRect.gameObject, Cooldown = cooldown, Label = label };
                iconRect.gameObject.SetActive(skill != null);
            }

            _rageBar = UiFactory.CreateRect("Rage", rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -114f), new Vector2(136f, 9f));
            UiFactory.AddImage(_rageBar, new Color(0f, 0f, 0f, 0.7f), null, false);
            _rageFill = UiFactory.AddImage(UiFactory.Stretch("Fill", _rageBar), new Color(1f, 0.5f, 0.15f), PlaceholderArt.Square, false);
            _rageFill.type = Image.Type.Filled;
            _rageFill.fillMethod = Image.FillMethod.Horizontal;
            _rageFill.fillAmount = 0f;
            _rageBar.gameObject.SetActive(false);
        }

        internal void Refresh(CardState state, bool selected, bool isLord)
        {
            var dead = state == CardState.Dead;
            _background.color = dead ? new Color(0.2f, 0.2f, 0.2f, 0.9f) : new Color(0.12f, 0.14f, 0.2f, 0.95f);
            _portrait.color = dead ? new Color(0.35f, 0.35f, 0.35f) : _heroColor;
            _selectedFrame.enabled = selected && state == CardState.Deployed;
            switch (state)
            {
                case CardState.Deployed: _status.text = "Trên sân"; _status.color = new Color(0.5f, 1f, 0.6f); break;
                case CardState.Dead: _status.text = "Đã chết"; _status.color = new Color(0.7f, 0.7f, 0.7f); break;
                default: _status.text = isLord ? "Chủ tướng" : "Triệu hồi"; _status.color = new Color(1f, 0.85f, 0.4f); break;
            }
        }

        private HeroCard Card => _hud.Battle.State.Cards[CardIndex];
        private HeroUnit Hero => _hud.Battle.GetHero(Card.Hero);

        private void Update()
        {
            if (_hud == null || _hud.Battle.State == null || CardIndex >= _hud.Battle.State.Cards.Count)
                return;
            UpdateSkills();
            if (_pressTime >= 0f && !_holdUsed && _dragMode == DragMode.None &&
                Time.unscaledTime - _pressTime >= HoldToSelectSeconds)
                SelectByHold();
        }

        private void UpdateSkills()
        {
            var hero = Card.State == CardState.Deployed ? Hero : null;
            var rules = _hud.Battle.Rules;
            foreach (var icon in _skillIcons)
            {
                if (_card.SkillLevel(icon.Slot, rules) <= 0)
                {
                    // Locked: the lord's unlock level or the stars needed (spec §3 🟡), shown before summoning too.
                    icon.Cooldown.fillAmount = 1f;
                    icon.Label.text = LockLabel(icon.Slot);
                    continue;
                }
                var slot = hero?.Skills.Get(icon.Slot);
                var cooldown = slot?.Definition != null ? slot.Definition.Cooldown : 0f;
                var left = slot?.CooldownLeft ?? 0f;
                icon.Cooldown.fillAmount = cooldown > 0f ? left / cooldown : 0f;
                icon.Label.text = left > 0f ? Mathf.CeilToInt(left).ToString() : "";
            }

            // No ultimate learned yet: no rage bar (spec §3 🟡).
            var showRage = hero != null && hero.Skills.HasUltimate;
            _rageBar.gameObject.SetActive(showRage);
            if (showRage)
            {
                _rageFill.fillAmount = hero.Skills.Rage.Fraction;
                _rageFill.color = hero.Skills.Rage.IsFull
                    ? Color.Lerp(new Color(1f, 0.85f, 0.2f), Color.white, Mathf.PingPong(Time.unscaledTime * 2f, 0.6f))
                    : new Color(1f, 0.5f, 0.15f);
            }
        }

        private string LockLabel(SkillSlot slot)
        {
            var rules = _hud.Battle.Rules;
            var kind = _card.Hero.Kind;
            if (kind == HeroKind.Lord)
                return $"Lv{rules.LordUnlockLevel(slot)}";
            for (var stars = 1; stars <= rules.MaxStars; stars++)
            {
                if (rules.IsSkillUnlocked(kind, slot, _card.Level, stars))
                    return $"{stars}★";
            }
            return "";
        }

        // ---- Input ----

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressTime = Time.unscaledTime;
            _holdUsed = false;
        }

        public void OnPointerUp(PointerEventData eventData) => _pressTime = -1f;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_holdUsed || eventData.dragging)
                return;
            Tap();
        }

        /// <summary>Tap: summon an unused card; use the ultimate of a deployed hero.</summary>
        public void Tap()
        {
            if (_hud.Battle.IsOver)
                return;
            switch (Card.State)
            {
                case CardState.Available:
                    _hud.Battle.TrySummon(SummonRequest.Quick(CardIndex));
                    break;
                case CardState.Deployed:
                    _hud.Battle.TryUseUltimate(CardIndex);
                    break;
            }
        }

        /// <summary>Held 1.5 s: select the deployed hero (and look at it).</summary>
        public void SelectByHold()
        {
            _holdUsed = true;
            _pressTime = -1f;
            if (Card.State == CardState.Deployed && _hud.Battle.TrySelectCard(CardIndex))
                _hud.FocusSelectedHero();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragMode = DragMode.None;
            _pressTime = -1f;
            if (_hud.Battle.IsOver || _holdUsed)
                return;
            switch (Card.State)
            {
                case CardState.Available:
                    _dragMode = DragMode.Summon;
                    _hud.Battle.ShowSummonZone(true);
                    break;
                case CardState.Deployed:
                    // 🟡 Aiming only starts with a full rage bar; otherwise the usual "not ready" message.
                    var hero = Hero;
                    if (hero == null || !hero.Skills.HasUltimate || !hero.Skills.Rage.IsFull)
                    {
                        _hud.Battle.TryUseUltimate(CardIndex);
                        return;
                    }
                    _dragMode = DragMode.Ultimate;
                    break;
                default:
                    return;
            }
            ShowGhost(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragMode != DragMode.None)
                ShowGhost(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData) => ReleaseDrag(eventData.position);

        /// <summary>End of a drag at a screen position: summon there, or aim the ultimate there; over UI = cancel.</summary>
        public void ReleaseDrag(Vector2 screenPosition)
        {
            var mode = _dragMode;
            _dragMode = DragMode.None;
            HideGhost();
            _hud.Battle.ShowSummonZone(false);
            if (mode == DragMode.None || _hud.Battle.IsOver)
                return;

            // Released over UI (e.g. back on the card bar) = cancel, the card is not used.
            if (_hud.IsOverUi(screenPosition))
                return;

            var world = _hud.Camera.ScreenToWorld(screenPosition);
            if (mode == DragMode.Summon)
                _hud.Battle.TrySummon(SummonRequest.Free(CardIndex, world));
            else
                _hud.Battle.TryUseUltimate(CardIndex, world);
        }

        /// <summary>Starts aiming the ultimate from code (tests): the same check as a real drag.</summary>
        public bool BeginUltimateDrag()
        {
            var hero = Hero;
            if (Card.State != CardState.Deployed || hero == null || !hero.Skills.Rage.IsFull)
                return false;
            _dragMode = DragMode.Ultimate;
            return true;
        }

        private void ShowGhost(Vector2 screenPosition)
        {
            if (_ghost == null)
            {
                _ghost = UiFactory.CreateRect("DragGhost", _hud.OverlayRoot, Vector2.zero, Vector2.zero,
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
                _ghostImage = UiFactory.AddImage(_ghost, Color.white, PlaceholderArt.Circle, false);
            }
            _ghost.gameObject.SetActive(true);
            _ghostImage.color = _dragMode == DragMode.Ultimate
                ? new Color(1f, 0.75f, 0.2f, 0.8f)
                : new Color(_heroColor.r, _heroColor.g, _heroColor.b, 0.75f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_hud.OverlayRoot, screenPosition, null, out var local);
            _ghost.localPosition = local;
        }

        private void HideGhost()
        {
            if (_ghost != null)
                _ghost.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_ghost != null)
                Destroy(_ghost.gameObject);
        }
    }
}
