using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>Vertex tint for UI fills; no extra texture, material or per-frame allocation.</summary>
    public sealed class UiGradient : BaseMeshEffect
    {
        private Color _bottom = Color.white, _top = Color.white;
        public void Configure(Color bottom, Color top) { _bottom = bottom; _top = top; graphic.SetVerticesDirty(); }
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            var rect = graphic.rectTransform.rect; var vertex = new UIVertex();
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.color *= Color.Lerp(_bottom, _top, Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y));
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
