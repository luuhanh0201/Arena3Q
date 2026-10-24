using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>UI geometry, rather than font symbol characters. Both rings share the handoff centre.</summary>
    public sealed class LoadingSpinner : MaskableGraphic
    {
        private float _angle;
        public static void Create(StartupUi ui, Transform parent, Vector2 center)
        {
            var rect = ui.Rect(parent, "LoadingSpinner", new Rect(center.x - 70, center.y - 70, 140, 140));
            var spinner = rect.gameObject.AddComponent<LoadingSpinner>(); spinner.color = ui.Config.gold; spinner.raycastTarget = false;
            ui.Image(rect, "Diamond", ui.Config.rounded, new Rect(54, 54, 32, 32), ui.Config.gold).rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        }
        private void Update() { _angle += Time.unscaledDeltaTime * 360; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Ring(vh, 64, 5, _angle, 280, false);
            Ring(vh, 44, 2, -_angle / 3.2f, 360, true);
        }
        private void Ring(VertexHelper vh, float radius, float width, float rotation, float arc, bool dashed)
        {
            var center = rectTransform.rect.center;
            for (var i = 0; i < 72; i++)
            {
                if (dashed && i % 4 >= 2) continue;
                var a = i * 5f; if (a > arc) break;
                var first = vh.currentVertCount;
                for (var j = 0; j < 4; j++)
                {
                    var angle = (rotation + a + (j >= 2 ? 5 : 0)) * Mathf.Deg2Rad;
                    var r = radius - (j == 1 || j == 2 ? width : 0);
                    vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r, color, Vector2.zero);
                }
                vh.AddTriangle(first, first + 1, first + 2); vh.AddTriangle(first, first + 2, first + 3);
            }
        }
    }
}
