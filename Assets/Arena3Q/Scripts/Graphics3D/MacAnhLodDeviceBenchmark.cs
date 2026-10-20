using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Arena3Q.Graphics3D
{
    /// <summary>Isolated APK harness. Each route runs in a fresh Android process.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class MacAnhLodDeviceBenchmark:MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string mode,utc,device,gpu,graphicsApi,unity,workload;
            public int width,height,frames,heroes,troops,enemies,bodyTriangles,fanTriangles,maxVfx;
            public float measuredSeconds,fps,medianMs,p95Ms,loadSeconds,heroScreenHeightFraction;
            public long graphicsDriverBytes,allocatedBytes,reservedBytes,pssKB;
        }
        [SerializeField] private string battleResource="MacAnh/BenchmarkBattle",portraitResource="MacAnh/BenchmarkPortrait";
        private MacAnhLodBenchmarkContent _content;
        private MacAnhPerformance performance;
        private HeroDefinition[] lineup;
        private GameObject portraitPrefab;
        private AnimationClip[] portraitClips;
        [SerializeField] private float warmupSeconds=6,measureSeconds=30;
        private int portraitBodyTriangles,portraitFanTriangles;
        private string _mode="portrait";
        private BattleController _battle;
        private GameObject _portrait;
        private Camera _camera;
        private readonly List<HeroUnit> _heroes=new List<HeroUnit>();
        private readonly HashSet<UnitId> _protected=new HashSet<UnitId>();
        private readonly Result _result=new Result();
        private float _accumulator,_nextUltimate=8;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var intent=activity.Call<AndroidJavaObject>("getIntent"))
                _mode=intent.Call<string>("getStringExtra","macanh_lod_mode")??"portrait";
