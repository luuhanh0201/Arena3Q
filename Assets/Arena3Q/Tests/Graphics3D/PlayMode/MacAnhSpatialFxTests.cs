#if UNITY_EDITOR
using System.Collections;
using Arena3Q.Gameplay;
using Arena3Q.Graphics3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arena3Q.Tests.Graphics3D
{
    public sealed class MacAnhSpatialFxTests
    {
        const string Root="Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D";
        MacAnhArcaneEffect _effect;
        [TearDown] public void Clean() {if(_effect!=null)Object.DestroyImmediate(_effect.gameObject);}
        [UnityTest] public IEnumerator ManualClockAlsoPausesParticlesAndStopsCleanly()
        {
            _effect=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/FX_MacAnh_Cyclone3D.prefab")).GetComponent<MacAnhArcaneEffect>();
            _effect.ManualClock=true;_effect.Begin(MacAnhFxQuality.High,3.2f);_effect.Advance(.8f);
            int count=_effect.ActiveParticleCount;Assert.That(count,Is.GreaterThan(0));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(_effect.Age,Is.EqualTo(.8f).Within(.00001f));Assert.That(_effect.ActiveParticleCount,Is.EqualTo(count));
            _effect.Seek(2.75f);Assert.That(_effect.ActiveParticleCount,Is.GreaterThan(0),"Dissolve releases light particles");
            _effect.Stop();Assert.That(_effect.ActiveParticleCount,Is.Zero);
            _effect.Begin(MacAnhFxQuality.Low,3.2f);Assert.That(_effect.Age,Is.Zero);
            Assert.That(_effect.transform.GetChild(0).gameObject.activeSelf,Is.True);Assert.That(_effect.transform.GetChild(2).gameObject.activeSelf,Is.False);
        }
        [UnityTest] public IEnumerator ShortAfterimageReachesVisiblePeakBeforeFading()
        {
            _effect=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/FX_MacAnh_Afterimage3D.prefab")).GetComponent<MacAnhArcaneEffect>();
            _effect.ManualClock=true;_effect.Begin(MacAnhFxQuality.Medium,.4f);_effect.Advance(.15f);
            var block=new MaterialPropertyBlock();_effect.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);
            Assert.That(block.GetFloat("_Fade"),Is.GreaterThan(.9f));
            _effect.Advance(.3f);Assert.That(_effect.IsPlaying,Is.False);yield return null;
        }
        [UnityTest] public IEnumerator CycloneTailIsNearlyInvisibleBeforePoolRelease()
        {
            _effect=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/FX_MacAnh_Cyclone3D.prefab")).GetComponent<MacAnhArcaneEffect>();
            _effect.ManualClock=true;_effect.Begin(MacAnhFxQuality.High,3.2f);
            _effect.Seek(1.2f);MeshRenderer body=null;
            foreach(var renderer in _effect.GetComponentsInChildren<MeshRenderer>())if(renderer.name.Contains("Bounded"))body=renderer;
            Assert.That(body,Is.Not.Null);var peakScale=body.transform.localScale;
            _effect.Seek(2.7f);
            Assert.That(Vector3.Distance(body.transform.localScale,peakScale),Is.LessThan(.00001f),"Cloud must erode in place rather than shrink into a point");
            _effect.Seek(3.19f);
            Assert.That(_effect.IsPlaying,Is.True,"Tail must remain active until its scheduled release");
            var block=new MaterialPropertyBlock();
            foreach(var renderer in _effect.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.GetPropertyBlock(block);
                Assert.That(Mathf.Abs(block.GetFloat("_Fade")),Is.LessThan(.01f),renderer.name+" must fade before deactivation");
            }
            Assert.That(_effect.ActiveParticleCount,Is.Zero,"Particles must finish their lifetimes before pool release");
            _effect.Advance(.02f);Assert.That(_effect.IsPlaying,Is.False);yield return null;
        }
    }
}
#endif
