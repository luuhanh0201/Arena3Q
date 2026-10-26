using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Lobby2D
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LuBuCutoutGraphic : MaskableGraphic
    {
        public LuBuCutoutDefinition.Part part;
        [System.NonSerialized] public LuBuCutoutPose pose;
        public override Texture mainTexture => part?.texture ? part.texture : Texture2D.whiteTexture;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); if(part == null || pose == null)return;
            if(part.onlyClip>=0&&pose.clipIndex!=part.onlyClip||part.exceptClip>=0&&pose.clipIndex==part.exceptClip)return;
            if(part.eyelids && pose.blink<=0)return;
            var tint=(Color32)color;
            if(part.eyelids)tint.a=(byte)(tint.a*pose.blink);
            for(var i=0;i<part.vertices.Length;i++)helper.AddVert(pose.Vertex(part,i),tint,part.uv[i]);
            for(var i=0;i<part.triangles.Length;i+=3)helper.AddTriangle(part.triangles[i],part.triangles[i+1],part.triangles[i+2]);
        }
    }
}
