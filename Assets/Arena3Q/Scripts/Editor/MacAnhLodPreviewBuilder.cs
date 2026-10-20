using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Data;
using Arena3Q.Graphics3D;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class MacAnhLodPreviewBuilder
    {
        public static void Build()
        {
            string root=MacAnhGraphicsBuilder.Root+"/LOD",prefabPath=root+"/MacAnh_Portrait3D.prefab";
            Directory.CreateDirectory(root+"/PreviewClips");AssetDatabase.Refresh();
            var profile=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(MacAnhContentBuilder.Art+"/MacAnhPerformance.asset");
            var source=AssetDatabase.LoadAllAssetsAtPath(MacAnhGraphicsBuilder.Root+"/Source/LOD/MacAnh_LODActions.fbx")
                .OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            if(source.Length!=11)throw new InvalidOperationException("Expected eleven LOD actions.");
            string controllerPath=root+"/MacAnh_LODPreview.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
            foreach(var original in source)
            {
                string path=root+"/PreviewClips/"+original.name+".anim";
                var copy=UnityEngine.Object.Instantiate(original);copy.name=original.name;
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(clip==null){AssetDatabase.CreateAsset(copy,path);clip=copy;}
                else{EditorUtility.CopySerialized(copy,clip);UnityEngine.Object.DestroyImmediate(copy);}
                var action=(MacAnhAction)Enum.Parse(typeof(MacAnhAction),clip.name);
                var metadata=profile.MetadataClips.First(c=>c.action==action);
                var events=metadata.events.Select(mark=>new AnimationEvent{functionName="OnMacAnhAnimationEvent",stringParameter=mark.key,intParameter=mark.comboIndex,time=mark.Seconds}).ToList();
                int prepare=action==MacAnhAction.Attack01?10:action==MacAnhAction.Attack02?9:action==MacAnhAction.Skill01?16:
                    action==MacAnhAction.Skill02?15:action==MacAnhAction.Skill03?13:action==MacAnhAction.Ultimate?30:action==MacAnhAction.DodgeAttack?7:0;
                if(prepare>0)events.Add(new AnimationEvent{functionName="OnMacAnhAnimationEvent",stringParameter="Prepare",time=prepare/30f});
                if(action==MacAnhAction.DodgeAttack)for(int i=0;i<3;i++)events.Add(new AnimationEvent{functionName="OnMacAnhAnimationEvent",stringParameter="Afterimage",intParameter=i,time=(8+i*2)/30f});
                AnimationUtility.SetAnimationEvents(clip,events.OrderBy(e=>e.time).ToArray());EditorUtility.SetDirty(clip);
                var state=machine.AddState(clip.name);state.motion=clip;if(action==MacAnhAction.Idle)machine.defaultState=state;
            }
            var go=PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                if(go.GetComponent<MacAnhAnimationEvents>()==null)go.AddComponent<MacAnhAnimationEvents>();
                PrefabUtility.SaveAsPrefabAsset(go,prefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
            AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.OpenScene(MacAnhGraphicsBuilder.ScenePath);
            var preview=UnityEngine.Object.FindAnyObjectByType<MacAnhGraphicsPreview>();
            var serialized=new SerializedObject(preview);serialized.FindProperty("characterPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("MACANH_PORTRAIT_PREVIEW_COMPLETE: shared actions with preview-only events, max four weights.");
        }
    }
}
