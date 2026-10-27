using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Lobby2D.Editor
{
    public static class LuBuCutoutBuilder
    {
        private const string Root="Assets/Arena3Q/Lobby2D";
        private const string Source="ArtSource/Lobby2D";
        [Serializable] private sealed class TextureList { public TextureEntry[] textures; }
        [Serializable] private sealed class TextureEntry { public string id,path; }
        [Serializable] private sealed class RigInput
        {
            public LuBuCutoutDefinition.Part[] parts;
            public LuBuCutoutDefinition.Joint[] joints;
            public Vector2 attackGripCenter;
            public float runCrouch;
            public ClipInput[] clips;
        }
        [Serializable] private sealed class ClipInput
        {
            public string name; public float duration; public bool loop;
            public Vector2[] lean,crouch,hipShift,weaponAngle,gripBlend;
        }
        [Serializable] private sealed class Audit
        {
            public int sampledPoses,parts,vertices;
            public float maximumReachError,maximumRigidScaleError,maximumClothEdgeStretch,minimumClothEdgeStretch=1,maximumLoopVertexStep,runContactSlip;
            public bool passed;
            public int transitionSamples;public float transitionReachError;
            public string reachErrorBone;public int reachErrorClip;public float reachErrorTime;
        }
        public static LuBuCutoutDefinition Build()
        {
            AssetDatabase.Refresh();
            var textures=JsonUtility.FromJson<TextureList>(File.ReadAllText(Source+"/rig-texture-list.json"));
            var map=new Dictionary<string,Texture2D>();
            foreach(var entry in textures.textures){
                var importer=(TextureImporter)AssetImporter.GetAtPath(entry.path);
                importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear;
                foreach(var name in new[]{"Android","iPhone"})importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{
                    name=name,overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_4x4,compressionQuality=100});
                importer.SaveAndReimport();map[entry.id]=AssetDatabase.LoadAssetAtPath<Texture2D>(entry.path);
            }
            var input=JsonUtility.FromJson<RigInput>(File.ReadAllText(Source+"/rig.json"));
            var path=Root+"/Unity/LuBuCutoutRig.asset";
            var rig=AssetDatabase.LoadAssetAtPath<LuBuCutoutDefinition>(path);
            if(!rig){rig=ScriptableObject.CreateInstance<LuBuCutoutDefinition>();AssetDatabase.CreateAsset(rig,path);}
            rig.parts=input.parts;rig.joints=input.joints;rig.attackGripCenter=input.attackGripCenter;
            rig.runCrouch=input.runCrouch;
            rig.slash=BuildSlash();
            foreach(var part in rig.parts)part.texture=map[part.textureId];
            rig.clips=input.clips.Select(c=>new LuBuCutoutDefinition.Clip{
                name=c.name,duration=c.duration,loop=c.loop,lean=Curve(c.lean),crouch=Curve(c.crouch),hipShift=Curve(c.hipShift),weaponAngle=Curve(c.weaponAngle),gripBlend=Curve(c.gripBlend)
            }).ToArray();
            EditorUtility.SetDirty(rig);AssetDatabase.SaveAssets();
            var prefabRoot=new GameObject("LuBu_Skeletal_UI",typeof(RectTransform));
            var rect=prefabRoot.GetComponent<RectTransform>();rect.sizeDelta=rig.frameSize;rect.pivot=rig.framePivot;
            prefabRoot.AddComponent<LuBuCutoutView>().definition=rig;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot,Root+"/Unity/Prefabs/LuBu_Skeletal_UI.prefab");
            UnityEngine.Object.DestroyImmediate(prefabRoot);
            return rig;
        }
        private static AnimationCurve Curve(Vector2[] values)
        {
            var curve=new AnimationCurve(values.Select(v=>new Keyframe(v.x,v.y)).ToArray());
            for(var i=0;i<curve.length;i++)curve.SmoothTangents(i,0);
            return curve;
        }
        [MenuItem("Arena3Q/Legacy Art Review/Lobby 2D/Lu Bu/Build Rig and Review Poses")]
        public static void BuildAndReview()
        {
            var rig=Build(); var output=Path.GetFullPath("NghiemThu/LuBu/XemTruoc_LobbyRig_20261026");Directory.CreateDirectory(output);
            var audit=AuditRig(rig);File.WriteAllText(Path.Combine(output,"surface-audit.json"),JsonUtility.ToJson(audit,true));
            CreateReviewScene(ref rig,out var camera,out var view);
            for(var clip=0;clip<3;clip++)for(var i=0;i<12;i++){
                view.Sample(clip,rig.clips[clip].duration*i/12);Canvas.ForceUpdateCanvases();
                Capture(camera,rig.frameSize,Path.Combine(output,rig.clips[clip].name+"_"+(i*2).ToString("D3")+".png"));
            }
            CaptureEffects(camera,view,rig);
            if(File.Exists(Root+"/Characters/LuBu/Animations/Attack/LuBu_Attack_023.png"))LuBuSpriteAnimationBuilder.Build();
            Debug.Log("LUBU_CUTOUT_REVIEW_OK "+JsonUtility.ToJson(audit));
        }
        [MenuItem("Arena3Q/Legacy Art Review/Lobby 2D/Lu Bu/Export 24 Frames and Layers")]
        public static void Export()
        {
            var rig=Build();var audit=AuditRig(rig);
            var review=Path.GetFullPath("NghiemThu/LuBu/XemTruoc_LobbyRig_20261026");Directory.CreateDirectory(review);
            File.WriteAllText(Path.Combine(review,"surface-audit.json"),JsonUtility.ToJson(audit,true));
            if(!audit.passed)throw new InvalidOperationException("Fix cutout reach/deformation/contact audit before exporting production frames.");
            CreateReviewScene(ref rig,out var camera,out var view);
            for(var clip=0;clip<3;clip++){
                var name=clip==2?"Attack":clip==1?"Run":"Idle";
                var folder=Root+"/Characters/LuBu/Animations/"+name;Directory.CreateDirectory(folder);
                for(var i=0;i<24;i++){
                    view.Sample(clip,rig.clips[clip].duration*i/24);Canvas.ForceUpdateCanvases();
                    Capture(camera,rig.frameSize,folder+"/LuBu_"+name+"_"+i.ToString("D3")+".png");
                }
            }
            var layerFolder=Root+"/Characters/LuBu/Layers";Directory.CreateDirectory(layerFolder);
            view.Sample(0,0);
            foreach(var layer in rig.parts.Select(p=>p.layer).Distinct()){
                view.ShowLayer(layer);Canvas.ForceUpdateCanvases();Capture(camera,rig.frameSize,layerFolder+"/"+layer+".png");
            }
            view.ShowLayer(null);AssetDatabase.Refresh();
            Debug.Log("LUBU_CUTOUT_EXPORT_OK frames=72 layers=26 pivot="+rig.framePivot);
        }
        private static void CreateReviewScene(ref LuBuCutoutDefinition rig,out Camera camera,out LuBuCutoutView view)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            rig=AssetDatabase.LoadAssetAtPath<LuBuCutoutDefinition>(Root+"/Unity/LuBuCutoutRig.asset");
            rig.slash=AssetDatabase.LoadAssetAtPath<LuBuSlashDefinition>(Root+"/Unity/LuBuSlash.asset");
            camera=new GameObject("LuBuCaptureCamera",typeof(Camera)).GetComponent<Camera>();
            camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;
            var root=new GameObject("CharacterCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scale.scaleFactor=1;
            var character=new GameObject("LuBu",typeof(RectTransform));var rect=character.GetComponent<RectTransform>();rect.SetParent(root.transform,false);
            rect.anchorMin=rect.anchorMax=rig.framePivot;rect.pivot=rig.framePivot;rect.sizeDelta=rig.frameSize;rect.anchoredPosition=Vector2.zero;
            view=character.AddComponent<LuBuCutoutView>();view.definition=rig;view.animate=false;view.effectsEnabled=false;view.Build();
        }
        private static LuBuSlashDefinition BuildSlash()
        {
            var source=Root+"/Effects/Characters/Slash_Source.png";if(!File.Exists(source))return null;
            var importer=(TextureImporter)AssetImporter.GetAtPath(source);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var path=Root+"/Unity/LuBuSlash.asset";var effect=AssetDatabase.LoadAssetAtPath<LuBuSlashDefinition>(path);
            if(!effect){effect=ScriptableObject.CreateInstance<LuBuSlashDefinition>();AssetDatabase.CreateAsset(effect,path);}
            JsonUtility.FromJsonOverwrite(File.ReadAllText(Source+"/slash.json"),effect);
            effect.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(source);EditorUtility.SetDirty(effect);return effect;
        }
        private static void CaptureEffects(Camera camera,LuBuCutoutView view,LuBuCutoutDefinition rig)
        {
            if(!rig.slash)throw new InvalidOperationException("Reload the slash dependency before VFX capture.");
            var folder=Root+"/Effects/Characters/LuBu_Attack";Directory.CreateDirectory(folder);
            view.ShowLayer("__effects_only");view.effectsEnabled=true;
            for(var i=0;i<24;i++){
                view.Sample(2,rig.clips[2].duration*i/24);Canvas.ForceUpdateCanvases();
                Capture(camera,rig.frameSize,folder+"/LuBu_Attack_VFX_"+i.ToString("D3")+".png");
            }
            view.effectsEnabled=false;view.ShowLayer(null);
            Debug.Log("LUBU_VFX_EXPORT_OK frames=24 independent=true");
        }
        private static void Capture(Camera camera,Vector2 size,string file)
        {
            var old=RenderTexture.active;var target=RenderTexture.GetTemporary((int)size.x,(int)size.y,24,RenderTextureFormat.ARGB32);
            var image=new Texture2D((int)size.x,(int)size.y,TextureFormat.RGBA32,false);
            try{
                camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());
            }finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);}
        }
        private static Audit AuditRig(LuBuCutoutDefinition rig)
        {
            var result=new Audit{parts=rig.parts.Length,vertices=rig.parts.Sum(p=>p.vertices.Length)};
            var pose=new LuBuCutoutPose();
            for(var clip=0;clip<3;clip++)for(var sample=0;sample<=96;sample++){
                pose.Evaluate(rig,clip,rig.clips[clip].duration*sample/96);result.sampledPoses++;
                if(pose.maximumReachError>result.maximumReachError){result.maximumReachError=pose.maximumReachError;result.reachErrorBone=pose.reachErrorBone;result.reachErrorClip=clip;result.reachErrorTime=pose.time;}
                result.maximumRigidScaleError=Mathf.Max(result.maximumRigidScaleError,pose.rigidScaleError);
                foreach(var part in rig.parts.Where(p=>p.flexibility>0))for(var i=0;i<part.triangles.Length;i++){
                    var a=part.triangles[i];var b=part.triangles[i%3==2?i-2:i+1];
                    var rest=Vector2.Distance(part.vertices[a],part.vertices[b]);if(rest<.001f)continue;
                    var ratio=Vector2.Distance(pose.Vertex(part,a),pose.Vertex(part,b))/rest;
                    result.maximumClothEdgeStretch=Mathf.Max(result.maximumClothEdgeStretch,ratio);
                    result.minimumClothEdgeStretch=Mathf.Min(result.minimumClothEdgeStretch,ratio);
                }
            }
            const float dt=.001f;
            var aPose=new LuBuCutoutPose();var bPose=new LuBuCutoutPose();
            for(var clip=0;clip<2;clip++){
                aPose.Evaluate(rig,clip,0);bPose.Evaluate(rig,clip,rig.clips[clip].duration);
                foreach(var part in rig.parts)for(var vertex=0;vertex<part.vertices.Length;vertex++)
                    result.maximumLoopVertexStep=Mathf.Max(result.maximumLoopVertexStep,Vector2.Distance(aPose.Vertex(part,vertex),bPose.Vertex(part,vertex)));
            }
            var speed=rig.runStride/(rig.runStance*rig.clips[1].duration);
            for(var i=1;i<95;i++){
                var t=rig.clips[1].duration*i/96;aPose.Evaluate(rig,1,t);bPose.Evaluate(rig,1,t+dt);
                foreach(var side in new[]{"R","L"}){
                    var phase=Mathf.Repeat(t/rig.clips[1].duration+(side=="R"?0:.5f),1);
                    if(phase>=rig.runStance-.001f||phase<.001f)continue;
                    var velocity=(bPose.points["Foot_"+side]-aPose.points["Foot_"+side])/dt+Vector2.right*speed;
                    result.runContactSlip=Mathf.Max(result.runContactSlip,velocity.magnitude);
                }
            }
            for(var from=0;from<3;from++)for(var to=0;to<3;to++)if(from!=to){
                aPose.Evaluate(rig,from,rig.clips[from].duration*.65f);var previous=aPose.Snapshot();
                for(var i=0;i<=24;i++){
                    bPose.Evaluate(rig,to,rig.transitionSeconds*i/24);bPose.BlendFrom(previous,i/24f);
                    result.transitionSamples++;result.transitionReachError=Mathf.Max(result.transitionReachError,bPose.maximumReachError);
                }
            }
            result.passed=result.maximumReachError<1&&result.maximumRigidScaleError<.00001f&&result.maximumClothEdgeStretch<1.25f&&result.minimumClothEdgeStretch>.75f&&result.runContactSlip<1&&result.maximumLoopVertexStep<.05f&&result.transitionReachError<1;
            return result;
        }
    }
}
