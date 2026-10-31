#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using Arena3Q.Core;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arena3Q.Exploration.Tests
{
    public sealed partial class ChapterMapFlowTests
    {
        [System.Serializable] private sealed class SupplementCaptureFile { public SupplementCapturePatch[] patches; }
        [System.Serializable] private sealed class SupplementCapturePatch { public string id,sprite; public Vector3 position; }
        [UnityTest] public IEnumerator CaptureSupplementDetails()
        {
            var input=JsonUtility.FromJson<SupplementCaptureFile>(System.IO.File.ReadAllText("ArtSource/Chapter01/2D/P4/Export/supplement-placements.json"));
            var env=_map.GetComponent<ChapterMapEnvironment>();env.enabled=false;env.Evaluate(0);
            var visibility=(float[])typeof(ChapterMapWorld).GetField("_visibility",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(_map.World);
            var saved=(float[])visibility.Clone();for(int i=0;i<visibility.Length;i++)visibility[i]=1;
            _map.World.SendMessage("LateUpdate");_map.World.enabled=false;_map.GetComponent<ChapterMapDetails>().RefreshVisibility();
            var canvas=_map.Hud.GetComponentInChildren<Canvas>();canvas.enabled=false;
            float size=_map.View.Camera.orthographicSize;
            try
            {
                foreach(int rate in new[]{30,60,120})
                {
                    DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1672,height=941,dpi=160,frameRate=rate});env.Evaluate(0);
                    for(int i=0;i<input.patches.Length;i++)
                    {
                        _map.View.Camera.orthographicSize=3;_map.View.FocusOn(input.patches[i].position);
                        yield return null;Capture("supplement-"+i+"-"+rate);
                    }
                }
                System.IO.File.Copy("ArtSource/Chapter01/2D/P4/Export/supplement-placements.json",CaptureDirectory+"/supplement-placements.json",true);
            }
            finally{saved.CopyTo(visibility,0);_map.View.Camera.orthographicSize=size;canvas.enabled=true;_map.World.enabled=true;env.enabled=true;DisplayMetrics.Reset();}
        }
        [UnityTest] public IEnumerator CaptureGroundDetailPresets()
        {
            var env=_map.GetComponent<ChapterMapEnvironment>();env.enabled=false;env.Evaluate(0);
            var visibility=(float[])typeof(ChapterMapWorld).GetField("_visibility",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(_map.World);
            var saved=(float[])visibility.Clone();for(int i=0;i<visibility.Length;i++)visibility[i]=1;
            try
            {
                foreach(int rate in new[]{30,60,120})
                {
                    DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1672,height=941,dpi=160,frameRate=rate});env.Evaluate(0);
                    foreach(int stage in new[]{0,1,2,3})
                    {
                        _map.View.FocusOn(_map.Config.stages[stage].center);
                        yield return null;yield return null;Capture("ground-"+stage+"-"+rate);
                    }
                }
            }
            finally{saved.CopyTo(visibility,0);env.enabled=true;DisplayMetrics.Reset();}
        }
        [UnityTest] public IEnumerator GroundDetailsRespectFogAndPreserveNavigationAndSource()
        {
            var config=_map.Config.details;Assert.NotNull(config);Assert.True(config.enabled);
            var details=_map.GetComponent<ChapterMapDetails>();Assert.AreEqual(7,details.RendererCount);
            Assert.Greater(config.patchCounts.Sum(),150);Assert.Less(config.stageMeshes.Sum(m=>m.vertexCount),4000);
            foreach(var mesh in config.stageMeshes)
            {
                Assert.Greater(mesh.uv.Select(v=>v.x).Distinct().Count(),1,"Atlas UV must span a sprite, not sample one transparent point.");
                Assert.Greater(mesh.uv.Select(v=>v.y).Distinct().Count(),1);
                Assert.True(mesh.uv.All(v=>v.x>0&&v.x<1&&v.y>0&&v.y<1));
            }
            var source=config.groundSource.GetTexture("_Sand");Assert.AreNotSame(source,config.groundReplacement.GetTexture("_Sand"));
            var progress=JsonUtility.ToJson(_map.Session.Progress);int colliders=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
            var renderers=details.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Ground details ")).ToArray();
            Assert.AreEqual(7,renderers.Length);Assert.True(renderers[0].enabled);Assert.True(renderers.Skip(1).All(r=>!r.enabled));
            var visibility=(float[])typeof(ChapterMapWorld).GetField("_visibility",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(_map.World);
            float old=visibility[1];visibility[1]=.5f;details.RefreshVisibility();var block=new MaterialPropertyBlock();renderers[1].GetPropertyBlock(block);
            Assert.AreEqual(.5f,block.GetFloat("_Visibility"));visibility[1]=old;
            foreach(int rate in new[]{30,60,120})
            {
                DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1672,height=941,dpi=160,frameRate=rate});yield return null;
                Assert.AreEqual(progress,JsonUtility.ToJson(_map.Session.Progress));Assert.AreEqual(colliders,Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length);
            }
            Object.Destroy(details);yield return null;
            Assert.True(renderers.All(r=>r==null));Assert.AreSame(source,config.groundSource.GetTexture("_Sand"));
            Assert.AreSame(config.groundSource,GameObject.Find("CH01 Exploration/Terrain").GetComponent<MeshRenderer>().sharedMaterial);
        }
        [UnityTest] public IEnumerator AudioHooksUseBoundedPoolAndOnlySuccessfulActions()
        {
            var audio=_map.Audio;var config=_map.Config.audio;Assert.NotNull(config);
            Assert.AreEqual(_map.Config.motion.maxAudioSources,audio.SourceCount);Assert.Greater(config.anchors.Length,10);
            Assert.True(config.cues.All(c=>c.clip==null),"Actual ambience clips have not been supplied; this tests silent attachment points only.");
            var progress=JsonUtility.ToJson(_map.Session.Progress);
            foreach(MapSound sound in System.Enum.GetValues(typeof(MapSound)))Assert.False(audio.Play(sound,Vector3.zero));
            Assert.AreEqual(0,audio.PlayingCount);Assert.AreEqual(progress,JsonUtility.ToJson(_map.Session.Progress));
            int accepted=audio.Requested(MapSound.AcceptQuest),selected=audio.Requested(MapSound.SelectNpc),chest=audio.Requested(MapSound.OpenChest);
            _map.SelectTarget(6,0);Assert.AreEqual(selected,audio.Requested(MapSound.SelectNpc),"Locked targets must stay silent.");
            _map.SelectTarget(0,0);Assert.AreEqual(selected+1,audio.Requested(MapSound.SelectNpc));_map.Motor.Cancel();
            _map.AcceptQuest(0);Assert.AreEqual(accepted+1,audio.Requested(MapSound.AcceptQuest));
            _map.AcceptQuest(0);Assert.AreEqual(accepted+1,audio.Requested(MapSound.AcceptQuest));
            Assert.False(_map.ClaimReward(0));Assert.AreEqual(chest,audio.Requested(MapSound.OpenChest));
            var reward=_map.Config.rewards[0];var original=reward.contents;
            try
            {
                reward.contents=new[]{new ResourceBalance{id="gold",amount=1}};
                for(int i=0;i<3;i++)_map.Session.Progress.Get(_map.Config.stages[i].id).stars=3;
                Assert.True(_map.ClaimReward(0));Assert.AreEqual(chest+1,audio.Requested(MapSound.OpenChest));
                Assert.False(_map.ClaimReward(0));Assert.AreEqual(chest+1,audio.Requested(MapSound.OpenChest));
            }
            finally{reward.contents=original;}
            var children=audio.GetComponentsInChildren<AudioSource>().Where(s=>s.name.StartsWith("Map audio ")).ToArray();
            Assert.AreEqual(audio.SourceCount,children.Length);Assert.True(children.All(s=>!s.ignoreListenerVolume&&!s.ignoreListenerPause));
            Object.Destroy(audio);yield return null;Assert.True(children.All(s=>s==null));
        }
    }
}
#endif
