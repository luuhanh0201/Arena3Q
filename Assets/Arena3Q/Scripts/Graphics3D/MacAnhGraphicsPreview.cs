using System;
using System.Collections.Generic;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arena3Q.Graphics3D
{
    public sealed class MacAnhGraphicsPreview : MonoBehaviour
    {
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private GameObject cyclonePrefab;
        [SerializeField] private GameObject[] skillPrefabs=new GameObject[11];
        [SerializeField] private GameObject preparePrefab,arrayPreparePrefab,ultimatePreparePrefab,afterimagePrefab,impactPrefab;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private UniversalRenderPipelineAsset previewPipeline;
        private RenderPipelineAsset _previousPipeline;
        private GameObject _character;
        private Animator _animator;
        private readonly List<MacAnhArcaneEffect> _effects=new List<MacAnhArcaneEffect>();
        private readonly Dictionary<string,List<MacAnhArcaneEffect>> _pool=new Dictionary<string,List<MacAnhArcaneEffect>>();
        private readonly HashSet<string> _marks=new HashSet<string>();
        private sealed class Flight {public MacAnhArcaneEffect effect;public Vector3 velocity;}
        private readonly List<Flight> _flights=new List<Flight>();
        private MacAnhArcaneEffect _charge;
        private Transform _fanTip,_placement;
        private MacAnhAction _action=MacAnhAction.Skill02;
        private MacAnhFxQuality _quality=MacAnhFxQuality.High;
        private float _clock,_speed=1,_azimuth=20,_elevation=13,_distance=5.7f;
        private bool _paused,_orbit,_characterView,_loop=true,_showUI=true,_returningToIdle;
        private string _lastEvent="";
        private float _fps,_fpsClock; private int _fpsFrames;
        private int _oldSleep,_oldCap,_oldVsync;
        private SkinWeights _oldSkinWeights;
        private bool _initialized;
        private Vector3 _lookAt=new Vector3(.45f,1,0);
        public GameObject Character => _character;
        public Camera PreviewCamera => previewCamera;
        public MacAnhFxQuality Quality => _quality;
        public MacAnhAction Action => _action;
        public float PreviewTime => _clock;
        public float Fps => _fps;
        public void Configure(GameObject character,GameObject cyclone,Camera cam,UniversalRenderPipelineAsset pipeline,GameObject[] skills)
        {characterPrefab=character;cyclonePrefab=cyclone;previewCamera=cam;previewPipeline=pipeline;skillPrefabs=skills;}
        public void ConfigureAux(GameObject prepare,GameObject arrayPrepare,GameObject ultimatePrepare,GameObject afterimage,GameObject impact)
        {preparePrefab=prepare;arrayPreparePrefab=arrayPrepare;ultimatePreparePrefab=ultimatePrepare;afterimagePrefab=afterimage;impactPrefab=impact;}
        private void Awake()
        {
            _previousPipeline=QualitySettings.renderPipeline;
            _oldSkinWeights=QualitySettings.skinWeights;QualitySettings.skinWeights=SkinWeights.Unlimited;
            _oldCap=Application.targetFrameRate;_oldVsync=QualitySettings.vSyncCount;_initialized=true;
            if(previewPipeline!=null) QualitySettings.renderPipeline=previewPipeline;
            previewCamera.rect=new Rect(0,0,.82f,1);
            Application.targetFrameRate=60;
            _oldSleep=Screen.sleepTimeout;Screen.sleepTimeout=SleepTimeout.NeverSleep;
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.orientation=ScreenOrientation.LandscapeLeft;
#endif
            // The FBX contains root rotation curves. Place/facing belong to a
            // separate parent so the Animator cannot reset them every frame.
            var placement=new GameObject("Character placement").transform;placement.SetParent(transform,false);
            _placement=placement;
            placement.localPosition=new Vector3(-1.15f,0,0);placement.localRotation=Quaternion.identity;
            _character=Instantiate(characterPrefab,placement);_character.transform.localPosition=Vector3.zero;
            _animator=_character.GetComponent<Animator>();
            foreach(var renderer in _character.GetComponentsInChildren<SkinnedMeshRenderer>())
            {renderer.skinnedMotionVectors=false;renderer.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;}
            foreach(var t in _character.GetComponentsInChildren<Transform>()) if(t.name=="Socket_Fan_Tip") _fanTip=t;
            var events=_character.GetComponent<MacAnhAnimationEvents>();events.preview=this;
            Play(_action);
        }
        private void Start()
        {
#if MACANH_GRAPHICS_BENCHMARK
#if UNITY_ANDROID && !UNITY_EDITOR
            bool run=false;
            using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var intent=activity.Call<AndroidJavaObject>("getIntent"))
            {
                run=intent.Call<bool>("getBooleanExtra","macanh_benchmark",false);
                if(run)intent.Call("removeExtra","macanh_benchmark");
            }
            if(run)StartCoroutine(GetComponent<MacAnhGraphicsBenchmark>().Run());
#endif
#endif
        }
        private void OnDestroy()
        {
            if(!_initialized)return;
            if(QualitySettings.renderPipeline==previewPipeline) QualitySettings.renderPipeline=_previousPipeline;
            QualitySettings.skinWeights=_oldSkinWeights;
            Screen.sleepTimeout=_oldSleep;Application.targetFrameRate=_oldCap;QualitySettings.vSyncCount=_oldVsync;
        }
        public void Play(MacAnhAction action)
        {
            foreach(var effect in _effects) effect.Stop();
            _effects.Clear();_flights.Clear();_charge=null;_marks.Clear();_clock=0;_lastEvent="";_action=action;_returningToIdle=false;
            _character.SetActive(true);
            _placement.localPosition=new Vector3(-1.15f,0,0);
            foreach(var r in _character.GetComponentsInChildren<Renderer>()) {r.forceRenderingOff=false;r.shadowCastingMode=ShadowCastingMode.On;}
            _animator.Play(action.ToString(),0,0);_animator.Update(0);
            _animator.speed=_paused?0:_speed;
        }
        private MacAnhArcaneEffect Spawn(GameObject prefab,Vector3 position,float seconds,float scale=1)
        {
            if(prefab==null) return null;
            if(!_pool.TryGetValue(prefab.name,out var list)) _pool[prefab.name]=list=new List<MacAnhArcaneEffect>();
            var effect=list.Find(x=>!x.IsPlaying);
            if(effect==null) {effect=Instantiate(prefab,transform).GetComponent<MacAnhArcaneEffect>();list.Add(effect);}
            effect.transform.SetPositionAndRotation(position,Quaternion.identity);effect.transform.localScale=Vector3.one*scale;
            effect.gameObject.SetActive(true);effect.ManualClock=true;effect.Begin(_quality,seconds);effect.SetDepthFading(previewPipeline.supportsCameraDepthTexture);_effects.Add(effect);return effect;
        }
        public void OnAnimationMark(string key,int combo)
        {
            if(_returningToIdle)return;
            if(!_marks.Add(key+combo)) return;
            _lastEvent=key+" @ "+Mathf.RoundToInt(_clock*30)+"f";
            var target=new Vector3(1.05f,0,0);
            if(key=="Prepare")
            {
                var prefab=_action==MacAnhAction.Ultimate?ultimatePreparePrefab:_action==MacAnhAction.Skill01?arrayPreparePrefab:preparePrefab;
                var pos=_action==MacAnhAction.Ultimate||_action==MacAnhAction.Skill01?target:(_fanTip!=null?_fanTip.position:_character.transform.position+Vector3.up);
                _charge=Spawn(prefab,pos,_action==MacAnhAction.Ultimate?1.6f:.65f);return;
            }
            if(key=="Afterimage") {Spawn(afterimagePrefab,_character.transform.position,.4f);return;}
            if(_charge!=null) {_charge.Stop();_charge=null;}
            if(_action==MacAnhAction.Skill02) Spawn(cyclonePrefab,target,3.2f);
            else
            {
                var pos=_action==MacAnhAction.Skill03?_character.transform.position:target;
                bool projectile=_action==MacAnhAction.Attack01||_action==MacAnhAction.Attack02||_action==MacAnhAction.DodgeAttack;
                if(projectile) pos=_character.transform.position+new Vector3(.4f,1,0);
                var fx=Spawn(skillPrefabs[(int)_action],pos, _action==MacAnhAction.Ultimate?3.2f:projectile?1.1f:2.4f,combo==2?1.18f:1);
                if(projectile && fx!=null) _flights.Add(new Flight{effect=fx,velocity=new Vector3(3,0,_action==MacAnhAction.Attack02?(combo-1)*.38f:0)});
            }
        }
        public void InspectCyclone(float seconds,MacAnhFxQuality quality)
        {
            _quality=quality;Play(MacAnhAction.Skill02);_paused=true;_animator.speed=0;
            _animator.Play("Skill02",0,.62f);_animator.Update(0);_clock=1.24f;
            foreach(var fx in _effects) fx.Stop();_effects.Clear();
            var effect=Spawn(cyclonePrefab,new Vector3(1.05f,0,0),3.2f);effect.Seek(seconds);
        }
        public void SetView(float azimuth,bool characterOnly=false)
        {
            _characterView=characterOnly;_azimuth=azimuth;_elevation=characterOnly?8:13;
            _distance=characterOnly?3.35f:5.7f;UpdateCamera();
        }
        public void SetBenchmarkState(MacAnhFxQuality quality,bool showBody=true,bool showEffect=true,bool castShadows=true,bool depth=true)
        {
            _quality=quality;_paused=false;_loop=false;_speed=1;_orbit=false;
            Play(MacAnhAction.Walk);SetView(20);
            SetPreviewDepth(depth);
            foreach(var r in _character.GetComponentsInChildren<Renderer>()) {r.forceRenderingOff=!showBody;r.shadowCastingMode=castShadows?ShadowCastingMode.On:ShadowCastingMode.Off;}
            _character.SetActive(showBody);
            if(showEffect) Spawn(cyclonePrefab,new Vector3(1.05f,0,0),180f);
        }
        public void EndBenchmark() {_paused=false;_loop=true;_speed=1;SetPreviewDepth(true);Play(MacAnhAction.Skill02);}
        private void SetPreviewDepth(bool enabled)
        {
            previewPipeline.supportsCameraDepthTexture=enabled;
            previewCamera.GetComponent<UniversalAdditionalCameraData>().requiresDepthTexture=enabled;
            previewCamera.depthTextureMode=enabled?DepthTextureMode.Depth:DepthTextureMode.None;
        }
        private void UpdateCamera()
        {
            _lookAt=_characterView?_character.transform.position+Vector3.up*.86f:new Vector3(.2f,1.05f,0);
            var rotation=Quaternion.Euler(_elevation,_azimuth,0);
            previewCamera.transform.SetPositionAndRotation(_lookAt+rotation*new Vector3(0,0,-_distance),rotation);
        }
        private void Update()
        {
            _fpsFrames++;_fpsClock+=Time.unscaledDeltaTime;
            if(_fpsClock>=.5f) {_fps=_fpsFrames/_fpsClock;_fpsClock=0;_fpsFrames=0;}
            float dt=_paused?0:Time.deltaTime*_speed;
            _clock+=dt;
            if(!_returningToIdle && dt>0 && _action>=MacAnhAction.Attack01 && _clock>=Duration(_action)-.08f)
            {
                // Blend the authored recovery pose into breathing instead of
                // holding the final frame motionless for the entire VFX tail.
                _returningToIdle=true;_animator.CrossFadeInFixedTime("Idle",.28f,0,0);
            }
            for(int i=_effects.Count-1;i>=0;i--)
            { var effect=_effects[i]; effect.Advance(dt);if(!effect.IsPlaying) _effects.RemoveAt(i); }
            for(int i=_flights.Count-1;i>=0;i--)
            {
                var flight=_flights[i];if(!flight.effect.IsPlaying){_flights.RemoveAt(i);continue;}
                flight.effect.transform.position+=flight.velocity*dt;
                if(flight.effect.transform.position.x>=1.15f) {var hit=flight.effect.transform.position;flight.effect.Stop();_flights.RemoveAt(i);Spawn(impactPrefab,hit,.45f,.45f);}
            }
            if(_action==MacAnhAction.DodgeAttack)
            {
                float away=Mathf.SmoothStep(0,1,Mathf.InverseLerp(8f/30,13f/30,_clock));
                float back=Mathf.SmoothStep(0,1,Mathf.InverseLerp(25f/30,33f/30,_clock));
                _placement.localPosition=new Vector3(-1.15f-.6f*away*(1-back),0,0);
            }
            if(_loop && !_paused && _action>=MacAnhAction.Attack01 && _clock>Duration(_action)+3.5f) Play(_action);
            if(_orbit) _azimuth+=Time.unscaledDeltaTime*18;
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                if(mouse.rightButton.isPressed)
                {var d=mouse.delta.ReadValue();_azimuth+=d.x*.2f;_elevation=Mathf.Clamp(_elevation-d.y*.16f,-5,65);}
                _distance=Mathf.Clamp(_distance-mouse.scroll.ReadValue().y*.002f,2.1f,10);
            }
            UpdateCamera();
        }
        public static float Duration(MacAnhAction a)
        {
            int[] frames={40,36,24,24,24,45,54,60,42,105,33};return frames[(int)a]/30f;
        }
        private void OnGUI()
        {
            if(!_showUI) return;
            float scale=Screen.height/900f;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale;
            GUILayout.BeginArea(new Rect(width-273,18,255,850),GUI.skin.box);
            GUILayout.Label("MẶC ẢNH · 3D / 1,70 m");
            GUILayout.Label("Phong Vũ Khốn Trận · URP");
            foreach(MacAnhAction a in Enum.GetValues(typeof(MacAnhAction)))
                if(GUILayout.Button((_action==a?"▶ ":"")+a,GUILayout.Height(29))) Play(a);
            GUILayout.Space(8);GUILayout.BeginHorizontal();
            foreach(MacAnhFxQuality q in Enum.GetValues(typeof(MacAnhFxQuality)))
                if(GUILayout.Button(q.ToString())) {_quality=q;Play(_action);}
            GUILayout.EndHorizontal();
            if(GUILayout.Button(_paused?"Tiếp tục":"Tạm dừng")) {_paused=!_paused;_animator.speed=_paused?0:_speed;}
            if(GUILayout.Button("Phát lại")) Play(_action);
            _loop=GUILayout.Toggle(_loop,"Lặp kỹ năng");_orbit=GUILayout.Toggle(_orbit,"Camera xoay 360°");
            bool character=GUILayout.Toggle(_characterView,"Xem rõ toàn thân nhân vật");
            if(character!=_characterView) SetView(_azimuth,character);
            GUILayout.Label("Tốc độ "+_speed.ToString("F2"));
            float speed=GUILayout.HorizontalSlider(_speed,.15f,1.5f);
            if(!Mathf.Approximately(speed,_speed)) {_speed=speed;_animator.speed=_paused?0:_speed;}
            GUILayout.Label(_quality+" · "+_clock.ToString("F2")+"s\n"+_lastEvent);
            GUILayout.Label(Screen.width+"×"+Screen.height+" · "+_fps.ToString("F1")+" FPS");
            if(GUILayout.Button("Đo FPS cả 3 mức"))
            {var bench=GetComponent<MacAnhGraphicsBenchmark>();if(!bench.Running) StartCoroutine(bench.Run());}
            GUILayout.Label("Chuột phải: xoay · Cuộn: zoom");
            GUILayout.EndArea();
        }
    }
}
