using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>Backgrounds span the physical canvas while controls stay in their safe 1280 design frame.</summary>
    public sealed class CanvasBackdrop : MonoBehaviour
    {
        private readonly Vector3[] _corners = new Vector3[4];
        public bool coverSprite;
        public float bottomHeight;
        private void LateUpdate()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || !(transform.parent is RectTransform parent)) return;
            var root = (RectTransform)canvas.rootCanvas.transform; root.GetWorldCorners(_corners);
            var min = parent.InverseTransformPoint(_corners[0]); var max = parent.InverseTransformPoint(_corners[2]);
            var rect = (RectTransform)transform; rect.anchorMin = rect.anchorMax = parent.pivot; rect.pivot = FrontendUi.Center;
            rect.anchoredPosition = (min + max) / 2; var size = (Vector2)(max - min);
            if (bottomHeight > 0)
            {
                rect.pivot = new Vector2(.5f, 0);
                rect.anchoredPosition = new Vector2((min.x + max.x) / 2, min.y);
                rect.sizeDelta = new Vector2(size.x, bottomHeight);
                return;
            }
            if (coverSprite)
            {
                var image = GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    var aspect = image.sprite.rect.width / image.sprite.rect.height;
                    if (size.x / size.y > aspect) size.y = size.x / aspect; else size.x = size.y * aspect;
                }
            }
            rect.sizeDelta = size;
        }
    }
}
