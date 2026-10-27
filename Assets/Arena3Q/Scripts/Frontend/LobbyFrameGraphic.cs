using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    // A code-native bevel keeps its stroke width when cards and tabs resize.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LobbyFrameGraphic : MaskableGraphic
    {
        public float corner = 7, stroke = 1.5f;
        public Color fill = Color.clear;
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear(); var bounds = rectTransform.rect;
            var outer = Polygon(bounds, corner);
            var inner = Polygon(new Rect(bounds.x + stroke, bounds.y + stroke,
                Mathf.Max(0, bounds.width - stroke * 2), Mathf.Max(0, bounds.height - stroke * 2)), Mathf.Max(0, corner - stroke));
            for (var i = 0; i < outer.Length; i++)
            {
                vertices.AddVert(outer[i], color, Vector2.zero); vertices.AddVert(inner[i], color, Vector2.zero);
            }
            for (var i = 0; i < outer.Length; i++)
            {
                var next = (i + 1) % outer.Length;
                vertices.AddTriangle(i * 2, next * 2, i * 2 + 1);
                vertices.AddTriangle(next * 2, next * 2 + 1, i * 2 + 1);
            }
            if (fill.a <= 0) return;
            var start = vertices.currentVertCount;
            vertices.AddVert(bounds.center, fill, Vector2.zero);
            foreach (var point in inner) vertices.AddVert(point, fill, Vector2.zero);
            for (var i = 0; i < inner.Length; i++) vertices.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % inner.Length);
        }
        private static Vector2[] Polygon(Rect bounds, float corner)
        {
            corner = Mathf.Min(corner, Mathf.Min(bounds.width, bounds.height) * .5f);
            return new[] { new Vector2(bounds.xMin + corner, bounds.yMax), new Vector2(bounds.xMax - corner, bounds.yMax),
                new Vector2(bounds.xMax, bounds.yMax - corner), new Vector2(bounds.xMax, bounds.yMin + corner),
                new Vector2(bounds.xMax - corner, bounds.yMin), new Vector2(bounds.xMin + corner, bounds.yMin),
                new Vector2(bounds.xMin, bounds.yMin + corner), new Vector2(bounds.xMin, bounds.yMax - corner) };
        }
    }
}
