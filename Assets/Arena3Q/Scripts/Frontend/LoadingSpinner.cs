using UnityEngine;

namespace Arena3Q.Frontend
{
    /// <summary>Two reusable alpha ring sprites from the handoff's CSS shapes, on the standard uGUI Image path.</summary>
    public sealed class LoadingSpinner : MonoBehaviour
    {
        private RectTransform _outer, _inner, _core;
        private float _time;
        public static void Create(StartupUi ui, Transform parent, Vector2 center)
        {
            var root = ui.Rect(parent, "LoadingSpinner", new Rect(center.x - 70, center.y - 70, 140, 140));
            ui.Image(root, "NavyDisc", ui.Config.spinnerDisc, new Rect(0, 0, 140, 140));
            var spinner = root.gameObject.AddComponent<LoadingSpinner>();
            spinner._outer = Center(ui.Image(root, "OuterRing", ui.Config.ringOuter, new Rect(6, 6, 128, 128)).rectTransform);
            spinner._inner = Center(ui.Image(root, "InnerRing", ui.Config.ringInner, new Rect(26, 26, 88, 88)).rectTransform);
            spinner._core = Center(ui.Image(root, "Diamond", null, new Rect(54, 54, 32, 32), ui.Config.gold).rectTransform);
            spinner._core.localRotation = Quaternion.Euler(0, 0, 45);
        }
        private static RectTransform Center(RectTransform rect)
        {
            var size = rect.sizeDelta; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition += new Vector2(size.x / 2, -size.y / 2); return rect;
        }
        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            _outer.localRotation = Quaternion.Euler(0, 0, -_time * 360);
            _inner.localRotation = Quaternion.Euler(0, 0, _time * 360 / 3.2f);
            _core.localScale = Vector3.one * Mathf.Lerp(.82f, 1.05f, .5f + .5f * Mathf.Sin(_time * Mathf.PI * 2 / 1.2f));
        }
    }
}
