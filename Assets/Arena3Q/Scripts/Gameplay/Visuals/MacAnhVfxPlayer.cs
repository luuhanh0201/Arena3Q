using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Plays a <see cref="MacAnhVfxRecipe"/> (layers of pre-rendered frames) inside a pooled <see cref="MacAnhFx"/>.
    /// Visual only: timing comes from the effect's clock (battle time), never from gameplay rules.
    /// </summary>
    public sealed class MacAnhVfxPlayer : MonoBehaviour, IMacAnhSpatialEffect
    {
        [SerializeField] private MacAnhVfxRecipe recipe;
        [SerializeField] private Material material;

        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int UnderlayId = Shader.PropertyToID("_Underlay");
        private static readonly int UnderlayColourId = Shader.PropertyToID("_UnderlayColor");

        /// <summary>Above every unit and ground decoration; the effect's layers go above it while the dim is on.</summary>
        private const int DimOrder = 20000;
        private static Sprite _dimSprite;

        private Transform[] _roots, _spins;
        private SpriteRenderer[] _renderers;
        private SpriteRenderer _dim;
        private MaterialPropertyBlock _block;
        private MacAnhFxQuality _quality;
        private MacAnhVfxPhase _phase;
        private float _seconds, _duration, _scale = 1f, _projection = 1f;
        private int _order;
        private bool _running;

        public MacAnhVfxRecipe Recipe => recipe;

        public void Configure(MacAnhVfxRecipe effect, Material shared)
        {
            recipe = effect;
            material = shared;
        }

        private void EnsureLayers()
        {
            if (_renderers != null || recipe == null)
                return;
            var count = recipe.layers.Length;
            _roots = new Transform[count];
            _spins = new Transform[count];
            _renderers = new SpriteRenderer[count];
            _block = new MaterialPropertyBlock();
            for (var i = 0; i < count; i++)
            {
                var root = new GameObject(recipe.layers[i].name).transform;
                root.SetParent(transform, false);
                var spin = new GameObject("Spin").transform;
                spin.SetParent(root, false);
                var sprite = new GameObject("Frames", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                sprite.transform.SetParent(spin, false);
                sprite.sharedMaterial = material;
                sprite.enabled = false;
                _roots[i] = root;
                _spins[i] = spin;
                _renderers[i] = sprite;
            }
            if (recipe.screenDim > 0f)
            {
                if (_dimSprite == null)
                {
                    // RGB 0 / A 1: with the flipbook shader this is pure underlay, i.e. a premultiplied darkening.
                    var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "MacAnh screen dim", hideFlags = HideFlags.DontSave };
                    texture.SetPixels(Repeat(new Color(0f, 0f, 0f, 1f), 16));
                    texture.Apply();
                    _dimSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4f);
                    _dimSprite.hideFlags = HideFlags.DontSave;
                }
                _dim = new GameObject("Screen dim", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                _dim.transform.SetParent(transform, false);
                _dim.sprite = _dimSprite;
                _dim.sharedMaterial = material;
                _dim.sortingOrder = DimOrder;
                _dim.enabled = false;
            }
        }

        private static Color[] Repeat(Color colour, int count)
        {
            var colours = new Color[count];
            for (var i = 0; i < count; i++)
                colours[i] = colour;
            return colours;
        }

        public void Begin(MacAnhFxQuality quality, float duration)
        {
            EnsureLayers();
            _quality = quality;
            _duration = Mathf.Max(.05f, duration);
            _seconds = 0f;
            _phase = MacAnhVfxPhase.Release;
            _running = true;
            Apply();
        }

        public void SetBattlePresentation(float radius, int order, float groundProjection, MacAnhFxKind kind)
        {
            _scale = recipe == null ? 1f : Mathf.Max(.01f, radius) / recipe.referenceRadius;
            _order = order;
            _projection = Mathf.Clamp(groundProjection, .05f, 1f);
            Apply();
        }

        public void SetCharge()
        {
            _phase = MacAnhVfxPhase.Charge;
            Apply();
        }

        public void Advance(float delta)
        {
            if (!_running)
                return;
            _seconds += delta;
            Apply();
        }

        public void Stop()
        {
            _running = false;
            if (_renderers == null)
                return;
            foreach (var r in _renderers)
                r.enabled = false;
            if (_dim != null)
                _dim.enabled = false;
        }

        private float ApplyDim()
        {
            if (_dim == null)
                return 0f;
            // The release instance takes over from the charge instance in the same frame, so only the charge fades in;
            // otherwise the battlefield would flash bright between the two.
            // Likewise the charge holds its dim to the end (the release continues it) and only the release fades out.
            var charge = _phase == MacAnhVfxPhase.Charge;
            var fadeIn = charge ? recipe.dimFadeIn : 0f;
            var fadeOut = charge ? 0f : recipe.dimFadeOut;
            var fade = Mathf.Min(fadeIn > 0f ? Mathf.Clamp01(_seconds / fadeIn) : 1f,
                fadeOut > 0f ? Mathf.Clamp01((_duration - _seconds) / fadeOut) : 1f);
            var amount = _running ? recipe.screenDim * fade : 0f;
            _dim.enabled = amount > .001f;
            if (!_dim.enabled)
                return 0f;
            // Big enough to cover any battle camera; it follows the effect, which stays on screen while it plays.
            _dim.transform.localScale = new Vector3(400f, 400f, 1f);
            _dim.transform.rotation = Quaternion.identity;
            _dim.color = new Color(1f, 1f, 1f, 1f);
            _block.Clear();
            _block.SetFloat(GlowId, 0f);
            _block.SetFloat(UnderlayId, amount);
            _block.SetColor(UnderlayColourId, recipe.dimColour);
            _dim.SetPropertyBlock(_block);
            return amount;
        }

        private void Apply()
        {
            if (_renderers == null || recipe == null)
                return;
            // Projectiles are spawned rotated towards their screen direction; keep "up" up when they fly left.
            var flip = Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad) < 0f ? -1f : 1f;
            var dimmed = ApplyDim() > 0f;
            for (var i = 0; i < _renderers.Length; i++)
            {
                var layer = recipe.layers[i];
                var r = _renderers[i];
                var end = recipe.LayerEnd(layer, _duration);
                var active = _running && layer.phase == _phase && layer.frames.Length > 0 &&
                             !(layer.highQualityOnly && _quality == MacAnhFxQuality.Low) &&
                             _seconds >= layer.start && _seconds <= end;
                r.enabled = active;
                if (!active)
                    continue;
                var age = _seconds - layer.start;
                var life = Mathf.Max(.001f, end - layer.start);
                var fade = Mathf.Min(layer.fadeIn > 0f ? Mathf.Clamp01(age / layer.fadeIn) : 1f,
                    layer.fadeOut > 0f ? Mathf.Clamp01((end - _seconds) / layer.fadeOut) : 1f);
                var index = Mathf.FloorToInt(age * layer.framesPerSecond);
                index = layer.loop ? index % layer.frames.Length : Mathf.Min(index, layer.frames.Length - 1);
                r.sprite = layer.frames[index];
                r.color = new Color(layer.tint.r, layer.tint.g, layer.tint.b, layer.tint.a * fade);
                r.sortingOrder = (dimmed ? DimOrder + 10 : layer.plane == MacAnhVfxPlane.Ground ? -4990 : _order) + layer.sortingOffset;
                _block.Clear();
                _block.SetFloat(GlowId, layer.glow);
                _block.SetFloat(UnderlayId, layer.underlay);
                _block.SetColor(UnderlayColourId, layer.underlayColour);
                r.SetPropertyBlock(_block);

                var size = _scale * Mathf.Lerp(layer.scaleFrom, layer.scaleTo, Mathf.SmoothStep(0f, 1f, age / life));
                var root = _roots[i];
                root.localPosition = new Vector3(0f, layer.height * _scale, 0f);
                if (layer.plane == MacAnhVfxPlane.Ground)
                {
                    // Squash after the spin so the ring stays an ellipse of the ground plane while it turns.
                    root.localScale = new Vector3(1f, _projection, 1f);
                    _spins[i].localRotation = Quaternion.Euler(0f, 0f, layer.spinDegreesPerSecond * _seconds);
                    _spins[i].localScale = Vector3.one * size;
                }
                else
                {
                    var stretch = layer.stretchToGround ? _projection / recipe.renderProjection : 1f;
                    root.localScale = new Vector3(1f, flip * stretch, 1f);
                    _spins[i].localRotation = Quaternion.identity;
                    _spins[i].localScale = Vector3.one * size;
                }
            }
        }

        private void OnDisable() => Stop();
    }
}
