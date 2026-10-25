using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arena3Q.Gameplay
{
    /// <summary>Runtime batches of the editable prefab renderers. GPU wind and original collision remain intact.</summary>
    [DefaultExecutionOrder(150)]
    public sealed class BattlefieldInstancing : MonoBehaviour
    {
        private const int BatchCapacity=100; // Conservative constant-buffer footprint on mobile GPUs.
        public BattlefieldEnvironment environment;
        public BattleController battle;
        public Transform[] roots;
        [Range(.05f,1f)] public float occludedTreeVisibility=.18f;
        [Min(.05f)] public float occlusionFadeSeconds=.25f;
        public int SubmittedDraws { get; private set; }
        public long SubmittedTriangles { get; private set; }
        private sealed class Source
        {
            public MeshRenderer renderer;
            public Matrix4x4 matrix;
            public Batch[] batches;
            public LODGroup group;
            public int lod;
        }
        private sealed class Batch
        {
            public Mesh mesh;
            public Material material;
            public int submesh,layer,triangles;
            public ShadowCastingMode shadows;
            public readonly List<Matrix4x4> matrices=new List<Matrix4x4>(128);
            public readonly List<float> visibility=new List<float>(128);
            public readonly float[] opacity=new float[BatchCapacity];
            public MaterialPropertyBlock properties;
        }
        private sealed class TreeFade{public Bounds bounds;public float visibility=1f;}
        private readonly List<Source> _sources=new List<Source>();
        private readonly List<Batch> _batches=new List<Batch>();
        private readonly Dictionary<LODGroup,bool> _groups=new Dictionary<LODGroup,bool>();
        private readonly Dictionary<LODGroup,LOD[]> _lods=new Dictionary<LODGroup,LOD[]>();
        private readonly Dictionary<LODGroup,int> _selected=new Dictionary<LODGroup,int>();
        private readonly Dictionary<LODGroup,TreeFade> _fades=new Dictionary<LODGroup,TreeFade>();
        private readonly Ray[] _heroRays=new Ray[5];
        private readonly float[] _heroDistances=new float[5];
        private BattlefieldLegacyBridge _bridge;
        private static readonly int VisibilityId=Shader.PropertyToID("_ArenaVisibility");
        private static readonly Quaternion ToNative=Quaternion.Euler(90f,0,0);
        private readonly Dictionary<(Mesh,Material,int,ShadowCastingMode,int),Batch> _lookup=new Dictionary<(Mesh,Material,int,ShadowCastingMode,int),Batch>();
        private int _frame=-1;
        private bool _ready;

        private void OnEnable()
        {
            if(!Application.isPlaying||!SystemInfo.supportsInstancing||environment==null||roots==null)return;
            _bridge=battle!=null?battle.GetComponent<BattlefieldLegacyBridge>():null;
            foreach(var root in roots)
            {
                if(root==null)continue;
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var filter=renderer.GetComponent<MeshFilter>();
                    if(!renderer.enabled||filter==null||filter.sharedMesh==null)continue;
                    var mesh=filter.sharedMesh;var materials=renderer.sharedMaterials;bool compatible=materials.Length>=mesh.subMeshCount;
                    foreach(var material in materials)if(material==null||!material.enableInstancing||!material.shader.name.StartsWith("Arena3Q/Battlefield/"))compatible=false;
                    if(!compatible)continue;
                    var source=new Source{renderer=renderer,matrix=renderer.localToWorldMatrix,batches=new Batch[mesh.subMeshCount],lod=-1};
                    var group=renderer.GetComponentInParent<LODGroup>();
                    if(group!=null)
                    {
                        if(!_groups.ContainsKey(group))
                        {
                            _groups.Add(group,group.enabled);_lods.Add(group,group.GetLODs());
                            var full=_lods[group][0].renderers;var bounds=full[0].bounds;foreach(var member in full)bounds.Encapsulate(member.bounds);_fades.Add(group,new TreeFade{bounds=bounds});
                        }
                        var lods=_lods[group];for(int l=0;l<lods.Length;l++)foreach(var member in lods[l].renderers)if(member==renderer){source.group=group;source.lod=l;}
                    }
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        var key=(mesh,materials[sub],sub,renderer.shadowCastingMode,renderer.gameObject.layer);
                        if(!_lookup.TryGetValue(key,out var batch))
                        {
                            batch=new Batch{mesh=mesh,material=materials[sub],submesh=sub,shadows=renderer.shadowCastingMode,layer=renderer.gameObject.layer,triangles=(int)(mesh.GetIndexCount(sub)/3),properties=new MaterialPropertyBlock()};
                            _lookup.Add(key,batch);_batches.Add(batch);
                        }
                        source.batches[sub]=batch;
                    }
                    _sources.Add(source);renderer.enabled=false;
                }
            }
            foreach(var pair in _groups)pair.Key.enabled=false;
            _ready=true;RenderPipelineManager.beginCameraRendering+=BeforeCamera;
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering-=BeforeCamera;
            foreach(var source in _sources)if(source.renderer!=null)source.renderer.enabled=true;
            foreach(var pair in _groups)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            _sources.Clear();_groups.Clear();_lods.Clear();_batches.Clear();_lookup.Clear();_selected.Clear();_fades.Clear();_ready=false;_frame=-1;
        }
        private void LateUpdate()=>SubmitForFrame();
        private void BeforeCamera(ScriptableRenderContext context,Camera camera)
        {
            if(camera==environment.battleCamera||camera.cameraType==CameraType.SceneView)SubmitForFrame();
        }
        public void SubmitForFrame()
        {
            if(!_ready||_frame==Time.frameCount||environment.battleCamera==null)return;
            _frame=Time.frameCount;SubmittedDraws=0;SubmittedTriangles=0;
            UpdateOcclusion();
            _selected.Clear();
            foreach(var pair in _groups)
            {
                var group=pair.Key;if(group==null)continue;
                var scale=group.transform.lossyScale;
                float relative=group.size*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z))*QualitySettings.lodBias/(2f*environment.battleCamera.orthographicSize);
                var lods=_lods[group];int selected=lods.Length;
                for(int i=0;i<lods.Length;i++)if(relative>=lods[i].screenRelativeTransitionHeight){selected=i;break;}
                _selected[group]=selected;
            }
            foreach(var batch in _batches){batch.matrices.Clear();batch.visibility.Clear();}
            foreach(var source in _sources)
            {
                if(source.renderer==null||!source.renderer.gameObject.activeInHierarchy)continue;
                if(source.group!=null&&_selected[source.group]!=source.lod)continue;
                float visibility=source.group!=null?_fades[source.group].visibility:1f;
                foreach(var batch in source.batches){batch.matrices.Add(source.matrix);batch.visibility.Add(visibility);}
            }
            foreach(var batch in _batches)
            {
                if(batch.matrices.Count==0)continue;
                var parameters=new RenderParams(batch.material){shadowCastingMode=environment.quality==BattlefieldGraphics.Low?ShadowCastingMode.Off:batch.shadows,receiveShadows=true,layer=batch.layer,lightProbeUsage=LightProbeUsage.Off,matProps=batch.properties};
                // Stay below the API's 511 limit and smaller mobile uniform-buffer budgets.
                for(int first=0;first<batch.matrices.Count;first+=BatchCapacity)
                {
                    int count=Mathf.Min(BatchCapacity,batch.matrices.Count-first);
                    for(int i=0;i<count;i++)batch.opacity[i]=batch.visibility[first+i];batch.properties.SetFloatArray(VisibilityId,batch.opacity);
                    Graphics.RenderMeshInstanced(parameters,batch.mesh,batch.submesh,batch.matrices,count,first);
                    SubmittedDraws++;SubmittedTriangles+=(long)batch.triangles*count;
                }
            }
        }
        public float TreeVisibility(LODGroup group)=>_fades.TryGetValue(group,out var fade)?fade.visibility:1f;
        private void UpdateOcclusion()
        {
            int count=0;
            var origin=environment.battleCamera.transform.position;if(_bridge!=null&&!_bridge.RenderingYUp)origin=ToNative*origin;
            var forward=environment.battleCamera.transform.forward;if(_bridge!=null&&!_bridge.RenderingYUp)forward=ToNative*forward;
            if(battle!=null)foreach(var unit in battle.Registry.Units)
                if(unit is HeroUnit&&unit.IsAlive&&count<_heroRays.Length)
                {
                    var target=BattlefieldLegacyBridge.ToEnvironment(unit.Position,1f);
                    var delta=target-origin;
                    _heroRays[count]=environment.battleCamera.orthographic?new Ray(target-forward*200f,forward):new Ray(origin,delta.normalized);
                    _heroDistances[count]=environment.battleCamera.orthographic?200f:delta.magnitude;count++;
                }
            foreach(var pair in _fades)
            {
                var bounds=pair.Value.bounds;bounds.Expand(.25f);
                bool hides=false;
                for(int hero=0;hero<count;hero++)if(bounds.IntersectRay(_heroRays[hero],out float distance)&&distance<_heroDistances[hero]-.3f){hides=true;break;}
                pair.Value.visibility=Mathf.MoveTowards(pair.Value.visibility,hides?occludedTreeVisibility:1f,Time.unscaledDeltaTime/occlusionFadeSeconds);
            }
        }
    }
}
