using UnityEngine;
namespace Arena3Q.Lobby2D
{
    [CreateAssetMenu(menuName="Arena3Q/Lobby 2D/Lu Bu Slash")]
    public sealed class LuBuSlashDefinition:ScriptableObject
    {
        public Sprite sprite;
        public Vector2 size=new Vector2(1500,1500),offset=new Vector2(100,500),pivot=new Vector2(.5f,.6875f);
        public Color outer=new Color(.79f,.12f,.12f),inner=new Color(1,.82f,.4f);
        public float start=.45f,end=.71f,outerAlpha=.65f,innerAlpha=.8f,innerScale=.94f;
        public float rotationStart=-24,rotationEnd=24,scaleStart=.72f,fadeSeconds=.12f;
    }
}
