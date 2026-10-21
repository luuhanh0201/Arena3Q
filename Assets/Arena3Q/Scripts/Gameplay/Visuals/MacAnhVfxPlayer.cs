using System.Collections.Generic;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Plays a <see cref="MacAnhVfxRecipe"/> (layers of pre-rendered frames) inside a pooled <see cref="MacAnhFx"/>.
    /// Visual only: timing comes from the effect's clock (battle time), never from gameplay rules.
    /// </summary>
    public sealed class MacAnhVfxPlayer : MonoBehaviour, IMacAnhSpatialEffect, IMacAnhBillboard
    {
        [SerializeField] private MacAnhVfxRecipe recipe;
        [SerializeField] private Material material;

        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int UnderlayId = Shader.PropertyToID("_Underlay");
        private static readonly int UnderlayColourId = Shader.PropertyToID("_UnderlayColor");

        /// <summary>Above the painted ground (-32000..-31970), below units, decorations and the effect layers, so only
        /// the ground darkens and everything standing on it stays lit like a stage.</summary>
        private const int DimOrder = -6000;
        private static Sprite _dimSprite;

        private Transform[] _roots, _spins;
        // Mesh layers, not SpriteRenderers: each draws the current and the next frame in one quad (frame blending).
        private MeshRenderer[] _renderers;
        private MeshFilter[] _filters;
        private Dictionary<int, Mesh>[] _pairs;
        private SpriteRenderer _dim;
        private MaterialPropertyBlock _block;
        private MacAnhFxQuality _quality;
        private MacAnhVfxPhase _phase;
        private float _seconds, _duration, _scale = 1f, _projection = 1f;
        private int _order;
        private bool _running;
        private float _vanishAt, _vanishSeconds, _seed;
        // Nothing pops in or out: every layer takes at least this long to gather and to disperse.
        private const float MinFadeIn = .1f, MinFadeOut = .16f;
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int BreathId = Shader.PropertyToID("_Breath");
        private static readonly int EffectBloomId = Shader.PropertyToID("_EffectBloom");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int EffectTimeId = Shader.PropertyToID("_EffectTime");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int NextTexId = Shader.PropertyToID("_NextTex");
        private static readonly int FrameBlendId = Shader.PropertyToID("_FrameBlend");
        private static readonly int RectAId = Shader.PropertyToID("_RectA");
        private static readonly int RectBId = Shader.PropertyToID("_RectB");
        private static readonly int TintId = Shader.PropertyToID("_Tint");

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
            _renderers = new MeshRenderer[count];
            _filters = new MeshFilter[count];
            _pairs = new Dictionary<int, Mesh>[count];
            _block = new MaterialPropertyBlock();
            for (var i = 0; i < count; i++)
            {
                var root = new GameObject(recipe.layers[i].name).transform;
                root.SetParent(transform, false);
                var spin = new GameObject("Spin").transform;
                spin.SetParent(root, false);
                var frames = new GameObject("Frames", typeof(MeshFilter), typeof(MeshRenderer));
                frames.transform.SetParent(spin, false);
                var r = frames.GetComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                r.enabled = false;
                _roots[i] = root;
                _spins[i] = spin;
                _renderers[i] = r;
                _filters[i] = frames.GetComponent<MeshFilter>();
                _pairs[i] = new Dictionary<int, Mesh>();
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
            _vanishSeconds = 0f;
            _seed = Random.Range(0f, 100f);
            Apply();
        }

        public void SetBattlePresentation(float radius, int order, float groundProjection, MacAnhFxKind kind)
        {
            _scale = recipe == null ? 1f : Mathf.Max(.01f, radius) / recipe.referenceRadius;
            _order = order;
            _projection = Mathf.Clamp(groundProjection, .05f, 1f);
            Apply();
        }

        private Quaternion _upright = Quaternion.identity;

        /// <summary>3D: upright layers face the camera with this rotation; ground layers stay on the ground.</summary>
        public void SetUpright(Quaternion upright) => _upright = upright;

        private float _depthBias;

        /// <summary>3D: upright layers are drawn this many effect radii nearer the camera (see Apply).</summary>
        public void SetDepthBias(float effectRadii) => _depthBias = Mathf.Max(0f, effectRadii);

        private float _bloomBoost;

        /// <summary>3D battle bloom: how far the hottest light is pushed past 1 (0 = as painted, outside the battle).</summary>
        public void SetBloomBoost(float boost) => _bloomBoost = Mathf.Max(0f, boost);

        public void SetCharge()
        {
            _phase = MacAnhVfxPhase.Charge;
            Apply();
        }

        /// <summary>Vanish now (hit, end of flight, hand-over to the release): erode away over <paramref name="seconds"/>
        /// from wherever each layer's fade currently is, instead of disappearing in one frame.</summary>
        public void Vanish(float seconds)
        {
            if (!_running || seconds <= 0f)
                return;
            var at = _seconds + seconds;
            if (_vanishSeconds > 0f && at >= _vanishAt)
                return;
            _vanishAt = at;
            _vanishSeconds = seconds;
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
            // A vanishing charge hands its dim to the release at once (two dims would stack into a darker flash).
            var amount = _running && !(charge && _vanishSeconds > 0f) ? recipe.screenDim * fade : 0f;
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
            ApplyDim();
            for (var i = 0; i < _renderers.Length; i++)
            {
                var layer = recipe.layers[i];
                var r = _renderers[i];
                var end = recipe.LayerEnd(layer, _duration);
                var vanish = _vanishSeconds > 0f ? Mathf.Clamp01((_vanishAt - _seconds) / _vanishSeconds) : 1f;
                var active = _running && layer.phase == _phase && layer.frames.Length > 0 &&
                             !(layer.highQualityOnly && _quality == MacAnhFxQuality.Low) &&
                             _seconds >= layer.start && _seconds <= end && vanish > 0f;
                r.enabled = active;
                if (!active)
                    continue;
                var age = _seconds - layer.start;
                var life = Mathf.Max(.001f, end - layer.start);
                // 0 means "continues another layer" (release taking over the charge, charge held to the hand-over):
                // no fade there; any other value is at least the minimum, so nothing pops.
                var fadeIn = layer.fadeIn <= 0f ? 0f : Mathf.Max(layer.fadeIn, MinFadeIn);
                var fadeOut = layer.fadeOut <= 0f ? 0f : Mathf.Min(Mathf.Max(layer.fadeOut, MinFadeOut), life * .5f);
                var fade = Mathf.Min(fadeIn > 0f ? Mathf.SmoothStep(0f, 1f, age / fadeIn) : 1f,
                    fadeOut > 0f ? Mathf.SmoothStep(0f, 1f, (end - _seconds) / fadeOut) : 1f) * vanish;
                // Blend each frame into the next on the effect clock (spec §1): a 12-24 fps flipbook still moves
                // smoothly at 60 fps. Looping layers blend the last frame into the first; others hold the last.
                var count = layer.frames.Length;
                var position = Mathf.Max(0f, age * layer.framesPerSecond);
                var index = Mathf.FloorToInt(position);
                var blend = position - index;
                int next;
                if (layer.loop)
                {
                    index %= count;
                    next = (index + 1) % count;
                }
                else if (index >= count - 1)
                {
                    index = next = count - 1;
                    blend = 0f;
                }
                else
                    next = index + 1;
                if (next == index)
                    blend = 0f;
                var current = layer.frames[index];
                var following = layer.frames[next];
                _filters[i].sharedMesh = PairMesh(i, index, next, current, following);
                // Dissolving layers erode along the noise; a little alpha fade on top keeps the edges soft.
                var alpha = layer.dissolve ? Mathf.Lerp(.55f, 1f, fade) * (fade > 0f ? 1f : 0f) : fade;
                r.sortingOrder = (layer.plane == MacAnhVfxPlane.Ground ? -4990 : _order) + layer.sortingOffset;
                _block.Clear();
                _block.SetColor(TintId, new Color(layer.tint.r, layer.tint.g, layer.tint.b, layer.tint.a * alpha));
                _block.SetTexture(MainTexId, current.texture);
                _block.SetTexture(NextTexId, following.texture);
                _block.SetVector(RectAId, AtlasRect(current));
                _block.SetVector(RectBId, AtlasRect(following));
                _block.SetFloat(FrameBlendId, blend);
                _block.SetFloat(GlowId, layer.glow);
                _block.SetFloat(UnderlayId, layer.underlay);
                _block.SetColor(UnderlayColourId, layer.underlayColour);
                _block.SetFloat(DissolveId, layer.dissolve ? 1f - fade : 0f);
                _block.SetFloat(BreathId, layer.breath);
                _block.SetFloat(NoiseScaleId, layer.noiseScale);
                _block.SetFloat(SeedId, _seed + i * 17.3f);
                _block.SetFloat(EffectBloomId, _bloomBoost);
                _block.SetFloat(EffectTimeId, _seconds);
                r.SetPropertyBlock(_block);

                var size = _scale * Mathf.Lerp(layer.scaleFrom, layer.scaleTo, Mathf.SmoothStep(0f, 1f, age / life));
                var root = _roots[i];
                root.localPosition = new Vector3(0f, layer.height * _scale, 0f);
                if (layer.plane == MacAnhVfxPlane.Ground)
                {
                    root.localRotation = Quaternion.identity;
                    // Squash after the spin so the ring stays an ellipse of the ground plane while it turns.
                    root.localScale = new Vector3(1f, _projection, 1f);
                    _spins[i].localRotation = Quaternion.Euler(0f, 0f, layer.spinDegreesPerSecond * _seconds);
                    _spins[i].localScale = Vector3.one * size;
                }
                else
                {
                    // 3D: the picture faces the camera, still rolled like the effect (projectiles turn towards their
                    // screen direction); world = upright · roll, so local = roll⁻¹ · upright · roll.
                    var roll = transform.rotation;
                    root.localRotation = Quaternion.Inverse(roll) * _upright * roll;
                    root.localPosition = root.localRotation * new Vector3(0f, layer.height * _scale, 0f);
                    // Moved along the view (towards the camera, or away for a layer behind the units): with the orthographic
                    // camera it stays in the same place on screen, but a 3D model at the target (a command post) no longer
                    // cuts it off with a straight line, and the units inside stand between the far and the near half.
                    if (_depthBias > 0f)
                        root.localPosition += transform.InverseTransformVector(
                            _upright * Vector3.back * ((layer.behindUnits ? -1f : 1f) * _depthBias * _scale * recipe.referenceRadius));
                    // Ground is foreshortened by the tilted camera; the billboard itself is not.
                    // Identity upright retains the legacy flat-camera projection.
                    var cameraProjection = Mathf.Abs((_upright * Vector3.forward).z);
                    var stretch = layer.stretchToGround ? _projection * cameraProjection / recipe.renderProjection : 1f;
                    root.localScale = new Vector3(1f, flip * stretch, 1f);
                    _spins[i].localRotation = Quaternion.identity;
                    _spins[i].localScale = Vector3.one * size;
                }
            }
        }

        /// <summary>The frame's area on its atlas page, in UV (min xy, max zw): the shader masks each frame to it.</summary>
        private static Vector4 AtlasRect(Sprite sprite)
        {
            var rect = sprite.textureRect;
            var size = new Vector2(sprite.texture.width, sprite.texture.height);
            return new Vector4(rect.xMin / size.x, rect.yMin / size.y, rect.xMax / size.x, rect.yMax / size.y);
        }

        /// <summary>One quad over the union of both frames' rects (pivots and crops may differ), with each frame's
        /// atlas UVs in its own channel. Cached per layer and frame pair, so a running effect builds no meshes.</summary>
        private Mesh PairMesh(int layer, int index, int next, Sprite a, Sprite b)
        {
            var key = index * 4096 + next;
            if (_pairs[layer].TryGetValue(key, out var mesh))
                return mesh;
            LocalRect(a, out var minA, out var maxA);
            LocalRect(b, out var minB, out var maxB);
            var min = Vector2.Min(minA, minB);
            var max = Vector2.Max(maxA, maxB);
            var corners = new[] { min, new Vector2(max.x, min.y), new Vector2(min.x, max.y), max };
            var vertices = new Vector3[4];
            var uvA = new Vector2[4];
            var uvB = new Vector2[4];
            for (var c = 0; c < 4; c++)
            {
                vertices[c] = corners[c];
                uvA[c] = AtlasUv(a, corners[c]);
                uvB[c] = AtlasUv(b, corners[c]);
            }
            mesh = new Mesh { name = $"{recipe.layers[layer].name} {index}>{next}", hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.uv = uvA;
            mesh.uv2 = uvB;
            mesh.colors32 = new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255),
                new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            _pairs[layer][key] = mesh;
            return mesh;
        }

        private static void LocalRect(Sprite sprite, out Vector2 min, out Vector2 max)
        {
            var ppu = sprite.pixelsPerUnit;
            min = (sprite.textureRectOffset - sprite.pivot) / ppu;
            max = min + sprite.textureRect.size / ppu;
        }

        private static Vector2 AtlasUv(Sprite sprite, Vector2 local)
        {
            var pixel = local * sprite.pixelsPerUnit + sprite.pivot - sprite.textureRectOffset + sprite.textureRect.position;
            return new Vector2(pixel.x / sprite.texture.width, pixel.y / sprite.texture.height);
        }

        private void OnDisable() => Stop();

        private void OnDestroy()
        {
            if (_pairs == null)
                return;
            foreach (var cache in _pairs)
                foreach (var mesh in cache.Values)
                    if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
    }
}
