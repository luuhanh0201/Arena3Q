using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arena3Q.Exploration
{
    /// <summary>Presentation of saved progress. It never awards stars or unlocks gameplay.</summary>
    public sealed class ChapterMapWorld : MonoBehaviour
    {
        private ChapterMapController _map;
        private Material _fog;
        private Transform _screen;
        private readonly Vector4[] _regions = new Vector4[16];
        private readonly List<FadeGroup> _groups = new List<FadeGroup>();
        private float[] _visibility;
        private bool[] _markers;
        private float _clock;
        public bool Revealing { get; private set; }
        public int VisibleCount => _markers == null ? 0 : _markers.Count(x => x);
        public bool MarkersVisible(int stage) => _markers != null && stage >= 0 && stage < _markers.Length && _markers[stage];
        public float Visibility(int stage) => _visibility[stage];

        public void Configure(ChapterMapController map)
        {
            _map=map;
            int count=map.Config.stages.Length, unlocked=map.Session.Rules.UnlockedCount(map.Session.Progress);
            int revealed=Mathf.Clamp(map.Session.Progress.revealedStages,1,unlocked);
            _visibility=new float[count]; _markers=new bool[count];
            for(int i=0;i<count;i++)
            {
                _visibility[i]=i<revealed?1:0; _markers[i]=i<revealed;
                var area=map.Areas[i]; if(area==null)continue;
                bool complete=map.Session.Rules.State(map.Session.Progress,i)==ChapterStageState.Completed;
                bool transitioning=unlocked>revealed && i==revealed-1;
                AddGroup(area.Find("State_Before"),i,!complete||transitioning);
                AddGroup(area.Find("State_After"),i,complete&&!transitioning);
                var always=area.Find("Always");
                if(always!=null)foreach(Transform child in always)
                    if(child.name.StartsWith("NPC_",System.StringComparison.Ordinal))AddGroup(child,i,true);
            }
            BuildFog();
            if(unlocked>revealed) StartCoroutine(Reveal(revealed,unlocked));
        }
        private void AddGroup(Transform root,int stage,bool wanted)
        {
            if(root==null)return;
            var group=new FadeGroup(root.gameObject,stage,wanted); _groups.Add(group);
            group.Set(wanted&&_markers[stage]?1:0);
        }
        private void BuildFog()
        {
            var motion=_map.Config.motion;
            if(motion.fogMaterial==null)return;
            _fog=new Material(motion.fogMaterial);
            _fog.SetTexture("_CloudTex",motion.fogTexture);
            var camera=_map.View.Camera;
            var data=camera.GetUniversalAdditionalCameraData(); data.requiresColorTexture=true;
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name="Chapter fog";
            Destroy(quad.GetComponent<Collider>()); quad.transform.SetParent(camera.transform,false); _screen=quad.transform;
            var renderer=quad.GetComponent<MeshRenderer>(); renderer.sharedMaterial=_fog; renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            RenderPipelineManager.beginCameraRendering+=BeforeCamera;
        }
        private void BeforeCamera(ScriptableRenderContext context,Camera camera)
        {
            if(_screen==null || camera!=_map.View.Camera)return;
            _screen.localPosition=new Vector3(0,0,camera.nearClipPlane+1);
            _screen.localScale=new Vector3(camera.orthographicSize*2*camera.aspect,camera.orthographicSize*2,1);
        }
        private IEnumerator Reveal(int first,int unlocked)
        {
            Revealing=true; _map.Busy=true;
            var motion=_map.Config.motion;
            // The first return shows the liberated area before the camera visits its successor.
            for(int stage=first;stage<unlocked;stage++)
            {
                int completed=stage-1;
                var area=_map.Areas[completed];
                foreach(var group in _groups.Where(g=>g.Stage==completed))
                    if(area!=null && group.Root.transform==area.Find("State_Before"))group.Wanted=false;
                    else if(area!=null && group.Root.transform==area.Find("State_After"))group.Wanted=true;
                yield return BlendGroups(completed,motion.environmentFadeSeconds);
                var from=_map.View.Focus; var to=_map.Config.stages[stage].center;
                for(float elapsed=0;elapsed<motion.cameraRevealSeconds;elapsed+=Time.unscaledDeltaTime)
                { _map.View.FocusOn(Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,elapsed/motion.cameraRevealSeconds))); yield return null; }
                _map.View.FocusOn(to);
                for(float elapsed=0;elapsed<motion.fogRevealSeconds;elapsed+=Time.unscaledDeltaTime)
                { _visibility[stage]=Mathf.SmoothStep(0,1,elapsed/motion.fogRevealSeconds); yield return null; }
                _visibility[stage]=1;
                yield return BlendGroups(stage,motion.environmentFadeSeconds);
                _markers[stage]=true;
                _map.Session.Progress.revealedStages=stage+1; _map.Session.Save();
            }
            Revealing=false; _map.Busy=false; _map.Hud.Refresh();
        }
        private IEnumerator BlendGroups(int stage,float seconds)
        {
            var groups=_groups.Where(g=>g.Stage==stage).ToArray();
            var from=groups.Select(g=>g.Alpha).ToArray();
            for(float elapsed=0;elapsed<seconds;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.SmoothStep(0,1,elapsed/seconds);
                for(int i=0;i<groups.Length;i++)groups[i].Set(Mathf.Lerp(from[i],groups[i].Wanted?1:0,t));
                yield return null;
            }
            foreach(var group in groups)group.Set(group.Wanted?1:0);
        }
        private void LateUpdate()
        {
            if(_fog==null)return;
            var motion=_map.Config.motion; var camera=_map.View.Camera;
            BeforeCamera(default,camera);
            _clock+=Time.unscaledDeltaTime;
            for(int i=0;i<_visibility.Length;i++) { var c=_map.Config.stages[i].center; _regions[i]=new Vector4(c.x,c.z,_visibility[i],0); }
            int rate=DisplayRate(), tier=rate<=motion.frameRates[0]?0:rate<=motion.frameRates[1]?1:2;
            _fog.SetVectorArray("_Regions",_regions); _fog.SetFloat("_Count",_visibility.Length);
            _fog.SetColor("_FogColor",motion.fogColor); _fog.SetFloat("_Boundary",motion.fogBoundaryWidth);
            _fog.SetFloat("_Clock",_clock); _fog.SetFloat("_Drift",motion.fogDrift); _fog.SetFloat("_Scale",motion.fogTextureScale);
            _fog.SetFloat("_Opacity",motion.fogOpacity); _fog.SetFloat("_Darkening",motion.fogDarkening); _fog.SetFloat("_Layers",motion.fogLayers[tier]);
        }
        private static int DisplayRate() => Arena3Q.Data.DisplayMetrics.PreviewFrameRate>0 ? Arena3Q.Data.DisplayMetrics.PreviewFrameRate : Application.targetFrameRate;
        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering-=BeforeCamera;
            foreach(var group in _groups)group.Dispose();
            if(_fog!=null)Destroy(_fog); if(_screen!=null)Destroy(_screen.gameObject);
        }
        private sealed class FadeGroup
        {
            public readonly GameObject Root; public readonly int Stage; public bool Wanted; public float Alpha;
            private readonly Renderer[] _renderers;
            private readonly Material[][] _originals;
            private Material[][] _fading;
            public FadeGroup(GameObject root,int stage,bool wanted)
            { Root=root;Stage=stage;Wanted=wanted;_renderers=root.GetComponentsInChildren<Renderer>(true);_originals=_renderers.Select(r=>r.sharedMaterials).ToArray(); }
            public void Set(float alpha)
            {
                Alpha=alpha;
                if(alpha<=0){Root.SetActive(false);return;}
                Root.SetActive(true);
                if(alpha>=1){for(int i=0;i<_renderers.Length;i++)_renderers[i].sharedMaterials=_originals[i];return;}
                if(_fading==null)
                {
                    _fading=_originals.Select(ms=>ms.Select(m=>m==null?null:new Material(m)).ToArray()).ToArray();
                    foreach(var material in _fading.SelectMany(x=>x))
                    {
                        if(material==null)continue;
                        material.SetFloat("_Surface",1); material.SetFloat("_ZWrite",0);
                        material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue=(int)RenderQueue.Transparent;
                    }
                }
                for(int i=0;i<_renderers.Length;i++)
                {
                    for(int j=0;j<_fading[i].Length;j++)
                    {
                        var m=_fading[i][j];if(m==null)continue;
                        foreach(string key in new[]{"_BaseColor","_Color"})if(m.HasProperty(key))
                        {var color=_originals[i][j].GetColor(key);color.a*=alpha;m.SetColor(key,color);}
                    }
                    _renderers[i].sharedMaterials=_fading[i];
                }
            }
            public void Dispose(){if(_fading!=null)foreach(var material in _fading.SelectMany(x=>x))if(material!=null)Object.Destroy(material);}
        }
    }
}
