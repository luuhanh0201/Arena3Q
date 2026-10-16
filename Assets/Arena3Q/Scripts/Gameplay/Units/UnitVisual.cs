using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Placeholder look of a unit: body, health bar, optional selection ring and hit flash.</summary>
    public sealed class UnitVisual
    {
        private const float FlashSeconds = 0.12f;

        private readonly Transform _root;
        private readonly SpriteRenderer _body;
        private readonly SpriteRenderer _healthBack;
        private readonly SpriteRenderer _healthFill;
        private readonly SpriteRenderer _ring;
        private readonly Color _color;
        private readonly float _barWidth;
        private float _flash;

        public UnitVisual(Transform root, Sprite sprite, Color color, float diameter, bool withRing)
        {
            _root = root;
            _color = color;
            _barWidth = Mathf.Max(0.6f, diameter * 0.9f);

            _body = PlaceholderArt.CreateRenderer("Body", root, sprite, color, new Vector2(diameter, diameter));

            var barY = diameter * 0.5f + 0.18f;
            _healthBack = PlaceholderArt.CreateRenderer("HealthBack", root, PlaceholderArt.Square,
                new Color(0f, 0f, 0f, 0.6f), new Vector2(_barWidth, 0.1f), 1);
            _healthBack.transform.localPosition = new Vector3(0f, barY, 0f);
            _healthFill = PlaceholderArt.CreateRenderer("HealthFill", root, PlaceholderArt.Square,
                new Color(0.3f, 1f, 0.35f), new Vector2(_barWidth, 0.1f), 2);
            _healthFill.transform.localPosition = new Vector3(0f, barY, 0f);

            if (withRing)
            {
                _ring = PlaceholderArt.CreateRenderer("SelectionRing", root, PlaceholderArt.Ring,
                    new Color(1f, 0.95f, 0.35f, 0.95f), new Vector2(diameter * 1.6f, diameter * 1.6f), -1);
                _ring.enabled = false;
            }
        }

        public void SetHealth(float fraction, bool isEnemy)
        {
            fraction = Mathf.Clamp01(fraction);
            var t = _healthFill.transform;
            t.localScale = new Vector3(_barWidth * fraction, t.localScale.y, 1f);
            t.localPosition = new Vector3(-_barWidth * (1f - fraction) * 0.5f, t.localPosition.y, 0f);
            _healthFill.color = isEnemy ? new Color(1f, 0.35f, 0.3f) : new Color(0.3f, 1f, 0.35f);
        }

        public void SetSelected(bool selected)
        {
            if (_ring != null)
                _ring.enabled = selected;
        }

        public void Flash() => _flash = FlashSeconds;

        /// <summary>Ground-plane decorations (the selection ring) are squashed with the 2.5D tilt; the body stays upright.</summary>
        public void SetGroundScale(float yScale)
        {
            if (_ring == null)
                return;
            var size = _ring.transform.localScale.x;
            _ring.transform.localScale = new Vector3(size, size * yScale, 1f);
        }

        /// <summary>Hit flash and Y-sorting for the top-down 2.5D look (lower on screen = drawn in front).</summary>
        public void Update(float deltaTime)
        {
            if (_flash > 0f)
            {
                _flash -= deltaTime;
                _body.color = _flash > 0f ? Color.Lerp(_color, Color.white, 0.7f) : _color;
            }

            var order = Mathf.RoundToInt(-_root.position.y * 20f) * 4;
            _body.sortingOrder = order;
            _healthBack.sortingOrder = order + 1;
            _healthFill.sortingOrder = order + 2;
            if (_ring != null)
                _ring.sortingOrder = order - 1;
        }
    }
}
