using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
namespace Arena3Q.Lobby2D.Editor
{
    public static class LuBuSpriteAnimationBuilder
    {
        private const string Root="Assets/Arena3Q/Lobby2D";
        public static void Build()
        {
            Directory.CreateDirectory(Root+"/Unity/Animators");Directory.CreateDirectory(Root+"/Unity/Materials");AssetDatabase.Refresh();
            var clips=new AnimationClip[3];var names=new[]{"Idle","Run","Attack"};var durations=new[]{2f,.9f,1.1f};Sprite first=null;
            for(var c=0;c<3;c++){
                var keys=new ObjectReferenceKeyframe[24];
                for(var i=0;i<24;i++){
                    var path=Root+"/Characters/LuBu/Animations/"+names[c]+"/LuBu_"+names[c]+"_"+i.ToString("D3")+".png";
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    var needsImport=importer.textureType!=TextureImporterType.Sprite||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.mipmapEnabled||importer.spritePivot!=new Vector2(.5f,.2f);
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;
                    importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
                    importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                    var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                    settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.2f);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;
                    importer.SetTextureSettings(settings);if(needsImport)importer.SaveAndReimport();
                    var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!sprite)throw new InvalidDataException(path);if(first==null)first=sprite;
                    keys[i]=new ObjectReferenceKeyframe{time=i*durations[c]/24,value=sprite};
                }
                var clipPath=Root+"/Unity/Animators/LuBu_"+(c==2?"Attack_01":names[c])+".anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
                clip.frameRate=24/durations[c];
                AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(Image),"m_Sprite"),keys);
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Image),"m_Color.a"),AnimationCurve.Constant(0,durations[c],1));
                var clipSettings=AnimationUtility.GetAnimationClipSettings(clip);clipSettings.loopTime=c<2;clipSettings.stopTime=durations[c];AnimationUtility.SetAnimationClipSettings(clip,clipSettings);
                AnimationUtility.SetAnimationEvents(clip,c==2?new[]{new AnimationEvent{time=13*durations[c]/24,functionName="OnAttackImpact"}}:Array.Empty<AnimationEvent>());
                EditorUtility.SetDirty(clip);clips[c]=clip;
            }
            var controllerPath=Root+"/Unity/Animators/LuBu_Sprites.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
            foreach(var parameter in controller.parameters)controller.RemoveParameter(parameter);
            controller.AddParameter("IsRunning",AnimatorControllerParameterType.Bool);controller.AddParameter("Attack",AnimatorControllerParameterType.Trigger);
            var idle=machine.AddState("Idle");var run=machine.AddState("Run");var attack=machine.AddState("Attack");idle.motion=clips[0];run.motion=clips[1];attack.motion=clips[2];machine.defaultState=idle;
            var toRun=idle.AddTransition(run);toRun.hasExitTime=false;toRun.duration=0;toRun.AddCondition(AnimatorConditionMode.If,0,"IsRunning");
            var toIdle=run.AddTransition(idle);toIdle.hasExitTime=false;toIdle.duration=0;toIdle.AddCondition(AnimatorConditionMode.IfNot,0,"IsRunning");
            var toAttack=machine.AddAnyStateTransition(attack);toAttack.hasExitTime=false;toAttack.duration=0;toAttack.canTransitionToSelf=false;toAttack.AddCondition(AnimatorConditionMode.If,0,"Attack");
            var recover=attack.AddTransition(idle);recover.hasExitTime=true;recover.exitTime=1;recover.duration=0;
            var prefab=new GameObject("LuBu_Sprite_Animation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Animator),typeof(LuBuAnimationEventRelay));
            var rect=prefab.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(2048,2048);rect.pivot=new Vector2(.5f,.2f);
            prefab.GetComponent<Image>().sprite=first;prefab.GetComponent<Image>().raycastTarget=false;prefab.GetComponent<Animator>().runtimeAnimatorController=controller;
            PrefabUtility.SaveAsPrefabAsset(prefab,Root+"/Unity/Prefabs/LuBu_Sprite_Animation.prefab");UnityEngine.Object.DestroyImmediate(prefab);
            var materialPath=Root+"/Unity/Materials/LobbyUI.mat";
            if(!AssetDatabase.LoadAssetAtPath<Material>(materialPath))AssetDatabase.CreateAsset(new Material(Shader.Find("UI/Default")),materialPath);
            AssetDatabase.SaveAssets();Debug.Log("LUBU_SPRITE_CONTENT_OK clips=3 sprites=72 attackEvents=1 attackLoop=false");
        }
    }
}
