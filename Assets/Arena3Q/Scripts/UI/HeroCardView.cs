using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arena3Q.UI
{
    /// <summary>
    /// A hero card (spec §3, §4). UI only: tap an unused card = quick summon, drag it onto the summon zone = free summon,
    /// tap a deployed card = select its hero, drag a deployed card onto an enemy = attack (provisional),
    /// dead card = greyed out and inert.
    /// </summary>
    public sealed class HeroCardView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private enum DragMode
        {
            None,
            Summon,
            Attack
        }

        public static readonly Vector2 CardSize = new Vector2(160f, 210f);

        private BattleHud _hud;
        private Image _background;
        private Image _portrait;
        private Image _selectedFrame;
        private Text _status;
        private RectTransform _ghost;
        private Image _ghostImage;
        private Color _heroColor;
        private DragMode _dragMode;

        public int CardIndex { get; private set; }

        internal void Build(BattleHud hud, int cardIndex, HeroDefinition hero, bool isLord)
        {
            _hud = hud;
            CardIndex = cardIndex;
            _heroColor = hero.PlaceholderColor;
            var rect = (RectTransform)transform;

            _selectedFrame = UiFactory.AddImage(UiFactory.CreateRect("Selected", rect, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f)), new Color(1f, 0.92f, 0.3f), null, false);
            _background = UiFactory.AddImage(UiFactory.Stretch("Background", rect), new Color(0.12f, 0.14f, 0.2f, 0.95f));

            var portraitRect = UiFactory.CreateRect("Portrait", rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(100f, 100f));
            _portrait = UiFactory.AddImage(portraitRect, _heroColor, hero.Sprite != null ? hero.Sprite : PlaceholderArt.Circle, false);

            var nameRect = UiFactory.CreateRect("Name", rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(150f, 40f));
            UiFactory.AddText(nameRect, hero.DisplayName, 26, TextAnchor.MiddleCenter, Color.white);

            var statusRect = UiFactory.CreateRect("Status", rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(150f, 34f));
            _status = UiFactory.AddText(statusRect, isLord ? "Chủ tướng" : "", 22, TextAnchor.MiddleCenter,
                new Color(1f, 0.85f, 0.4f));
            _status.gameObject.name = isLord ? "Status (Lord)" : "Status";
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_hud.Battle.IsOver)
                return;
            switch (Card.State)
            {
                case CardState.Available:
                    _hud.Battle.TrySummon(SummonRequest.Quick(CardIndex));
                    break;
                case CardState.Deployed:
                    if (_hud.Battle.TrySelectCard(CardIndex))
                        _hud.FocusSelectedHero();
                    break;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragMode = DragMode.None;
            if (_hud.Battle.IsOver)
                return;
            switch (Card.State)
            {
                case CardState.Available:
                    _dragMode = DragMode.Summon;
                    _hud.Battle.ShowSummonZone(true);
                    break;
                case CardState.Deployed:
                    _dragMode = DragMode.Attack;
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

        public void OnEndDrag(PointerEventData eventData)
        {
            var mode = _dragMode;
            _dragMode = DragMode.None;
            HideGhost();
            _hud.Battle.ShowSummonZone(false);
            if (mode == DragMode.None || _hud.Battle.IsOver)
                return;

            // Released over UI (e.g. back on the card bar) = cancel, the card is not used.
            if (_hud.IsOverUi(eventData.position))
                return;

            var world = _hud.Camera.ScreenToWorld(eventData.position);
            if (mode == DragMode.Summon)
            {
                _hud.Battle.TrySummon(SummonRequest.Free(CardIndex, world));
                return;
            }

            var target = _hud.Battle.FindEnemyAt(world);
            if (target != null && Card.State == CardState.Deployed)
                _hud.Battle.AttackWithHero(Card.Hero, target);
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
            _ghostImage.color = _dragMode == DragMode.Attack
                ? new Color(1f, 0.3f, 0.25f, 0.8f)
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
