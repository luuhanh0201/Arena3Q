using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Short-lived feedback marker: shrinks, fades and removes itself.</summary>
    public sealed class FadeAndDestroy : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.6f;

        private SpriteRenderer _renderer;
        private Color _color;
        private Vector3 _scale;
        private float _age;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _color = _renderer != null ? _renderer.color : Color.white;
            _scale = transform.localScale;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / lifetime);
            transform.localScale = _scale * Mathf.Lerp(1f, 0.4f, t);
            if (_renderer != null)
                _renderer.color = new Color(_color.r, _color.g, _color.b, _color.a * (1f - t));
            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
