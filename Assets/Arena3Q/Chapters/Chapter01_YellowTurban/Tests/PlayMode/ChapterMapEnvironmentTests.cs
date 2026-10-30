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
