using System;
using System.Collections.Generic;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Arena3Q.Graphics3D
{
    /// <summary>
    /// Realtime skinned 3D rendered through a separate Forward camera into an
    /// alpha target. The composite participates in the existing sprite y-sort.
    /// No simulation coordinates or global pipeline settings are changed.
    /// </summary>
    [DefaultExecutionOrder(80)]
    public sealed class MacAnhRealtimeBattleView:MonoBehaviour,IMacAnhRealtimeView
    {
        [Serializable] public struct ActionClip {public MacAnhAction action;public AnimationClip clip;}
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private ActionClip[] clips=Array.Empty<ActionClip>();
        [SerializeField] private Material compositeMaterial;
        [SerializeField,Min(0)]private int forwardRendererIndex=1;
        [SerializeField,Range(128,1024)]private int captureResolution=256;
        [SerializeField]private int captureMsaa=4;
        [SerializeField,Range(.1f,.95f)]private float groundProjection=.75f;
        [SerializeField,Min(.5f)]private float orthographicSize=1.25f;
        [SerializeField]private float lookHeight=.8f,yawOffset=-90f,yawSign=-1f;
        [SerializeField,Min(0)]private float transitionSeconds=.07f;
        [SerializeField,Min(0)]private float keyIntensity=20f,fillIntensity=5f;
        private static readonly HashSet<int> Slots=new HashSet<int>();
        private HeroUnit _hero;
        private SpriteRenderer _sprite;
        private GameObject _stage,_model;
        private Transform _placement,_fanTip,_leftHand,_cast;
        private Transform[] _bones;
        private Vector3[] _fromPosition,_fromScale;
        private Quaternion[] _fromRotation;
        private Camera _camera;
        private RenderTexture _target;
        private Material _material,_shadowMaterial;
        private Mesh _quad,_shadowQuad;
        private MeshRenderer _renderer,_shadowRenderer;
        private int _slot=-1;
        private bool _ready,_sampled,_dirty,_blending;
        private MacAnhAction _action;
        private float _seconds,_facing,_transitionStart,_opacity=1;
        public RenderTexture LiveTexture=>_target;
        public void Configure(GameObject character,ActionClip[] animations,Material composite,int rendererIndex)
        {characterPrefab=character;clips=animations;compositeMaterial=composite;forwardRendererIndex=rendererIndex;}
        public void ConfigureFacing(float offset,float sign){yawOffset=offset;yawSign=sign;}
        public bool Initialize(HeroUnit hero,SpriteRenderer sprite)
        {
            if(characterPrefab==null||compositeMaterial==null||clips.Length!=11)return false;
            _hero=hero;_sprite=sprite;
            _slot=0;while(Slots.Contains(_slot))_slot++;Slots.Add(_slot);
            _stage=new GameObject("MacAnh realtime capture "+_slot);
            // Behind the battle camera and outside the map, while retaining
            // millimetre precision for thin hair, jewellery and UV derivatives.
            _stage.transform.position=new Vector3(-128+_slot*12,-128,-64);
            _placement=new GameObject("Facing placement").transform;_placement.SetParent(_stage.transform,false);
            _model=Instantiate(characterPrefab,_placement);
            foreach(var animator in _model.GetComponentsInChildren<Animator>())animator.enabled=false;
            foreach(var child in _model.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer=30;
                if(child.name=="Socket_Fan_Tip")_fanTip=child;
                if(child.name=="Socket_Hand_L")_leftHand=child;
                if(child.name=="Socket_Cast")_cast=child;
            }
            foreach(var skin in _model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=true;skin.skinnedMotionVectors=false;}
            _bones=_model.GetComponentsInChildren<Transform>();
            _fromPosition=new Vector3[_bones.Length];_fromScale=new Vector3[_bones.Length];_fromRotation=new Quaternion[_bones.Length];
            var cameraGo=new GameObject("Forward capture",typeof(Camera),typeof(UniversalAdditionalCameraData));
            cameraGo.transform.SetParent(_stage.transform,false);_camera=cameraGo.GetComponent<Camera>();
            _camera.enabled=false;_camera.orthographic=true;_camera.orthographicSize=orthographicSize;
            _camera.clearFlags=CameraClearFlags.SolidColor;_camera.backgroundColor=Color.clear;
            _camera.cullingMask=1<<30;_camera.nearClipPlane=.1f;_camera.farClipPlane=8;
            _camera.allowHDR=false;_camera.allowMSAA=captureMsaa>1;_camera.useOcclusionCulling=false;
            var data=cameraGo.GetComponent<UniversalAdditionalCameraData>();data.SetRenderer(forwardRendererIndex);
            data.renderPostProcessing=false;data.requiresColorTexture=false;data.requiresDepthTexture=false;
            float tilt=Mathf.Asin(groundProjection)*Mathf.Rad2Deg;
            var rotation=Quaternion.Euler(tilt,0,0);
            cameraGo.transform.SetLocalPositionAndRotation(Vector3.up*lookHeight+rotation*new Vector3(0,0,-4),rotation);
            AddLight("Top left soft key",new Vector3(-3,4,-3),keyIntensity);
            AddLight("Soft fill",new Vector3(2,2,-1),fillIntensity);
            _target=new RenderTexture(captureResolution,captureResolution,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB)
            {name="MacAnh realtime "+_slot,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,antiAliasing=captureMsaa==4?4:captureMsaa==2?2:1};
            _target.Create();_camera.targetTexture=_target;
            _material=new Material(compositeMaterial);_material.SetTexture("_MainTex",_target);
            float bottom=-orthographicSize+lookHeight*Mathf.Cos(tilt*Mathf.Deg2Rad);
            _quad=Quad(orthographicSize*2,orthographicSize*2,bottom);
            _renderer=Surface("Realtime 3D image",_quad,_material);
            _shadowMaterial=new Material(compositeMaterial);_shadowMaterial.SetFloat("_Shadow",1);
            _shadowQuad=Quad(.95f,.36f,-.18f);_shadowRenderer=Surface("Soft ground shadow",_shadowQuad,_shadowMaterial);
            _shadowRenderer.sortingOrder=-4990;
            _ready=true;_sprite.enabled=false;return true;
        }
        private void AddLight(string name,Vector3 position,float intensity)
        {
            var go=new GameObject(name,typeof(Light));go.transform.SetParent(_stage.transform,false);go.transform.localPosition=position;
            var light=go.GetComponent<Light>();light.type=LightType.Point;light.range=8;light.intensity=intensity;
            light.color=new Color(1,.97f,.94f);light.cullingMask=1<<30;light.shadows=LightShadows.None;
        }
        private static Mesh Quad(float width,float height,float bottom)
        {
            var mesh=new Mesh{name="MacAnh live composite quad"};
            mesh.vertices=new[]{new Vector3(-width*.5f,bottom,0),new Vector3(width*.5f,bottom,0),
                new Vector3(width*.5f,bottom+height,0),new Vector3(-width*.5f,bottom+height,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();return mesh;
        }
        private MeshRenderer Surface(string name,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
        }
        public void Sample(MacAnhAction action,float seconds,float facingDegrees)
        {
            if(!_ready)return;
            if(_sampled && action==_action && Mathf.Abs(seconds-_seconds)<.00001f && Mathf.Abs(facingDegrees-_facing)<.001f)return;
            var clip=Array.Find(clips,item=>item.action==action).clip;if(clip==null)return;
            if(_sampled && action!=_action)
            {
                for(int i=0;i<_bones.Length;i++)
                {_fromPosition[i]=_bones[i].localPosition;_fromRotation[i]=_bones[i].localRotation;_fromScale[i]=_bones[i].localScale;}
                _transitionStart=_hero.Battle.BattleTime;_blending=transitionSeconds>0;
            }
            _action=action;_seconds=seconds;_facing=facingDegrees;
            clip.SampleAnimation(_model,clip.isLooping?Mathf.Repeat(seconds,clip.length):Mathf.Clamp(seconds,0,clip.length));
            if(_blending)
            {
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((_hero.Battle.BattleTime-_transitionStart)/transitionSeconds));
                for(int i=0;i<_bones.Length;i++)
                {
                    var bone=_bones[i];bone.localPosition=Vector3.Lerp(_fromPosition[i],bone.localPosition,t);
                    bone.localRotation=Quaternion.Slerp(_fromRotation[i],bone.localRotation,t);bone.localScale=Vector3.Lerp(_fromScale[i],bone.localScale,t);
                }
                _blending=t<1;
            }
            _placement.localRotation=Quaternion.Euler(0,yawOffset+yawSign*facingDegrees,0);
            _sampled=true;_dirty=true;
        }
        public void SetOpacity(float opacity)
        {
            _opacity=Mathf.Clamp01(opacity);
            if(_material!=null)_material.SetFloat("_Opacity",_opacity);
            if(_shadowMaterial!=null)_shadowMaterial.SetFloat("_Opacity",_opacity);
        }
        public bool TrySocket(MacAnhSocket socket,out Vector3 position)
        {
            position=default;if(!_ready||!_sampled)return false;
            var bone=socket==MacAnhSocket.FanTip?_fanTip:socket==MacAnhSocket.LeftHand?_leftHand:_cast;
            if(bone==null)return false;
            var uv=_camera.WorldToViewportPoint(bone.position);
            float bottom=_quad.bounds.min.y;
            position=transform.TransformPoint(new Vector3((uv.x-.5f)*orthographicSize*2,bottom+uv.y*orthographicSize*2,0));return true;
        }
        public bool CaptureAfterimage(MacAnhFx effect,int sortingOrder)
        {
            if(!_ready||!_sampled)return false;
            if(_dirty){_camera.Render();_dirty=false;}
            var descriptor=_target.descriptor;descriptor.depthBufferBits=0;descriptor.msaaSamples=1;
            var target=new RenderTexture(descriptor){name="MacAnh captured afterimage"};target.Create();Graphics.Blit(_target,target);
            var mesh=Instantiate(_quad);var material=new Material(compositeMaterial);
            material.SetTexture("_MainTex",target);material.SetColor("_Tint",new Color(.84f,.67f,1,.6f));
            var pose=new GameObject("Captured realtime pose",typeof(MeshFilter),typeof(MeshRenderer));
            pose.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=pose.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.sortingOrder=sortingOrder;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            effect.SetCapturedAfterimage(pose,()=>{target.Release();Destroy(target);Destroy(mesh);Destroy(material);Destroy(pose);});return true;
        }
        private void LateUpdate()
        {
            if(!_ready||_hero==null)return;
            _renderer.sortingOrder=_sprite.sortingOrder;
            _renderer.sortingLayerID=_sprite.sortingLayerID;
            if(_dirty){_camera.Render();_dirty=false;}
        }
        private void OnDestroy()
        {
            if(_slot>=0)Slots.Remove(_slot);
            if(_sprite!=null)_sprite.enabled=true;
            if(_target!=null){_target.Release();Destroy(_target);}
            if(_stage!=null)Destroy(_stage);
            if(_material!=null)Destroy(_material);if(_shadowMaterial!=null)Destroy(_shadowMaterial);
            if(_quad!=null)Destroy(_quad);if(_shadowQuad!=null)Destroy(_shadowQuad);
        }
    }
}
