using UnityEngine;
using UnityEngine.UI;
namespace Arena3Q.Lobby2D
{
    public sealed class LobbyBannerCycle:MonoBehaviour
    {
        public LobbyArtCatalog catalog;
        private Image _base,_next;private float _clock;private int _index;
        private void Start()
        {
            _base=GetComponent<Image>();
            var rect=new GameObject("BannerCrossfade",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(transform,false);rect.SetAsFirstSibling();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            _next=rect.GetComponent<Image>();_next.raycastTarget=false;_next.color=Color.clear;
        }
        private void Update()
        {
            if(!_next||catalog.eventBannerSprites.Length<2||!catalog.bannerAnimation)return;
            _clock+=Time.unscaledDeltaTime;
            if(_clock>=catalog.bannerSeconds){_clock-=catalog.bannerSeconds;_index=(_index+1)%catalog.eventBannerSprites.Length;_base.sprite=catalog.Sprite(catalog.eventBannerSprites[_index]);}
            _next.sprite=catalog.Sprite(catalog.eventBannerSprites[(_index+1)%catalog.eventBannerSprites.Length]);
            var color=Color.white;color.a=Mathf.SmoothStep(0,1,Mathf.InverseLerp(catalog.bannerSeconds-catalog.bannerFadeSeconds,catalog.bannerSeconds,_clock));_next.color=color;
        }
    }
}
