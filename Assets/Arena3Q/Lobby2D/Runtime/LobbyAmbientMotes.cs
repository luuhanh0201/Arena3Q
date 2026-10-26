using UnityEngine;
using UnityEngine.UI;
namespace Arena3Q.Lobby2D
{
    public sealed class LobbyAmbientMotes:MonoBehaviour
    {
        public LobbyArtCatalog catalog;
        public bool recruitment;
        private Image[] _motes;
        private float _clock;
        private void Start()
        {
            _motes=new Image[recruitment?catalog.recruitMoteCount:catalog.ambientMoteCount];
            for(var i=0;i<_motes.Length;i++){
                var rect=new GameObject("Mote_"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<RectTransform>();
                rect.SetParent(transform,false);rect.SetAsFirstSibling();rect.anchorMin=rect.anchorMax=Vector2.zero;
                rect.sizeDelta=Vector2.one*Mathf.Lerp(catalog.moteMinSize,catalog.moteMaxSize,Fraction(i*.618034f));
                var image=rect.GetComponent<Image>();image.sprite=catalog.Sprite("Ambient_Mote");image.raycastTarget=false;_motes[i]=image;
            }
        }
        private void Update()
        {
            if(_motes==null)return;_clock+=Time.unscaledDeltaTime;var size=((RectTransform)transform).rect.size;
            var active=recruitment?catalog.recruitAnimation:catalog.ambientAnimation;
            for(var i=0;i<_motes.Length;i++){
                var phase=Fraction(_clock/catalog.moteLifetime+i*.618034f);
                var x=(.12f+.76f*Fraction(i*.754877f))*size.x+Mathf.Sin(_clock*.4f+i)*catalog.moteDrift;
                var y=phase*size.y;
                _motes[i].rectTransform.anchoredPosition=new Vector2(x,y);
                var color=catalog.moteColor;var target=active?Mathf.Sin(phase*Mathf.PI)*catalog.moteAlpha:0;
                color.a=Mathf.MoveTowards(_motes[i].color.a,target,Time.unscaledDeltaTime/catalog.moteFadeSeconds);_motes[i].color=color;
            }
        }
        private static float Fraction(float value)=>value-Mathf.Floor(value);
    }
}
