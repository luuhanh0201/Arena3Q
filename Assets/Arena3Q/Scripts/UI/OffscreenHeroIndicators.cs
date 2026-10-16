using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.UI
{
    /// <summary>
    /// Edge-of-screen icons for heroes outside the view (spec §9). Tapping an icon selects the hero and brings the
    /// camera to it.
    /// </summary>
    public sealed class OffscreenHeroIndicators : MonoBehaviour
    {
        private const float EdgeMargin = 0.045f;

        private readonly Dictionary<UnitId, Indicator> _indicators = new Dictionary<UnitId, Indicator>();
        private readonly HashSet<UnitId> _seen = new HashSet<UnitId>();
        private readonly List<UnitId> _stale = new List<UnitId>();
        private BattleHud _hud;
        private RectTransform _root;

        private sealed class Indicator
        {
            public RectTransform Rect;
            public Image Icon;
            public Image Frame;
        }

        internal void Init(BattleHud hud, RectTransform root)
        {
            _hud = hud;
            _root = root;
        }

        /// <summary>Viewport positions of visible indicators, keyed by hero (used by tests).</summary>
        public bool TryGetIndicatorViewport(UnitId hero, out Vector2 viewport)
        {
            viewport = default;
            if (!_indicators.TryGetValue(hero, out var indicator) || !indicator.Rect.gameObject.activeSelf)
                return false;
            viewport = indicator.Rect.anchorMin;
            return true;
        }

        private void LateUpdate()
        {
            if (_hud == null || _hud.Battle == null || _hud.Camera == null)
                return;

            _seen.Clear();
            var selected = _hud.Battle.State?.SelectedHero ?? UnitId.None;
            foreach (var hero in _hud.Battle.LivingHeroes)
            {
                _seen.Add(hero.Id);
                var viewport = _hud.Camera.WorldToViewport(hero.Position);
                var indicator = GetOrCreate(hero);
                if (EdgeIndicatorMath.IsOnScreen(viewport) || _hud.Battle.IsOver)
                {
                    indicator.Rect.gameObject.SetActive(false);
                    continue;
                }
                var edge = EdgeIndicatorMath.ClampToEdge(viewport, EdgeMargin);
                indicator.Rect.gameObject.SetActive(true);
                indicator.Rect.anchorMin = edge;
                indicator.Rect.anchorMax = edge;
                indicator.Rect.anchoredPosition = Vector2.zero;
                indicator.Frame.color = hero.Id == selected ? new Color(1f, 0.92f, 0.3f) : new Color(0f, 0f, 0f, 0.65f);
            }

            _stale.Clear();
            foreach (var id in _indicators.Keys)
            {
                if (!_seen.Contains(id))
                    _stale.Add(id);
            }
            foreach (var id in _stale)
            {
                Destroy(_indicators[id].Rect.gameObject);
                _indicators.Remove(id);
            }
        }

        internal void ClearAll()
        {
            foreach (var indicator in _indicators.Values)
            {
                if (indicator.Rect != null)
                    Destroy(indicator.Rect.gameObject);
            }
            _indicators.Clear();
        }

        private Indicator GetOrCreate(HeroUnit hero)
        {
            if (_indicators.TryGetValue(hero.Id, out var existing))
                return existing;

            var rect = UiFactory.CreateRect($"EdgeIcon {hero.Id}", _root, Vector2.zero, Vector2.zero,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));
            var frame = UiFactory.AddImage(rect, new Color(0f, 0f, 0f, 0.65f), PlaceholderArt.Circle);
            var iconRect = UiFactory.CreateRect("Icon", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
            var icon = UiFactory.AddImage(iconRect, hero.Definition.PlaceholderColor, PlaceholderArt.Circle, false);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            var heroId = hero.Id;
            button.onClick.AddListener(() =>
            {
                if (_hud.Battle.TrySelectHero(heroId))
                    _hud.FocusSelectedHero();
            });

            var indicator = new Indicator { Rect = rect, Icon = icon, Frame = frame };
            _indicators[hero.Id] = indicator;
            return indicator;
        }
    }
}
