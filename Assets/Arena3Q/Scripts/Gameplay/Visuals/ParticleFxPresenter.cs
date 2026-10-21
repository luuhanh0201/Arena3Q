using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// A store particle effect (a Shuriken prefab such as Hovl Studio's Magic circle 2) played as a Mặc Ảnh battle
    /// effect through <see cref="MacAnhFx"/>: on the battle clock (pause and speed follow the battle), sized to the
    /// skill's radius, standing on the battle ground (the prefab is authored Y-up; its parent turns it to up = -Z) and
    /// fading out instead of popping. The particles are real 3D, so units in the effect stand inside it by depth.
    /// </summary>
    public sealed class ParticleFxPresenter : MonoBehaviour, IMacAnhSpatialEffect
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("The turned child holding the effect (scaled to the skill radius).")]
        [SerializeField] private Transform content;
        [Tooltip("Ground radius the effect was authored at; the battle radius divided by this scales it.")]
        [SerializeField, Min(.01f)] private float referenceRadius = 1.7f;
        [Tooltip("Seconds over which the effect fades at its end (and when it is told to disperse).")]
        [SerializeField, Min(.01f)] private float fadeSeconds = .45f;

        private ParticleSystem _root;
        private ParticleSystemRenderer[] _renderers;
        private readonly List<Color> _colours = new List<Color>();
        private MaterialPropertyBlock _block;
        private float _seconds, _duration, _vanishAt = float.MaxValue, _vanishSeconds;
        private bool _running, _charge;

        public void Configure(Transform turned, float radius)
        {
            content = turned;
            referenceRadius = radius;
        }

        private void Ensure()
        {
            if (_root != null)
                return;
            _root = content.GetComponentInChildren<ParticleSystem>(true);
            _renderers = content.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var r in _renderers)
                _colours.Add(r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId)
                    ? r.sharedMaterial.GetColor(BaseColorId) : Color.white);
        }

        public void Begin(MacAnhFxQuality quality, float duration)
        {
            Ensure();
            _seconds = 0f;
            _duration = Mathf.Max(.05f, duration);
            _vanishAt = float.MaxValue;
            _running = true;
            _charge = false;
            content.gameObject.SetActive(true);
            // Restarted and paused: from now on only Advance moves it.
            _root.Simulate(0f, true, true, false);
            ApplyFade(1f);
        }

        public void SetBattlePresentation(float radius, int order, float groundProjection, MacAnhFxKind kind)
        {
            Ensure();
            content.localScale = Vector3.one * (Mathf.Max(.01f, radius) / referenceRadius);
            for (var i = 0; i < _renderers.Length; i++)
                _renderers[i].sortingOrder = order + i;
        }

        /// <summary>Wind-up before the release: only the circle itself, without the bursts around it.</summary>
        public void SetCharge()
        {
            _charge = true;
            foreach (var system in _root.GetComponentsInChildren<ParticleSystem>(true))
                if (system != _root)
                    system.Clear(false);
        }

        /// <summary>Disperse over <paramref name="seconds"/> (the charge handed over to the release).</summary>
        public void Vanish(float seconds)
        {
            if (!_running)
                return;
            _vanishAt = Mathf.Min(_vanishAt, _seconds);
            _vanishSeconds = Mathf.Max(.01f, seconds);
        }

        public void Advance(float delta)
        {
            if (!_running || delta <= 0f)
                return;
            _seconds += delta;
            _root.Simulate(delta, !_charge, false, false);
            var end = Mathf.Clamp01((_duration - _seconds) / fadeSeconds);
            var vanish = _seconds >= _vanishAt ? 1f - Mathf.Clamp01((_seconds - _vanishAt) / _vanishSeconds) : 1f;
            ApplyFade(Mathf.Min(end, vanish));
        }

        public void Stop()
        {
            if (!_running)
                return;
            _running = false;
            if (_root != null)
                _root.Clear(true);
            if (content != null)
                content.gameObject.SetActive(false);
        }

        // Colour × fade on every renderer: alpha-blended parts thin out, additive ones dim.
        private void ApplyFade(float fade)
        {
            _block ??= new MaterialPropertyBlock();
            for (var i = 0; i < _renderers.Length; i++)
            {
                _block.Clear();
                var c = _colours[i];
                _block.SetColor(BaseColorId, new Color(c.r * fade, c.g * fade, c.b * fade, c.a * fade));
                _renderers[i].SetPropertyBlock(_block);
            }
        }
    }
}
