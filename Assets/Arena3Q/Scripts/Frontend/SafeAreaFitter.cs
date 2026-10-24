using UnityEngine;

namespace Arena3Q.Frontend
{
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        // Tests may supply a synthetic cutout without changing PlayerSettings or the OS display.
        public static Rect? OverrideArea { get; set; }
        private Rect _last; private Vector2Int _screen;
        private void OnEnable() => Apply();
        private void Update()
        {
            if (_last != (OverrideArea ?? Screen.safeArea) || _screen != new Vector2Int(Screen.width, Screen.height)) Apply();
        }
        public void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            _last = OverrideArea ?? Screen.safeArea; _screen = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(_last.xMin / Screen.width, _last.yMin / Screen.height);
            rect.anchorMax = new Vector2(_last.xMax / Screen.width, _last.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