#endif
            if(_mode!="portrait"&&_mode!="sprite"&&_mode!="realtime")throw new InvalidOperationException("Unknown benchmark mode "+_mode);
            Application.targetFrameRate=120;QualitySettings.vSyncCount=0;QualitySettings.skinWeights=SkinWeights.FourBones;
            Screen.sleepTimeout=SleepTimeout.NeverSleep;
        }
        private IEnumerator Start()
        {
            float start=Time.realtimeSinceStartup;
            _content=Resources.Load<MacAnhLodBenchmarkContent>(_mode=="portrait"?portraitResource:battleResource);
            if(_content==null)throw new InvalidOperationException("Missing benchmark resource for "+_mode);
            performance=_content.performance;lineup=_content.heroes;portraitPrefab=_content.portraitPrefab;portraitClips=_content.clips;
            portraitBodyTriangles=_content.bodyTriangles;portraitFanTriangles=_content.fanTriangles;
            if(performance!=null)performance.SetBattle3DTrial(_mode=="realtime");
            _result.mode=_mode;_result.utc=DateTime.UtcNow.ToString("o");_result.device=SystemInfo.deviceModel;
            _result.gpu=SystemInfo.graphicsDeviceName;_result.graphicsApi=SystemInfo.graphicsDeviceType.ToString();
            _result.unity=Application.unityVersion;_result.width=Screen.width;_result.height=Screen.height;
            if(_mode=="portrait")CreatePortrait();
            else
            {
                yield return SceneManager.LoadSceneAsync("Battle",LoadSceneMode.Additive);yield return null;
                _battle=FindAnyObjectByType<BattleController>();_battle.AutoSimulate=false;
                _battle.StartBattle(_battle.Stages[0],lineup.Select(def=>new LineupEntry(def,15,3)).ToArray());
                for(int i=0;i<5;i++)
                {
                    var decision=_battle.TrySummon(SummonRequest.Quick(i));
                    if(!decision.Accepted)throw new InvalidOperationException("Benchmark summon "+i+": "+decision.Result.Reason);
                    var hero=_battle.GetHero(_battle.State.Cards[i].Hero);_heroes.Add(hero);
                    _battle.MoveSelectedHero(hero.Position+Vector2.right*2);
                }
                _camera=Camera.main;
                if(_camera!=null)_camera.orthographicSize=4.5f;
                _result.workload="Real Battle stage 0, seed 1; five MacAnh heroes Lv15/3 stars, their troops, stage enemies; current Medium VFX; fixed 60 Hz simulation; identical protection/ultimate schedule in both routes.";
            }
            yield return null;yield return new WaitForEndOfFrame();
            _result.loadSeconds=Time.realtimeSinceStartup-start;
            float warmEnd=Time.realtimeSinceStartup+warmupSeconds;
            while(Time.realtimeSinceStartup<warmEnd)yield return null;
            var times=new List<float>(4000);float first=Time.realtimeSinceStartup,previous=first;
            while(Time.realtimeSinceStartup-first<measureSeconds)
            {
                yield return null;float now=Time.realtimeSinceStartup;times.Add((now-previous)*1000);previous=now;
                _result.maxVfx=Math.Max(_result.maxVfx,_battle!=null?MacAnhFxPool.For(_battle).ActiveCount:0);
            }
            _result.measuredSeconds=Time.realtimeSinceStartup-first;times.Sort();_result.frames=times.Count;
            _result.fps=times.Count/_result.measuredSeconds;_result.medianMs=times[times.Count/2];
            _result.p95Ms=times[Math.Min(times.Count-1,(int)Math.Floor(times.Count*.95f))];
            _result.graphicsDriverBytes=Profiler.GetAllocatedMemoryForGraphicsDriver();
            _result.allocatedBytes=Profiler.GetTotalAllocatedMemoryLong();_result.reservedBytes=Profiler.GetTotalReservedMemoryLong();
            _result.pssKB=ReadPss();
            if(_battle!=null)
            {
                _result.heroes=_battle.Registry.Units.Count(u=>u is HeroUnit);
                _result.troops=_battle.Registry.Units.Count(u=>u is TroopUnit&&!(u is EnemyUnit));
                _result.enemies=_battle.Registry.Units.Count(u=>u is EnemyUnit);
                if(_result.heroes!=5||_result.troops==0||_result.enemies==0||_result.maxVfx==0)
                    throw new InvalidOperationException("Incomplete benchmark workload: "+JsonUtility.ToJson(_result));
            }
            string directory=Application.persistentDataPath;File.WriteAllText(Path.Combine(directory,"MacAnhLOD_"+_mode+".json"),JsonUtility.ToJson(_result,true));
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"MacAnhLOD_"+_mode+".png"));
            Debug.Log("MACANH_LOD_BENCHMARK_COMPLETE "+JsonUtility.ToJson(_result));
        }
        private void CreatePortrait()
        {
            _portrait=Instantiate(portraitPrefab);foreach(var animator in _portrait.GetComponentsInChildren<Animator>())animator.enabled=false;
            var cameraGo=new GameObject("Portrait benchmark camera",typeof(Camera),typeof(UniversalAdditionalCameraData));
            _camera=cameraGo.GetComponent<Camera>();_camera.orthographic=true;_camera.orthographicSize=1.1f;
            _camera.transform.position=new Vector3(0,.85f,-4);_camera.clearFlags=CameraClearFlags.SolidColor;
            _camera.backgroundColor=new Color(.12f,.13f,.16f);_camera.nearClipPlane=.1f;_camera.farClipPlane=10;
            cameraGo.GetComponent<UniversalAdditionalCameraData>().SetRenderer(1);
            foreach(var p in new[]{new Vector3(-3,4,-3),new Vector3(2,2,-1)})
            {var go=new GameObject("Portrait soft light",typeof(Light));var light=go.GetComponent<Light>();go.transform.position=p;light.type=LightType.Point;light.range=10;light.intensity=p.x<0?20:5;}
            var bounds=_portrait.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            _result.heroScreenHeightFraction=bounds.size.y/(_camera.orthographicSize*2);
            _result.bodyTriangles=portraitBodyTriangles;_result.fanTriangles=portraitFanTriangles;_result.heroes=1;
            _result.workload="One Portrait LOD skinned body and rigid fan, Idle animation, no VFX, no battle, fixed camera; target 70–80% screen height.";
            if(_result.heroScreenHeightFraction<.7f||_result.heroScreenHeightFraction>.8f)throw new InvalidOperationException("Portrait framing outside 70–80%: "+_result.heroScreenHeightFraction);
        }
        private void Update()
        {
            if(_portrait!=null)
            {var clip=portraitClips.First(c=>c.name=="Idle");clip.SampleAnimation(_portrait,Mathf.Repeat(Time.realtimeSinceStartup,clip.length));}
            if(_battle==null)return;
            _accumulator+=Time.unscaledDeltaTime;
            while(_accumulator>=1f/60)
            {
                foreach(var unit in _battle.Registry.Units)
                    if(_protected.Add(unit.Id))unit.ApplyStatus(StatusKind.Shield,10000000,120);
                _battle.Simulate(1f/60);_accumulator-=1f/60;
                if(_battle.BattleTime>=_nextUltimate)
                {
                    foreach(var hero in _heroes)
                    {hero.Skills.Rage.Add(1000);hero.Skills.TryUltimate(null);}
                    _nextUltimate+=8;
                }
            }
        }
        private static long ReadPss()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var debug=new AndroidJavaClass("android.os.Debug"))
            using(var info=new AndroidJavaObject("android.os.Debug$MemoryInfo"))
            {debug.CallStatic("getMemoryInfo",info);return info.Call<int>("getTotalPss");}
#else
            return -1;
#endif
        }
    }
}
