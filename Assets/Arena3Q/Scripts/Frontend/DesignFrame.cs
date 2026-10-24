using UnityEngine;

namespace Arena3Q.Frontend
{
    public sealed class DesignFrame : MonoBehaviour
    {
        private Vector2 _size;
        public void Configure(Vector2 size) { _size = size; Fit(); }
        private void LateUpdate() => Fit();
        private void Fit()
        {
            if (_size.x <= 0 || !(transform.parent is RectTransform parent)) return;
            var size = parent.rect.size;
            var scale = Mathf.Min(size.x / _size.x, size.y / _size.y);
            ((RectTransform)transform).sizeDelta = _size;
            transform.localScale = Vector3.one * Mathf.Max(.001f, scale);
        }
    }
}
