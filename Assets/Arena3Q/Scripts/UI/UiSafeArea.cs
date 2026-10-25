using UnityEngine;

namespace Arena3Q.UI
{
    /// <summary>Keeps battle controls inside the current cutout/system-bar area, including transient Android bars.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiSafeArea : MonoBehaviour
    {
        public static Rect? OverrideArea { get; set; }
        private Rect _area;
        private Vector2Int _screen;
        private void OnEnable() => Apply();
        private void LateUpdate()
        {
            if (_area != (OverrideArea ?? Screen.safeArea) || _screen != new Vector2Int(Screen.width, Screen.height)) Apply();
        }
        public void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            _area = OverrideArea ?? Screen.safeArea;
            _screen = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(_area.xMin / Screen.width, _area.yMin / Screen.height);
            rect.anchorMax = new Vector2(_area.xMax / Screen.width, _area.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
