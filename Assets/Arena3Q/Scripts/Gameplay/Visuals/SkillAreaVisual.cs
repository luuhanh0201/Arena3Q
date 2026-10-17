using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Placeholder skill area on the ground: stays for the skill's duration (following a unit if asked), then fades.</summary>
    public sealed class SkillAreaVisual : MonoBehaviour
    {
        private const float FadeSeconds = 0.3f;

        private SpriteRenderer _renderer;
        private Color _color;
        private float _seconds;
        private float _age;
        private UnitBase _follow;

        public void Setup(float seconds, UnitBase follow)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _color = _renderer.color;
            _seconds = Mathf.Max(FadeSeconds, seconds);
            _follow = follow;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_follow != null && _follow.IsAlive)
                transform.localPosition = _follow.Position;
            var fade = Mathf.Clamp01((_seconds - _age) / FadeSeconds);
            if (_renderer != null)
                _renderer.color = new Color(_color.r, _color.g, _color.b, _color.a * fade);
            if (_age >= _seconds)
                Destroy(gameObject);
        }
    }
}
