using UnityEngine;
using UnityEngine.UI;
namespace Arena3Q.Lobby2D
{
    public sealed class LuBuSlashView:MonoBehaviour
    {
        public LuBuSlashDefinition definition;
        private Image _outer,_inner;private float _alpha;
        public void Build()
        {
            if(_outer)return;
            _outer=Layer("Crimson",1);_inner=Layer("Gold",definition.innerScale);
        }
        private Image Layer(string name,float scale)
        {
            var rect=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(transform,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.2f);rect.pivot=definition.pivot;
            rect.anchoredPosition=definition.offset;rect.sizeDelta=definition.size*scale;
            var image=rect.GetComponent<Image>();image.sprite=definition.sprite;image.raycastTarget=false;return image;
        }
        public void Sample(float normalized,bool active,float delta=1,bool snap=true)
        {
            Build();var progress=Mathf.InverseLerp(definition.start,definition.end,normalized);
            var target=active&&normalized>=definition.start&&normalized<=definition.end?Mathf.Sin(progress*Mathf.PI):0;
            _alpha=snap?target:Mathf.MoveTowards(_alpha,target,delta/Mathf.Max(.01f,definition.fadeSeconds));
            if(target>0){
                var rotation=Quaternion.Euler(0,0,Mathf.Lerp(definition.rotationStart,definition.rotationEnd,progress));
                _outer.rectTransform.localRotation=_inner.rectTransform.localRotation=rotation;
                var scale=Mathf.Lerp(definition.scaleStart,1,Mathf.Sin(progress*Mathf.PI*.5f));
                _outer.rectTransform.localScale=_inner.rectTransform.localScale=Vector3.one*scale;
            }
            var outer=definition.outer;outer.a=_alpha*definition.outerAlpha;_outer.color=outer;
            var inner=definition.inner;inner.a=_alpha*definition.innerAlpha;_inner.color=inner;
        }
    }
}
