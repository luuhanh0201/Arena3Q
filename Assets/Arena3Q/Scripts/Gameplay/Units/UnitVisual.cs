using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Placeholder look of a unit: body, health bar, optional selection ring and hit flash. In 3D a unit without a
    /// picture gets a lit capsule (<see cref="UnitBody3D"/>) with a soft shadow, and the markers float above its top.
    /// </summary>
    public sealed class UnitVisual
    {
        private const float FlashSeconds = 0.12f;
        // Ground pieces of 3D bodies: above the painted ground and the altar, below everything standing.
        private const int ShadowOrder = -30990;

        private readonly Transform _root;
        private readonly SpriteRenderer _body;
        internal SpriteRenderer BodyRenderer => _body;
        private readonly SpriteRenderer _healthBack;
        private readonly SpriteRenderer _healthFill;
        private readonly SpriteRenderer _ring;
        private Color _color;
        private SpriteRenderer _outline;
        private SpriteRenderer _taunt;
        private readonly float _barWidth;
        private readonly float _diameter;
        private float _flash;
        private UnitBody3D _model;
        private SpriteRenderer _shadow;
        private Quaternion _groundFromRoot = Quaternion.identity;
        // Top of the picture above the unit's ground point, in screen-plane units (where the markers start).
        private float _artTop;

        internal UnitBody3D Model => _model;

        public UnitVisual(Transform root, Sprite sprite, Color color, float diameter, bool withRing,
            BattleViewConfig model3D = null)
        {
            _root = root;
            _color = color;
            _diameter = diameter;
            _barWidth = Mathf.Max(0.6f, diameter * 0.9f);

            _body = PlaceholderArt.CreateRenderer("Body", root, sprite, color, new Vector2(diameter, diameter));
            _artTop = diameter * 0.5f;

            _healthBack = PlaceholderArt.CreateRenderer("HealthBack", root, PlaceholderArt.Square,
                new Color(0f, 0f, 0f, 0.6f), new Vector2(_barWidth, 0.1f), 1);
            _healthFill = PlaceholderArt.CreateRenderer("HealthFill", root, PlaceholderArt.Square,
                new Color(0.3f, 1f, 0.35f), new Vector2(_barWidth, 0.1f), 2);

            if (withRing)
            {
                _ring = PlaceholderArt.CreateRenderer("SelectionRing", root, PlaceholderArt.Ring,
                    new Color(1f, 0.95f, 0.35f, 0.95f), new Vector2(diameter * 1.6f, diameter * 1.6f), -1);
                _ring.enabled = false;
            }

            if (model3D != null)
            {
                _model = new UnitBody3D(root, model3D.UnitMaterial, color, diameter, diameter * model3D.PlaceholderHeight);
                _body.enabled = false;
                _shadow = PlaceholderArt.CreateRenderer("Shadow", root, PlaceholderArt.Blob,
                    new Color(0f, 0f, 0f, model3D.BlobShadowAlpha), new Vector2(diameter * 1.5f, diameter * 1.5f), ShadowOrder);
            }
            LayoutMarkers();
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
            _body.enabled = true;
            if (_model != null)
            {
                // The picture already carries its own shadow.
                _model.Destroy();
                _model = null;
                Object.Destroy(_shadow.gameObject);
                _shadow = null;
            }
            _artTop = sprite.bounds.max.y - 0.03f;
            LayoutMarkers();
        }

        // 3D map prop standing for the picture (castles): turned by its yaw on the ground, tinted by the hit flash.
        private Transform _prop;
        private MeshRenderer _propRenderer;
        private float _propYaw, _propHeight;
        private MaterialPropertyBlock _propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>Shows a 3D map prop instead of the picture (3D presentation); the picture stays as the reference.</summary>
        internal void UsePropModel(MapProps3DLibrary.Prop prop, float yaw, float shadowAlpha)
        {
            _prop = MapProp3D.Create("Body3D Prop", _root, prop);
            _propRenderer = _prop.GetComponentInChildren<MeshRenderer>();
            _propYaw = yaw;
            _propHeight = MapProp3D.Height(prop);
            _body.enabled = false;
            if (_shadow == null)
            {
                _shadow = MapProp3D.CreateShadow(_root, prop, shadowAlpha);
                _shadow.transform.localRotation = _groundFromRoot;
            }
            SetGroundRotation(_groundFromRoot);
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

        /// <summary>Turns a 3D body towards a direction on the logic ground (pictures keep their own facing).</summary>
        public void Face(Vector2 direction) => _model?.Face(direction);

        /// <summary>Rank outline behind the body: golden for elites, red for bosses (spec §8 ✅ "viền sáng").</summary>
        public void SetRankOutline(Color color)
        {
            if (_outline == null)
            {
                // A 3D body gets a glowing disc on the ground around its feet instead of a halo behind it.
                var size = _model != null ? _diameter * 1.45f : _body.transform.localScale.x * 1.3f;
                _outline = PlaceholderArt.CreateRenderer("RankOutline", _root, PlaceholderArt.Circle, color, new Vector2(size, size), -1);
                if (_model != null)
                {
                    _outline.sortingOrder = ShadowOrder + 1;
                    _outline.transform.localRotation = _groundFromRoot;
                }
            }
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
                LayoutMarkers();
            }
            if (_taunt != null)
                _taunt.enabled = taunted;
            if (stunned && _stun == null)
            {
                _stun = PlaceholderArt.CreateRenderer("Stun", _root, PlaceholderArt.Star, new Color(1f, 0.9f, 0.2f),
                    new Vector2(0.35f, 0.35f), 3);
                LayoutMarkers();
            }
            if (_stun != null)
                _stun.enabled = stunned;
            if (shielded && _shield == null)
            {
                var size = Mathf.Max(_body.transform.localScale.x, _model != null ? _diameter : 0f);
                _shield = PlaceholderArt.CreateRenderer("Shield", _root, PlaceholderArt.Ring, new Color(1f, 0.85f, 0.3f, 0.8f),
                    new Vector2(size * 1.35f, size * 1.35f), 3);
                LayoutMarkers();
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

        /// <summary>
        /// In 3D the unit root faces the camera; ground-plane decorations get the inverse rotation so they lie flat on
        /// the ground again (identity in the flat 2.5D view).
        /// </summary>
        public void SetGroundRotation(Quaternion local)
        {
            _groundFromRoot = local;
            if (_ring != null)
                _ring.transform.localRotation = local;
            if (_shadow != null)
                _shadow.transform.localRotation = local;
            if (_outline != null && _model != null)
                _outline.transform.localRotation = local;
            _model?.SetGroundFromRoot(local);
            if (_prop != null)
                _prop.localRotation = local * MapProp3D.Rotation(_propYaw);
            LayoutMarkers();
        }

        // Health bar, stun star, taunt mark and shield ring sit above the top of the body as seen on screen.
        private void LayoutMarkers()
        {
            var top = _artTop;
            var screenUp = Quaternion.Inverse(_groundFromRoot) * Vector3.up;
            var sinTilt = Mathf.Clamp01(-screenUp.z);
            if (_model != null)
            {
                // A body of height h shows h · sin(tilt) tall; its round top adds a little.
                top = _model.Height * sinTilt + _diameter * 0.4f * Mathf.Sqrt(1f - sinTilt * sinTilt);
            }
            else if (_prop != null)
                top = Mathf.Max(_propHeight * sinTilt + _diameter * 0.5f * Mathf.Sqrt(1f - sinTilt * sinTilt), 0.5f);
            var barY = top + 0.18f;
            _healthBack.transform.localPosition = new Vector3(0f, barY, 0f);
            _healthFill.transform.localPosition = new Vector3(_healthFill.transform.localPosition.x, barY, 0f);
            if (_stun != null)
                _stun.transform.localPosition = new Vector3(0f, top + 0.4f, 0f);
            if (_taunt != null)
                _taunt.transform.localPosition = new Vector3(0.3f, top + 0.35f, 0f);
            if (_shield != null)
                _shield.transform.localPosition = new Vector3(0f, _model != null ? top * 0.5f : 0f, 0f);
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
            _model?.SetColor(_body.color);
            if (_propRenderer != null)
            {
                _propBlock ??= new MaterialPropertyBlock();
                _propBlock.SetColor(BaseColorId, _body.color);
                _propRenderer.SetPropertyBlock(_propBlock);
            }
            if (_stun != null && _stun.enabled)
                _stun.transform.Rotate(0f, 0f, 240f * deltaTime);

            var order = Mathf.RoundToInt(-_root.position.y * 20f) * 4;
            _body.sortingOrder = order;
            _healthBack.sortingOrder = order + 1;
            _healthFill.sortingOrder = order + 2;
            if (_ring != null)
                _ring.sortingOrder = order - 1;
            if (_outline != null && _model == null)
                _outline.sortingOrder = order - 1;
        }
    }
}
