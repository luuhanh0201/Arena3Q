using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    public sealed class LobbyHorizontalGradient : BaseMeshEffect
    {
        private Color _left = Color.white, _right = Color.white;
        public void Configure(Color left, Color right) { _left = left; _right = right; graphic.SetVerticesDirty(); }
        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive()) return;
            var bounds = graphic.rectTransform.rect; var vertex = new UIVertex();
            for (var i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                vertex.color *= Color.Lerp(_left, _right, Mathf.InverseLerp(bounds.xMin, bounds.xMax, vertex.position.x));
                vertices.SetUIVertex(vertex, i);
            }
        }
    }
}
