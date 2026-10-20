using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using Arena3Q.Graphics3D;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arena3Q.Editor
{
    public static class MacAnhGraphicsBuilder
    {
        public const string Root="Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D";
        public const string ScenePath="Assets/Arena3Q/Scenes/MacAnhGraphicsPreview.unity";
        public const string CyclonePath=Root+"/FX_MacAnh_Cyclone3D.prefab";
        [Serializable] private class Bridge { public SourceMaterial[] materials; }
        [Serializable] private class SourceMaterial
        {public string name,baseMap;public float[] color,emission;public float metallic,roughness,strength;public bool sourceORM;}
        static Color Purple=>Hex("9657EF");static Color Gold=>Hex("E6C47C");
        static Color Hex(string rgb) {ColorUtility.TryParseHtmlString("#"+rgb,out var c);return c;}
        static T Save<T>(T asset,string path) where T:UnityEngine.Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing==null) {AssetDatabase.CreateAsset(asset,path);return asset;}
            EditorUtility.CopySerialized(asset,existing);UnityEngine.Object.DestroyImmediate(asset);return existing;
        }
        [MenuItem("Arena3Q/Mac Anh/Graphics/Build 3D Preview")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building graphics assets.");
            Directory.CreateDirectory(Root+"/Meshes");Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Textures");
            AssetDatabase.Refresh();
            BuildCharacter();AddPreparationEvents();BuildCyclone();MacAnhSkillGraphicsBuilder.Build();BuildScene();AssetDatabase.SaveAssets();
            Debug.Log("[MacAnh Graphics] Original 3D character + seven-layer cyclone preview built. Source blend and existing skill prefabs were preserved.");
        }
        private static void BuildCharacter()
        {
            string modelPath=Root+"/Source/MacAnh_OriginalRig.fbx";
            var mi=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            if(mi==null) throw new InvalidOperationException("Run export_macanh_3d.py first.");
            mi.animationType=ModelImporterAnimationType.Generic;mi.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            // Garment seams need the complete continuous weight blend. A runtime
            // Bone4 override silently discarded those influences after import.
            mi.skinWeights=ModelImporterSkinWeights.Custom;mi.maxBonesPerVertex=16;mi.minBoneWeight=0;
            mi.importAnimation=false;mi.SaveAndReimport();
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var bridge=JsonUtility.FromJson<Bridge>(File.ReadAllText(Root+"/Source/MaterialBridge.json"));
            var materialMap=new Dictionary<string,Material>();
            foreach(var src in bridge.materials)
            {
                var mat=new Material(Shader.Find("Arena3Q/MacAnh/Original PBR")){name=src.name+"_URP"};
                // Feed the recorded linear RGB directly to the custom PBR shader.
                var c=new Color(src.color[0],src.color[1],src.color[2],src.color[3]);
                mat.SetColor("_BaseColor",string.IsNullOrEmpty(src.baseMap)?c:Color.white);
                mat.SetFloat("_Metallic",src.metallic);mat.SetFloat("_Smoothness",1-src.roughness);
                if(!string.IsNullOrEmpty(src.baseMap)) mat.SetTexture("_BaseMap",LoadMap(src.baseMap));
                if(src.sourceORM)
                {mat.SetFloat("_UseORM",1);mat.SetTexture("_ORMMap",LoadMap("Image_1"));mat.SetTexture("_BumpMap",LoadMap("Image_2"));}
                if(src.strength>0) mat.SetColor("_EmissionColor",new Color(src.emission[0],src.emission[1],src.emission[2])*src.strength);
                materialMap[src.name]=Save(mat,Root+"/Materials/"+mat.name+".mat");
            }
            // Explicit material remap is by the FBX's original material names.
            foreach(var id in mi.GetExternalObjectMap().Keys.ToArray()) if(id.type==typeof(Material)) mi.RemoveRemap(id);
            foreach(var pair in materialMap) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
            mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;mi.SaveAndReimport();
            var animPath=Root+"/Source/MacAnh_OriginalActions.fbx";
            var ai=(ModelImporter)AssetImporter.GetAtPath(animPath);
            ai.animationType=ModelImporterAnimationType.Generic;ai.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
            ai.sourceAvatar=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().First();
            ai.importAnimation=true;
            var clips=ai.defaultClipAnimations;
            foreach(var clip in clips)
            {
                var name=clip.name;int index=name.LastIndexOf("MacAnh_",StringComparison.Ordinal);
                if(index>=0) name=name.Substring(index+7);
                clip.name=name;clip.loopTime=name=="Idle"||name=="Walk"||name=="Run";
                var events=new List<AnimationEvent>();
                // ModelImporter event time is normalized, unlike AnimationClip's
                // event time in seconds. Keep the source frame / clip frame span.
                Action<string,int,int> add=(key,frame,combo)=>events.Add(new AnimationEvent {functionName="OnMacAnhAnimationEvent",stringParameter=key,intParameter=combo,time=frame/Mathf.Max(1,clip.lastFrame-clip.firstFrame)});
                switch(name)
                {
                    case "Attack01":add("SpawnWindSlash",14,0);break;
                    case "Attack02":add("SpawnTripleFeather",15,0);add("SpawnTripleFeather",24,1);add("SpawnTripleFeather",33,2);break;
                    case "Skill01":add("SpawnMagicArray",33,0);break;
                    case "Skill02":add("SpawnCyclone",30,0);break;
                    case "Skill03":add("ApplyShield",27,0);break;
                    case "Ultimate":add("OnUltimateHit",75,0);break;
                    case "DodgeAttack":add("SpawnCounterSlash",24,0);break;
                }
                clip.events=events.ToArray();
            }
            ai.clipAnimations=clips;ai.SaveAndReimport();
            var controllerPath=Root+"/MacAnh3D.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm=controller.layers[0].stateMachine;
            foreach(var state in sm.states) sm.RemoveState(state.state);
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(animPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")))
            {var state=sm.AddState(clip.name);state.motion=clip;if(clip.name=="Idle") sm.defaultState=state;}
            var go=(GameObject)PrefabUtility.InstantiatePrefab(original);
            go.transform.localRotation=Quaternion.Euler(0,180,0);
            go.name="MacAnh_Character3D";var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=controller;
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            go.AddComponent<MacAnhAnimationEvents>();
            foreach(var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>()) {renderer.updateWhenOffscreen=false;renderer.quality=SkinQuality.Auto;}
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/MacAnh_Character3D.prefab");UnityEngine.Object.DestroyImmediate(go);
        }
        static Texture2D LoadMap(string name)
        {
            var jpg=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Source/"+name+".jpg");
            return jpg!=null?jpg:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Source/"+name+".png");
        }
        static float Line(Vector2 p,Vector2 a,Vector2 b)
        {var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));}
        static float Smooth(float a,float b,float t) => Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        static Texture2D Texture(string name,int size,Func<float,float,Color> pixel,bool repeat=false)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name=name,wrapMode=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp};
            var pixels=new Color[size*size];for(int y=0;y<size;y++) for(int x=0;x<size;x++) pixels[y*size+x]=pixel((x+.5f)/size,(y+.5f)/size);
            tex.SetPixels(pixels);tex.Apply();return Save(tex,Root+"/Textures/"+name+".asset");
        }
        static Material Energy(string name,Color color,float emission,float opacity,int mode,Texture2D noise,Texture2D shape=null)
        {
            var mat=new Material(Shader.Find("Arena3Q/MacAnh/Arcane Flow 3D")){name=name};mat.SetColor("_Color",color.linear);
            mat.SetFloat("_Emission",emission);mat.SetFloat("_Opacity",opacity);mat.SetFloat("_Mode",mode);
            mat.SetTexture("_NoiseTex",noise);
            var cloud=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/CycloneWindDensity.png");
            if(cloud!=null) mat.SetTexture("_CloudTex",cloud);
            if(shape!=null) mat.SetTexture("_MainTex",shape);
            return Save(mat,Root+"/Materials/"+name+".mat");
        }
        public static void BuildCyclone()
        {
            var noise=Texture("ArcaneNoise",256,(u,v)=>{
                float a=u*Mathf.PI*2,b=v*Mathf.PI*2;
                float n=.5f+.19f*Mathf.Sin(a*3+b*2+Mathf.Sin(b*5))+.16f*Mathf.Sin(a*7-b*5)+.1f*Mathf.Sin(a*13+b*11);
                return new Color(n,.5f+.3f*Mathf.Sin(a*5-b*7+Mathf.Sin(a*3)),n,1);},true);
            var circle=Texture("RunicCircle",1024,(u,v)=>{
                Vector2 p=new Vector2(u-.5f,v-.5f)*2;float r=p.magnitude,a=Mathf.Atan2(p.y,p.x);
                float d=Mathf.Min(Mathf.Abs(r-.94f),Mathf.Min(Mathf.Abs(r-.89f),Mathf.Min(Mathf.Abs(r-.62f),Mathf.Abs(r-.58f))));
                float sector=Mathf.Repeat(a+Mathf.PI*2,Mathf.PI/4)-Mathf.PI/8;
                var q=new Vector2(Mathf.Sin(sector)*r,Mathf.Cos(sector)*r);
                int trigram=Mathf.FloorToInt(Mathf.Repeat(a+Mathf.PI*2,Mathf.PI*2)/(Mathf.PI/4));
                for(int j=0;j<3;j++)
                {
                    float y=.7f+j*.043f;
                    if((trigram&(1<<j))!=0) {d=Mathf.Min(d,Line(q,new Vector2(-.085f,y),new Vector2(-.018f,y)));d=Mathf.Min(d,Line(q,new Vector2(.018f,y),new Vector2(.085f,y)));}
                    else d=Mathf.Min(d,Line(q,new Vector2(-.085f,y),new Vector2(.085f,y)));
                }
                // Octagonal inner formation, no pentagram.
                float oct=Mathf.Cos(Mathf.Repeat(a+Mathf.PI/8,Mathf.PI/4)-Mathf.PI/8)*r;
                d=Mathf.Min(d,Mathf.Abs(oct-.46f));
                float alpha=(1-Smooth(.002f,.007f,d))*.85f+(1-Smooth(.006f,.026f,d))*.15f;
                return new Color(1,1,1,alpha);});
            var rune=Texture("FloatingRune",128,(u,v)=>{
                int variant=2;
                Vector2 p=new Vector2(u,v);float d=Line(p,new Vector2(.48f,.15f),new Vector2(.52f,.84f));
                d=Mathf.Min(d,Line(p,new Vector2(.24f,.78f),new Vector2(.5f,.92f)));
                d=Mathf.Min(d,Line(p,new Vector2(.5f,.92f),new Vector2(.75f,.75f)));
                for(int j=0;j<3;j++)
                {
                    float y=.33f+j*.17f;
                    d=Mathf.Min(d,Line(p,new Vector2(.23f,y+.05f),new Vector2(.72f,y)));
                    d=Mathf.Min(d,Line(p,new Vector2(.24f,y+.05f),new Vector2(.2f+(variant%2)*.12f,y-.08f)));
                }
                d=Mathf.Min(d,Line(p,new Vector2(.5f,.31f),new Vector2(.27f,.13f)));
                d=Mathf.Min(d,Line(p,new Vector2(.5f,.31f),new Vector2(.75f,.16f)));
                return new Color(1,1,1,1-Smooth(.014f,.04f,d));});
            var mote=Texture("Mote",64,(u,v)=>new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-new Vector2(u-.5f,v-.5f).magnitude*2),2)));
            MacAnhEnergyAssets.Build();
            var wispsShader=Shader.Find("Arena3Q/MacAnh/Cyclone Wisps");
            if(wispsShader==null)throw new InvalidOperationException("Import CycloneWisps.shader first.");
            var wispMat=new Material(wispsShader){name="CycloneShortWisps"};
            wispMat.SetColor("_Color",Hex("D6B9FF").linear);wispMat.SetFloat("_Emission",2.5f);wispMat.SetFloat("_Opacity",.72f);
            wispMat.SetTexture("_NoiseTex",MacAnhVolumeTexture.BuildFlow());wispMat.SetTexture("_CloudTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/CycloneWindDensity.png"));
            var wisps=Save(wispMat,Root+"/Materials/CycloneShortWisps.mat");
            var runeMat=new Material(wispsShader){name="CycloneInteriorRunes",renderQueue=2985};
            runeMat.SetColor("_Color",Gold.linear);runeMat.SetFloat("_Emission",1.4f);runeMat.SetFloat("_Opacity",.95f);runeMat.SetFloat("_Rune",1);runeMat.SetTexture("_MainTex",rune);
            var gold=Save(runeMat,Root+"/Materials/CycloneInteriorRunes.mat");
            var seal=Energy("CircleGold",Gold,1.25f,.5f,2,noise,circle);
            var sealPurple=Energy("CircleViolet",Purple,1.1f,.28f,2,noise,circle);
            var dustShader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/ArcaneParticles.shadergraph");
            if(dustShader==null) throw new InvalidOperationException("Import ArcaneParticles.shadergraph first.");
            // This material is shared with other skills. Reuse it without resetting
            // its URP blend state during a cyclone-only rebuild.
            var dust=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RisingMotesGraph.mat");
            if(dust==null){var dustMat=new Material(dustShader){name="RisingMotesGraph"};dustMat.SetTexture("_BaseMap",mote);dust=Save(dustMat,Root+"/Materials/RisingMotesGraph.mat");}
            var battleDust=Energy("RisingMotesBattle",Hex("D6B9FF"),2.2f,.9f,2,noise,mote);battleDust.SetFloat("_SoftDistance",0);
            var featherMat=Energy("MagicFeathers",Gold,2.0f,.8f,2,noise);featherMat.SetFloat("_SoftDistance",0);
            var cloudTex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/CycloneWindDensity.png");
            var smoke=Energy("RollingWind",Hex("552C91"),1.4f,.48f,2,noise,cloudTex);
            smoke.SetFloat("_SoftShape",1);
            var go=new GameObject("FX_MacAnh_Cyclone3D");var fx=go.AddComponent<MacAnhArcaneEffect>();
            var tiers=new MacAnhArcaneEffect.Tier[3];
            var volumeNoise=MacAnhVolumeTexture.BuildCloud();
            for(int quality=0;quality<3;quality++)
            {
                var root=new GameObject(((MacAnhFxQuality)quality).ToString());root.transform.SetParent(go.transform,false);
                var parts=new List<MacAnhArcaneEffect.Part>();
                Action<string,Mesh,Material,MacAnhArcaneEffect.PartKind,float,Vector3> add=(name,mesh,mat,kind,spin,pos)=>{
                    mesh=Save(mesh,Root+"/Meshes/"+mesh.name+".asset");
                    var child=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(root.transform,false);child.transform.localPosition=pos;
                    child.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;
                    renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    parts.Add(new MacAnhArcaneEffect.Part{transform=child.transform,kind=kind,spin=spin,scale=Vector3.one});};
                int segments=quality==0?20:quality==1?28:36;
                string suffix="_"+quality;
                add("01 Magic circle / gold",MacAnhArcaneGeometry.Circle("Seal"+suffix,1.32f),seal,MacAnhArcaneEffect.PartKind.Circle,18,new Vector3(0,.022f,0));
                add("01 Counter rotating violet",MacAnhArcaneGeometry.Circle("SealInner"+suffix,1.16f),sealPurple,MacAnhArcaneEffect.PartKind.Circle,-25,new Vector3(0,.028f,0));
                var volumeMat=new Material(Shader.Find("Arena3Q/MacAnh/Cyclone Volume")){name="CycloneVolume_"+quality};
                volumeMat.SetTexture("_VolumeTex",volumeNoise);volumeMat.SetColor("_Color",Purple.linear);volumeMat.SetFloat("_Opacity",2.1f);volumeMat.SetFloat("_Emission",1.9f);
                volumeMat.SetFloat("_Steps",quality==0?10:quality==1?14:20);
                volumeMat=Save(volumeMat,Root+"/Materials/"+volumeMat.name+".mat");
                add("02 Bounded volumetric wind",MacAnhArcaneGeometry.VolumeBounds("Volume"+suffix),volumeMat,MacAnhArcaneEffect.PartKind.Body,22,Vector3.zero);
                add("03 Short drifting wisps",MacAnhArcaneGeometry.CycloneWisps("ShortWisps"+suffix,segments),wisps,MacAnhArcaneEffect.PartKind.Ribbon,55,Vector3.zero);
                add("04 Interior drifting runes",MacAnhArcaneGeometry.CycloneRunes("InteriorRunes"+suffix,quality==0?8:quality==1?12:16),gold,MacAnhArcaneEffect.PartKind.Runes,0,Vector3.zero);
                add("06 Ground shockwave",MacAnhArcaneGeometry.Circle("Shockwave"+suffix,1.1f),sealPurple,MacAnhArcaneEffect.PartKind.Impact,0,new Vector3(0,.038f,0));
                var featherMesh=Save(MacAnhArcaneGeometry.Feather("Feather"+suffix),Root+"/Meshes/Feather"+suffix+".asset");
                tiers[quality]=new MacAnhArcaneEffect.Tier{root=root,parts=parts.ToArray(),previewParticleMaterial=dust,battleParticleMaterial=battleDust,
                    particles=Particles(root.transform,"05 Rising motes",dust,quality==0?12:quality==1?22:32,null,false),
                    feathers=Particles(root.transform,"05 Orbiting feathers",featherMat,quality==0?3:quality==1?5:7,featherMesh,false),
                    smoke=Particles(root.transform,"02 Rolling wind volume",smoke,quality==0?3:quality==1?5:8,null,false,true),
                    burst=Particles(root.transform,"06 Spawn impact",dust,quality==0?12:quality==1?24:40,null,true)};
                root.SetActive(false);
            }
            fx.Configure(tiers);fx.ConfigureSmokeDissolve();PrefabUtility.SaveAsPrefabAsset(go,CyclonePath);UnityEngine.Object.DestroyImmediate(go);AssetDatabase.SaveAssets();
        }
        internal static ParticleSystem Particles(Transform parent,string name,Material material,int rate,Mesh mesh,bool burst,bool volume=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.up*.12f;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=!burst;main.duration=burst?.2f:5;
            main.simulationSpace=ParticleSystemSimulationSpace.Local;main.maxParticles=Mathf.Max(1,rate*3);
            main.startLifetime=new ParticleSystem.MinMaxCurve(burst?.3f:.65f,burst?.65f:1.5f);
            main.startSpeed=burst?2.2f:0;main.startSize=mesh!=null?new ParticleSystem.MinMaxCurve(.45f,1.15f):new ParticleSystem.MinMaxCurve(.015f,.055f);
            if(volume) {main.startSize=new ParticleSystem.MinMaxCurve(.35f,.72f);main.startLifetime=new ParticleSystem.MinMaxCurve(.9f,1.3f);}
            main.startColor=volume?Color.white:mesh!=null?Gold:Hex("D6B9FF")*2.2f;main.gravityModifier=burst?.1f:0;
            var emission=ps.emission;emission.rateOverTime=burst?0:rate;
            if(burst) emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)rate)});
            var shape=ps.shape;shape.shapeType=burst?ParticleSystemShapeType.Sphere:ParticleSystemShapeType.Circle;
            shape.radius=burst?.3f:.36f;shape.rotation=new Vector3(90,0,0);shape.radiusThickness=1;
            if(!burst)
            {
                var vel=ps.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.Local;
                vel.x=new ParticleSystem.MinMaxCurve(0,0);vel.z=new ParticleSystem.MinMaxCurve(0,0);
                vel.y=new ParticleSystem.MinMaxCurve(.9f,1.7f);
                vel.orbitalX=new ParticleSystem.MinMaxCurve(0,0);vel.orbitalZ=new ParticleSystem.MinMaxCurve(0,0);
                vel.orbitalY=new ParticleSystem.MinMaxCurve(2.5f,4.5f);vel.radial=.35f;
            }
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,0),new Keyframe(.15f,1),new Keyframe(.7f,.8f),new Keyframe(1,0)));
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Hex("9657EF"),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.1f),new GradientAlphaKey(0,1)});color.color=gradient;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            if(mesh!=null) {renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=mesh;main.startRotation3D=true;main.startRotationY=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);}
            ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(rate*17+11);return ps;
        }
        private static void BuildScene()
        {
            var rdPath=Root+"/MacAnhPreviewRenderer.asset";
            var rd=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rdPath);
            if(rd==null) {rd=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(rd,rdPath);}
            var pipelinePath=Root+"/MacAnhPreviewURP.asset";
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if(pipeline==null) {pipeline=UniversalRenderPipelineAsset.Create(rd);AssetDatabase.CreateAsset(pipeline,pipelinePath);}
            pipeline.supportsCameraDepthTexture=true;pipeline.msaaSampleCount=4;pipeline.renderScale=1;
            pipeline.shadowDistance=15;pipeline.supportsHDR=true;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.5f,.52f,.57f);RenderSettings.ambientEquatorColor=new Color(.37f,.39f,.43f);RenderSettings.ambientGroundColor=new Color(.18f,.19f,.22f);
            var cameraGO=new GameObject("Preview Camera",typeof(Camera),typeof(UniversalAdditionalCameraData));cameraGO.tag="MainCamera";
            var camera=cameraGO.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.19f,.21f,.25f);
            camera.backgroundColor=new Color(.388f,.416f,.467f);
            camera.fieldOfView=35;camera.nearClipPlane=.08f;camera.farClipPlane=50;camera.allowHDR=true;camera.allowMSAA=true;
            var camData=cameraGO.GetComponent<UniversalAdditionalCameraData>();camData.renderPostProcessing=true;camData.requiresDepthTexture=true;
            var key=new GameObject("Neutral key light",typeof(Light));var light=key.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.color=Color.white;light.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(45,-35,0);
            var fill=new GameObject("Soft rim",typeof(Light));var f=fill.GetComponent<Light>();f.type=LightType.Directional;f.color=new Color(.86f,.91f,1);f.intensity=.45f;fill.transform.rotation=Quaternion.Euler(25,145,0);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Neutral matte floor";ground.transform.localScale=Vector3.one*20;ground.transform.position=Vector3.down*.006f;
            var floor=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="NeutralFloor"};floor.SetColor("_BaseColor",new Color(.28f,.3f,.34f));floor.SetFloat("_Smoothness",.05f);
            ground.GetComponent<Renderer>().sharedMaterial=Save(floor,Root+"/Materials/NeutralFloor.mat");UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            var volumeGO=new GameObject("Restrained bloom",typeof(Volume));var volume=volumeGO.GetComponent<Volume>();volume.isGlobal=true;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/PreviewVolume.asset");
            if(profile==null) {profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/PreviewVolume.asset");}
            if(!profile.TryGet<Bloom>(out var bloom)) {bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.threshold.Override(1.1f);bloom.intensity.Override(.32f);bloom.scatter.Override(.55f);
            EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(profile);volume.sharedProfile=profile;
            var showcase=new GameObject("MacAnh Graphics Preview").AddComponent<MacAnhGraphicsPreview>();
            showcase.gameObject.AddComponent<MacAnhGraphicsBenchmark>();
            var skills=new GameObject[11];foreach(MacAnhAction action in Enum.GetValues(typeof(MacAnhAction))) skills[(int)action]=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/FX_MacAnh_"+action+"3D.prefab");
            showcase.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/MacAnh_Character3D.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(CyclonePath),camera,pipeline,skills);
            showcase.ConfigureAux(LoadFx("Prepare"),LoadFx("ArrayPrepare"),LoadFx("UltimatePrepare"),LoadFx("Afterimage"),LoadFx("Impact"));
            EditorSceneManager.SaveScene(scene,ScenePath);
        }
        [MenuItem("Arena3Q/Mac Anh/Graphics/Open 3D Preview")]
        public static void Open() { EditorSceneManager.OpenScene(ScenePath); }
        [MenuItem("Arena3Q/Mac Anh/Graphics/Rebuild VFX Only")]
        public static void RebuildVfx() {BuildCyclone();AssetDatabase.SaveAssets();}
        static GameObject LoadFx(string key)=>AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/FX_MacAnh_"+key+"3D.prefab");
        public static void AddPreparationEvents()
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Source/MacAnh_OriginalActions.fbx");
            var clips=importer.clipAnimations;
            foreach(var clip in clips)
            {
                var events=clip.events.Where(e=>e.stringParameter!="Prepare"&&e.stringParameter!="Afterimage").ToList();
                float span=Mathf.Max(1,clip.lastFrame-clip.firstFrame);
                foreach(var e in events)
                {
                    int f=e.stringParameter=="SpawnWindSlash"?14:e.stringParameter=="SpawnTripleFeather"?15+9*e.intParameter:e.stringParameter=="SpawnMagicArray"?33:e.stringParameter=="SpawnCyclone"?30:e.stringParameter=="ApplyShield"?27:e.stringParameter=="OnUltimateHit"?75:e.stringParameter=="SpawnCounterSlash"?24:0;
                    if(f>0)e.time=f/span;
                }
                int frame=clip.name=="Attack01"?10:clip.name=="Attack02"?9:clip.name=="Skill01"?16:clip.name=="Skill02"?15:clip.name=="Skill03"?13:clip.name=="Ultimate"?30:clip.name=="DodgeAttack"?7:0;
                if(frame>0) events.Add(new AnimationEvent{functionName="OnMacAnhAnimationEvent",stringParameter="Prepare",time=frame/span});
                if(clip.name=="DodgeAttack") for(int i=0;i<3;i++) events.Add(new AnimationEvent{functionName="OnMacAnhAnimationEvent",stringParameter="Afterimage",intParameter=i,time=(8+i*2)/span});
                clip.events=events.OrderBy(e=>e.time).ToArray();
            }
            importer.clipAnimations=clips;importer.SaveAndReimport();
        }
        public static void UpdateSourceColors()
        {
            var bridge=JsonUtility.FromJson<Bridge>(File.ReadAllText(Root+"/Source/MaterialBridge.json"));
            foreach(var src in bridge.materials)
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+src.name+"_URP.mat");
                var color=new Color(src.color[0],src.color[1],src.color[2],src.color[3]);
                material.SetColor("_BaseColor",string.IsNullOrEmpty(src.baseMap)?color:Color.white);EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
        public static void Capture(string name)
        {
            var preview=UnityEngine.Object.FindAnyObjectByType<MacAnhGraphicsPreview>();
            var camera=preview.PreviewCamera;var old=camera.targetTexture;var active=RenderTexture.active;var oldRect=camera.rect;
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4};
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try
            {
                rt.Create();camera.rect=new Rect(0,0,1,1);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                string path="docs/art/concepts/MuuSi_main-hero/MacAnh/Production/GraphicsUpgrade/Captures/"+name+"_1920x1080.png";
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {camera.targetTexture=old;camera.rect=oldRect;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
        [MenuItem("Arena3Q/Mac Anh/Graphics/Build Android Preview")]
        public static void BuildAndroidPreview()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building.");
            var oldId=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            var oldName=PlayerSettings.productName;var oldPipeline=QualitySettings.renderPipeline;
            var oldBundle=EditorUserBuildSettings.buildAppBundle;
            var oldDeformation=PlayerSettings.meshDeformation;
            int oldQuality=QualitySettings.GetQualityLevel();
            // Player builds can unload Editor asset instances. Keep paths rather
            // than Unity Object references so restoration survives that unload.
            string oldDefaultPipelinePath=AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
            var oldQualityPipelines=new string[QualitySettings.names.Length];
            for(int i=0;i<oldQualityPipelines.Length;i++)oldQualityPipelines[i]=AssetDatabase.GetAssetPath(QualitySettings.GetRenderPipelineAssetAt(i));
            try
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.arena3q.macanhpreview");
                PlayerSettings.productName="MacAnh Graphics Preview";
                // The original high-density rig is preserved. Compute skinning
                // avoids deforming its millions of vertices on the CPU.
                PlayerSettings.meshDeformation=MeshDeformation.GPU;
                QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/MacAnhPreviewURP.asset");
                var previewPipeline=QualitySettings.renderPipeline;
                // URP's build stripper inspects the target platform's quality
                // levels, not only the Editor's current override. Otherwise it
                // removes UniversalRendererResources as though this were 2D-only.
                GraphicsSettings.defaultRenderPipeline=previewPipeline;
                for(int i=0;i<oldQualityPipelines.Length;i++)
                {QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=previewPipeline;}
                QualitySettings.SetQualityLevel(oldQuality,false);
                EditorUserBuildSettings.buildAppBundle=false;Directory.CreateDirectory("Builds/MacAnhPreview");
                var options=new BuildPlayerOptions {scenes=new[]{ScenePath},locationPathName="Builds/MacAnhPreview/MacAnhPreview.apk",target=BuildTarget.Android,
                    options=BuildOptions.Development,extraScriptingDefines=new[]{"MACANH_GRAPHICS_BENCHMARK"}};
                var report=BuildPipeline.BuildPlayer(options);
                long apkBytes=report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?new FileInfo(options.locationPathName).Length:0;
                var result=new {result=report.summary.result.ToString(),seconds=report.summary.totalTime.TotalSeconds,apkBytes,uncompressedBuildBytes=report.summary.totalSize,skinning=PlayerSettings.meshDeformation.ToString()};
                File.WriteAllText("Logs/macanh-android-build.txt",result.ToString());
                Debug.Log("[MacAnh Graphics] Android preview build "+result+"; APK "+(apkBytes/1048576.0).ToString("F1")+" MB");
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,oldId);PlayerSettings.productName=oldName;
                QualitySettings.renderPipeline=oldPipeline;EditorUserBuildSettings.buildAppBundle=oldBundle;
                PlayerSettings.meshDeformation=oldDeformation;
                for(int i=0;i<oldQualityPipelines.Length;i++)
                {QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=string.IsNullOrEmpty(oldQualityPipelines[i])?null:AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldQualityPipelines[i]);}
                QualitySettings.SetQualityLevel(oldQuality,false);
                GraphicsSettings.defaultRenderPipeline=string.IsNullOrEmpty(oldDefaultPipelinePath)?null:AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldDefaultPipelinePath);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
