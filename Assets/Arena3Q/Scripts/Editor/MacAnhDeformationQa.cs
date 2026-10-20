using System;
using System.IO;
using System.Linq;
using Arena3Q.Gameplay;
using Arena3Q.Graphics3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arena3Q.Editor
{
    /// <summary>Reproducible renders of the imported rig and every effect.</summary>
    public static class MacAnhDeformationQa
    {
        private const string Root=MacAnhGraphicsBuilder.Root;
        private const string Output="docs/art/concepts/MuuSi_main-hero/MacAnh/Production/DeformationFix/UnityCaptures";
        private static bool _effectsOnly;
        public static void BuildLodsAndCapture()
        {
            MacAnhRealtimeBattleBuilder.Build();MacAnhLodPreviewBuilder.Build();
            MacAnhGraphicsBuilder.RebuildVfx();MacAnhSkillGraphicsBuilder.Build();
            MacAnhContentBuilder.UpdatePresentationMaterial();MacAnhBattleFxBuilder.Bind();
            Capture();
        }
        public static void BuildEffectsAndCapture()
        {
            MacAnhGraphicsBuilder.RebuildVfx();MacAnhSkillGraphicsBuilder.Build();
            MacAnhContentBuilder.UpdatePresentationMaterial();MacAnhBattleFxBuilder.Bind();
            _effectsOnly=true;
            try {Capture();} finally {_effectsOnly=false;}
        }
        public static void BuildAndCapture()
        {
            MacAnhGraphicsBuilder.Build();
            Capture();
        }
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene(MacAnhGraphicsBuilder.ScenePath);
            var previous=QualitySettings.renderPipeline;
            var previousWeights=QualitySettings.skinWeights;
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/MacAnhPreviewURP.asset");
            GameObject character=null;
            try
            {
                QualitySettings.renderPipeline=pipeline;
                bool lod=File.Exists(Root+"/Source/LOD/MacAnh_PortraitRig.fbx");
                QualitySettings.skinWeights=lod?SkinWeights.FourBones:SkinWeights.Unlimited;
                character=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+(lod?"/LOD/MacAnh_Portrait3D.prefab":"/MacAnh_Character3D.prefab")));
                character.GetComponent<Animator>().enabled=false;
                foreach(var r in character.GetComponentsInChildren<SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
                var camera=Camera.main;
                camera.backgroundColor=new Color(.388f,.416f,.467f);
                var clips=AssetDatabase.LoadAllAssetsAtPath(Root+(lod?"/Source/LOD/MacAnh_LODActions.fbx":"/Source/MacAnh_OriginalActions.fbx"))
                    .OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
                if(clips.Length!=11)throw new InvalidOperationException("Expected all eleven imported actions.");
                foreach(var clip in _effectsOnly?Array.Empty<AnimationClip>():clips)
                {
                    int release=clip.name=="Attack01"?14:clip.name=="Attack02"?24:clip.name=="Skill01"?33:
                        clip.name=="Skill02"?30:clip.name=="Skill03"?27:clip.name=="Ultimate"?75:clip.name=="DodgeAttack"?24:10;
                    foreach(int offset in new[]{-3,0,3})
                    {
                        float time=Mathf.Clamp((release+offset)/30f,0,clip.length);
                        clip.SampleAnimation(character,time);
                        for(int angle=0;angle<8;angle++)
                        {
                            var rotation=Quaternion.Euler(10,angle*45,0);
                            camera.transform.SetPositionAndRotation(new Vector3(0,.86f,0)+rotation*new Vector3(0,0,-3.45f),rotation);
                            Render(camera,clip.name+"_f"+Mathf.RoundToInt(time*30)+"_a"+angle,640,800);
                        }
                    }
                    Debug.Log("[MacAnh surface QA] Captured "+clip.name+" around release from eight views.");
                }
                character.SetActive(false);
                var floor=GameObject.Find("Neutral matte floor");
                if(floor!=null)floor.SetActive(false);
                foreach(string key in new[]{"Attack01","Attack02","Skill01","Skill02","Skill03","Ultimate","DodgeAttack"})
                {
                    string name=key=="Skill02"?"Cyclone":key;
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/FX_MacAnh_"+name+"3D.prefab");
                    if(prefab==null)throw new InvalidOperationException("Missing effect "+key);
                    var go=UnityEngine.Object.Instantiate(prefab);var effect=go.GetComponent<MacAnhArcaneEffect>();
                    if(key=="Attack01"||key=="Attack02"||key=="DodgeAttack")
                        go.transform.position=Vector3.up*1.1f;
                    try
                    {
                        effect.ManualClock=true;
                        foreach(MacAnhFxQuality quality in Enum.GetValues(typeof(MacAnhFxQuality)))
                        foreach(float age in new[]{.35f,1.2f,2.7f})
                        {
                            effect.Begin(quality,3.2f);effect.Seek(age);
                            for(int view=0;view<4;view++)
                            {
                                var rotation=Quaternion.Euler(view==3?62:13,view*95+17,0);
                                camera.transform.SetPositionAndRotation(new Vector3(0,1.15f,0)+rotation*new Vector3(0,0,-5.8f),rotation);
                                Render(camera,"FX_"+key+"_"+quality+"_t"+Mathf.RoundToInt(age*100)+"_v"+view,800,800);
                            }
                        }
                        if(key=="Skill02")
                        {
                            effect.Begin(MacAnhFxQuality.High,3.2f);effect.Seek(.35f);
                            var rotation=Quaternion.Euler(13,112,0);
                            camera.transform.SetPositionAndRotation(new Vector3(0,1.15f,0)+rotation*new Vector3(0,0,-5.8f),rotation);
                            foreach(var filter in go.GetComponentsInChildren<MeshFilter>())
                                Debug.Log("[Cyclone bounds] "+filter.name+" "+filter.sharedMesh.bounds+" scale "+filter.transform.lossyScale);
                            foreach(string layer in new[]{"Bounded","Rolling wind","Short drifting"})
                            {
                                foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.enabled=renderer.name.Contains(layer);
                                Render(camera,"Diagnostic_"+layer.Replace(' ','_'),1000,1000);
                            }
                        }
                    }
                    finally {UnityEngine.Object.DestroyImmediate(go);}
                    Debug.Log("[MacAnh surface QA] Captured effect "+key+" at three ages, three tiers, four views.");
                }
                File.WriteAllText(Output+(_effectsOnly?"/EffectsComplete.txt":"/Complete.txt"),(_effectsOnly?"":"11 actions × 3 poses × 8 views; ")+"7 effects × 3 tiers × 3 ages × 4 views.\n"+DateTime.UtcNow.ToString("O"));
            }
            finally
            {
                if(character!=null)UnityEngine.Object.DestroyImmediate(character);
                QualitySettings.renderPipeline=previous;
                QualitySettings.skinWeights=previousWeights;
            }
        }
        private static void Render(Camera camera,string name,int width,int height)
        {
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4};
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
                rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
