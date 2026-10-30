using System.Collections.Generic;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Exploration
{
    /// <summary>Decorative motion only. Reuses the battlefield wind contract and preserves shared source materials.</summary>
    public sealed class ChapterMapEnvironment : MonoBehaviour
    {
        private MapMotionConfig _config;
        private Material _water, _bamboo;
        private float _clock, _previousClock;
        private Vector4 _previousWind, _previousNoise;
        private bool _configured;
        private float _quality;
        private readonly List<(Renderer renderer, Material[] materials)> _originals=new List<(Renderer,Material[])>();
        private readonly Dictionary<Renderer,MaterialPropertyBlock> _blocks=new Dictionary<Renderer,MaterialPropertyBlock>();
        public int WaterRenderers { get; private set; }
        public int BambooRenderers { get; private set; }
        public float Clock => _clock;
        public Material WaterMaterial => _water;
        public Material BambooMaterial => _bamboo;

        public void Configure(ChapterMapController map)
        {
            _config=map.Config.motion;
            if(!_config.environmentMotion)return;
            _previousWind=Shader.GetGlobalVector("_ArenaWind");_previousNoise=Shader.GetGlobalVector("_ArenaWindNoise");_previousClock=Shader.GetGlobalFloat("_ArenaWindTime");
            _water=new Material(_config.waterMotionMaterial);_bamboo=new Material(_config.bambooMotionMaterial);
            var block=new MaterialPropertyBlock();
            foreach(var renderer in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var originals=renderer.sharedMaterials;var materials=(Material[])originals.Clone();bool changed=false;
                for(int i=0;i<materials.Length;i++)
                {
                    if(materials[i]==_config.waterSource){materials[i]=_water;WaterRenderers++;changed=true;}
                    else if(materials[i]==_config.bambooSource)
                    {
                        materials[i]=_bamboo;BambooRenderers++;changed=true;
                        if(!_blocks.ContainsKey(renderer)){var previous=new MaterialPropertyBlock();renderer.GetPropertyBlock(previous);_blocks.Add(renderer,previous);}
                        renderer.GetPropertyBlock(block);
                        block.SetVector("_ChapterPlantBounds",new Vector4(renderer.bounds.min.y,Mathf.Max(.1f,renderer.bounds.size.y),renderer.bounds.center.x,renderer.bounds.center.z));renderer.SetPropertyBlock(block);
                    }
                }
                if(changed){_originals.Add((renderer,originals));renderer.sharedMaterials=materials;}
            }
            _configured=true;Evaluate(0);
        }
        private bool LowPreset
        {
            get {int rate=DisplayMetrics.PreviewFrameRate>0?DisplayMetrics.PreviewFrameRate:Application.targetFrameRate;return rate>0&&rate<=_config.frameRates[0];}
        }
        private void Update()
        {
            if(!_configured)return;
            _quality=Mathf.MoveTowards(_quality,LowPreset?0:1,Time.unscaledDeltaTime/_config.environmentFadeSeconds);
            Apply(_clock+Time.unscaledDeltaTime);
        }
        // Deterministic seek for capture; normal playback blends quality changes instead of popping.
        public void Evaluate(float seconds)
        {
            if(!_configured)return;
            _quality=LowPreset?0:1;Apply(seconds);
        }
        private void Apply(float seconds)
        {
            _clock=seconds;var wind=_config.battlefieldWind;
            var direction=wind.windDirection.sqrMagnitude>.001f?wind.windDirection.normalized:Vector2.right;
            Shader.SetGlobalVector("_ArenaWind",new Vector4(direction.x,direction.y,wind.windStrength*_config.windStrength*Mathf.Lerp(_config.lowWindMultiplier,1,_quality),wind.windSpeed));
            Shader.SetGlobalVector("_ArenaWindNoise",new Vector4(2*Mathf.PI/_config.gustSeconds,wind.windNoiseScale,wind.branchBend,wind.grassBend));
            Shader.SetGlobalFloat("_ArenaWindTime",seconds);
            _water.SetVector("_Flow",new Vector4(seconds*_config.waterSpeed,seconds*_config.waterSecondSpeed,_quality*_config.waterSecondBlend,0));
            _water.SetVector("_SecondDirection",_config.waterSecondDirection);
            _bamboo.SetVector("_Motion",new Vector4(seconds,_config.bambooAmplitude*Mathf.Lerp(_config.lowBambooMultiplier,1,_quality),2*Mathf.PI/_config.bambooSeconds,0));
        }
        private void OnDestroy()
        {
            if(!_configured)return;
            foreach(var item in _originals)if(item.renderer!=null)item.renderer.sharedMaterials=item.materials;
            foreach(var item in _blocks)if(item.Key!=null)item.Key.SetPropertyBlock(item.Value.isEmpty?null:item.Value);
            Shader.SetGlobalVector("_ArenaWind",_previousWind);Shader.SetGlobalVector("_ArenaWindNoise",_previousNoise);Shader.SetGlobalFloat("_ArenaWindTime",_previousClock);
            Destroy(_water);Destroy(_bamboo);
        }
    }
}
