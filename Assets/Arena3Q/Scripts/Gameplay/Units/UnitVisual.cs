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
        private Color _color;
        private SpriteRenderer _outline;
        private SpriteRenderer _taunt;
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

        /// <summary>
        /// Shows a painted sprite (pivot on the ground, real size) instead of the tinted placeholder shape; the health
        /// bar moves above the picture.
        /// </summary>
        public void UseArt(Sprite sprite)
        {
            if (sprite == null)
                return;
            _color = Color.white;
            _body.sprite = sprite;
            _body.color = Color.white;
            _body.transform.localScale = Vector3.one;
            _body.transform.localPosition = Vector3.zero;
            var barY = sprite.bounds.max.y + 0.15f;
            _healthBack.transform.localPosition = new Vector3(0f, barY, 0f);
            _healthFill.transform.localPosition = new Vector3(_healthFill.transform.localPosition.x, barY, 0f);
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

        /// <summary>Rank outline behind the body: golden for elites, red for bosses (spec §8 ✅ "viền sáng").</summary>
        public void SetRankOutline(Color color)
        {
            var size = _body.transform.localScale.x * 1.3f;
            if (_outline == null)
                _outline = PlaceholderArt.CreateRenderer("RankOutline", _root, PlaceholderArt.Circle, color, new Vector2(size, size), -1);
            _outline.color = color;
        }

        /// <summary>Changes the body colour for good (boss phase 2).</summary>
        public void SetTint(Color color)
        {
            _color = color;
            _body.color = color;
        }

        private SpriteRenderer _stun;
        private SpriteRenderer _shield;
        private bool _weakened;

        /// <summary>Status markers: spinning stars when stunned, a ring while shielded, a dim tint when slowed or weakened.</summary>
        public void SetStatus(bool stunned, bool shielded, bool weakened, bool taunted = false)
        {
            if (taunted && _taunt == null)
            {
                _taunt = PlaceholderArt.CreateRenderer("Taunt", _root, PlaceholderArt.Circle, new Color(1f, 0.15f, 0.1f, 0.95f),
                    new Vector2(0.22f, 0.22f), 3);
                _taunt.transform.localPosition = new Vector3(0.3f, _body.transform.localScale.x * 0.5f + 0.35f, 0f);
            }
            if (_taunt != null)
                _taunt.enabled = taunted;
            var size = _body.transform.localScale.x;
            if (stunned && _stun == null)
            {
                _stun = PlaceholderArt.CreateRenderer("Stun", _root, PlaceholderArt.Star, new Color(1f, 0.9f, 0.2f),
                    new Vector2(0.35f, 0.35f), 3);
                _stun.transform.localPosition = new Vector3(0f, size * 0.5f + 0.4f, 0f);
            }
            if (_stun != null)
                _stun.enabled = stunned;
            if (shielded && _shield == null)
            {
                _shield = PlaceholderArt.CreateRenderer("Shield", _root, PlaceholderArt.Ring, new Color(1f, 0.85f, 0.3f, 0.8f),
                    new Vector2(size * 1.35f, size * 1.35f), 3);
            }
            if (_shield != null)
                _shield.enabled = shielded;
            _weakened = weakened;
            if (_flash <= 0f)
                _body.color = _weakened ? Color.Lerp(_color, new Color(0.4f, 0.4f, 0.6f), 0.4f) : _color;
        }

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
                var resting = _weakened ? Color.Lerp(_color, new Color(0.4f, 0.4f, 0.6f), 0.4f) : _color;
                _body.color = _flash > 0f ? Color.Lerp(_color, Color.white, 0.7f) : resting;
            }
            if (_stun != null && _stun.enabled)
                _stun.transform.Rotate(0f, 0f, 240f * deltaTime);

            var order = Mathf.RoundToInt(-_root.position.y * 20f) * 4;
            _body.sortingOrder = order;
            _healthBack.sortingOrder = order + 1;
            _healthFill.sortingOrder = order + 2;
            if (_ring != null)
                _ring.sortingOrder = order - 1;
            if (_outline != null)
                _outline.sortingOrder = order - 1;
        }
    }
}
