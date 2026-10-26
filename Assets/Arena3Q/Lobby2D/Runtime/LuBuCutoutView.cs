using System;
using UnityEngine;

namespace Arena3Q.Lobby2D
{
    public sealed class LuBuCutoutView : MonoBehaviour
    {
        public LuBuCutoutDefinition definition;
        public int clipIndex;
        public bool animate = true;
        public bool effectsEnabled=true;
        public bool returnToIdle=true;
        public float clock;
        public event Action Impact;
        public LuBuCutoutPose Pose { get; private set; }
        private LuBuCutoutGraphic[] _parts;
        private bool _impactRaised;
        private LuBuSlashView _slash;
        private LuBuCutoutPose _previous;
        private float _transitionClock;
        public void Build()
        {
            if(_parts!=null)return;
            Pose=new LuBuCutoutPose(); _parts=new LuBuCutoutGraphic[definition.parts.Length];
            for(var i=0;i<_parts.Length;i++){
                var rect=new GameObject(definition.parts[i].name,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();
                rect.SetParent(transform,false); rect.anchorMin=rect.anchorMax=definition.framePivot;
                rect.anchoredPosition=Vector2.zero; rect.sizeDelta=definition.frameSize;
                var graphic=rect.gameObject.AddComponent<LuBuCutoutGraphic>();
                graphic.part=definition.parts[i]; graphic.pose=Pose; graphic.raycastTarget=false; _parts[i]=graphic;
            }
            if(definition.slash){
                var rect=new GameObject("IndependentSlash",typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(transform,false);
                rect.anchorMin=rect.anchorMax=definition.framePivot;rect.pivot=definition.framePivot;rect.sizeDelta=definition.frameSize;rect.anchoredPosition=Vector2.zero;
                _slash=rect.gameObject.AddComponent<LuBuSlashView>();_slash.definition=definition.slash;_slash.Build();
            }
            Sample(clipIndex,clock);
        }
        public void Sample(int index,float seconds)
        {
            if(_parts==null)Build(); clipIndex=index; clock=seconds; Pose.Evaluate(definition,index,seconds);
            if(_previous!=null){var amount=Mathf.SmoothStep(0,1,_transitionClock/Mathf.Max(.01f,definition.transitionSeconds));Pose.BlendFrom(_previous,amount);if(amount>=1)_previous=null;}
            foreach(var part in _parts){if(!part.isActiveAndEnabled){part.canvasRenderer.Clear();continue;}part.SetAllDirty();if(!Application.isPlaying){part.canvasRenderer.cull=false;part.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);}}
            if(_slash)_slash.Sample(Pose.normalized,index==2&&effectsEnabled,Time.unscaledDeltaTime,!Application.isPlaying);
        }
        public void ShowLayer(string layer)
        {
            foreach(var graphic in _parts)graphic.enabled=string.IsNullOrEmpty(layer)||graphic.part.layer==layer;
        }
        public void Play(int index){if(_parts==null)Build();_previous=Pose.Snapshot();_transitionClock=0;clipIndex=index;clock=0;_impactRaised=false;animate=true;}
        private void Start(){Build();}
        private void Update()
        {
            if(!animate)return;
            _transitionClock+=Time.unscaledDeltaTime;
            Sample(clipIndex,clock+Time.unscaledDeltaTime);
            if(clipIndex==2&&!_impactRaised&&clock>=definition.clips[2].duration*definition.impactNormalizedTime){_impactRaised=true;Impact?.Invoke();}
            if(clipIndex==2&&returnToIdle&&clock>=definition.clips[2].duration)Play(0);
        }
    }
}
