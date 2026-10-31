#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arena3Q.Exploration.Tests
{
    public sealed partial class ChapterMapFlowTests
    {
        [System.Serializable] private sealed class EnvironmentCounters { public string mode;public bool srp;public long drawCalls,batches,setPass; }
        [System.Serializable] private sealed class EnvironmentCounterReport { public EnvironmentCounters[] samples; }
        [UnityTest] public IEnumerator CaptureEnvironmentBatching()
        {
            var rows=new System.Collections.Generic.List<EnvironmentCounters>();
            bool batching=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
            var canvas=_map.Hud.GetComponentInChildren<Canvas>();canvas.enabled=false;
            var details=_map.GetComponent<ChapterMapDetails>();if(details!=null)Object.Destroy(details);
            var env=_map.GetComponent<ChapterMapEnvironment>();Object.Destroy(env);yield return null;
            var visibility=(float[])typeof(ChapterMapWorld).GetField("_visibility",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(_map.World);
            for(int i=0;i<visibility.Length;i++)visibility[i]=1;
            _map.View.FocusOn(_map.Config.stages[2].center);_map.World.SendMessage("LateUpdate");_map.World.enabled=false;
            var camera=_map.View.Camera;var previousTarget=camera.targetTexture;var target=new RenderTexture(1672,941,24);target.Create();camera.targetTexture=target;
            try
            {
                foreach(string mode in new[]{"source","motion"})
                {
                    if(mode=="motion"){env=_map.gameObject.AddComponent<ChapterMapEnvironment>();env.Configure(_map);env.enabled=false;env.Evaluate(.6f);}
                    foreach(bool srp in new[]{false,true})
                    {
                        UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=srp;
                        using var draw=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"Draw Calls Count");
                        using var batch=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"Batches Count");
                        using var pass=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"SetPass Calls Count");
                        for(int i=0;i<5;i++){camera.Render();yield return null;}
                        var row=new EnvironmentCounters{mode=mode,srp=srp,drawCalls=draw.Valid&&draw.Count>0?draw.LastValue:-1,batches=batch.Valid&&batch.Count>0?batch.LastValue:-1,setPass=pass.Valid&&pass.Count>0?pass.LastValue:-1};rows.Add(row);
                        Capture("batching-"+mode+"-"+(srp?"on":"off"));
                    }
                }
                System.IO.File.WriteAllText(CaptureDirectory+"/batching.json",JsonUtility.ToJson(new EnvironmentCounterReport{samples=rows.ToArray()},true));
            }
            finally{camera.targetTexture=previousTarget;target.Release();Object.Destroy(target);UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=batching;canvas.enabled=true;_map.World.enabled=true;}
        }
        [UnityTest] public IEnumerator CaptureEnvironmentMotionPresets()
        {
            var environment=_map.GetComponent<ChapterMapEnvironment>();environment.enabled=false;
            var visibility=(float[])typeof(ChapterMapWorld).GetField("_visibility",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(_map.World);
            var saved=(float[])visibility.Clone();for(int i=0;i<visibility.Length;i++)visibility[i]=1;
            bool batching=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
            try
            {
                UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=false;
                foreach(int rate in new[]{30,60,120})
                {
                    Assert.True(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1672,height=941,dpi=160,frameRate=rate}));
                    for(int view=0;view<3;view++)
                    {
                        _map.View.FocusOn(_map.Config.stages[view==0?0:view==1?2:1].center);
                        for(int frame=0;frame<5;frame++)
                        {
                            environment.Evaluate(frame*.55f);yield return null;Capture("environment-"+new[]{"village","bamboo","water"}[view]+"-"+rate+"-"+frame);
                        }
                    }
                }
            }
            finally{saved.CopyTo(visibility,0);DisplayMetrics.Reset();UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=batching;environment.enabled=true;}
        }
        [UnityTest] public IEnumerator EnvironmentMotionPreservesSourcesAndProgressAcrossPresets()
        {
            var environment=_map.GetComponent<ChapterMapEnvironment>();var config=_map.Config.motion;
            Assert.True(config.environmentMotion);Assert.AreEqual(1,environment.WaterRenderers);Assert.AreEqual(30,environment.BambooRenderers);
            Assert.AreNotSame(config.waterSource,environment.WaterMaterial);Assert.AreNotSame(config.bambooSource,environment.BambooMaterial);
            var sourceShader=config.waterSource.shader;var sourceOffset=config.waterSource.mainTextureOffset;
            var progress=JsonUtility.ToJson(_map.Session.Progress);environment.enabled=false;
            foreach(int rate in new[]{30,60,120})
            {
                Assert.True(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1672,height=941,dpi=160,frameRate=rate}));
                environment.Evaluate(2);yield return null;
                Assert.AreEqual(progress,JsonUtility.ToJson(_map.Session.Progress),"Decorative presets must not change chapter progress.");
                Assert.AreEqual(rate==30?0:config.waterSecondBlend,environment.WaterMaterial.GetVector("_Flow").z,.00001f);
                Assert.AreEqual(2*config.waterSpeed,environment.WaterMaterial.GetVector("_Flow").x,.00001f);
                Assert.AreEqual(config.bambooAmplitude*(rate==30?config.lowBambooMultiplier:1),environment.BambooMaterial.GetVector("_Motion").y,.00001f);
                var wind=Shader.GetGlobalVector("_ArenaWind");Assert.AreEqual(config.battlefieldWind.windStrength*config.windStrength*(rate==30?config.lowWindMultiplier:1),wind.z,.00001f);
            }
            Assert.AreEqual(sourceShader,config.waterSource.shader);Assert.AreEqual(sourceOffset,config.waterSource.mainTextureOffset);
            Assert.True(Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.sharedMaterials.Contains(environment.BambooMaterial)).All(r=>r.HasPropertyBlock()));
            Assert.True(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1672,height=941,dpi=160,frameRate=30}));
            environment.enabled=true;yield return null;
            Assert.Greater(environment.WaterMaterial.GetVector("_Flow").z,0,"Changing tier must fade the second layer, not remove it in one frame.");
            yield return new WaitForSecondsRealtime(config.environmentFadeSeconds+.1f);
            Assert.AreEqual(0,environment.WaterMaterial.GetVector("_Flow").z,.00001f);
            var affected=Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.sharedMaterials.Contains(environment.BambooMaterial)).ToArray();
            Object.Destroy(environment);yield return null;
            Assert.True(affected.All(r=>r.sharedMaterials.Contains(config.bambooSource)),"Leaving presentation restores source materials.");
            Assert.True(affected.All(r=>!r.HasPropertyBlock()),"Temporary plant bounds must not survive presentation cleanup.");
            DisplayMetrics.Reset();
        }
    }
}
#endif
