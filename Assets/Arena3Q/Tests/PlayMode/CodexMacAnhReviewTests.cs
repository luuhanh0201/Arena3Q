#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.IO;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Arena3Q.Tests.PlayMode
{
public class CodexMacAnhReviewTests
{

private BattleController _battle;
private HeroUnit _hero;
private UnitBase _target;
[UnitySetUp]
public IEnumerator SetUp()
        {
            Random.InitState(210021); yield return SceneManager.LoadSceneAsync("Battle"); yield return null;
            _battle = Object.FindAnyObjectByType<BattleController>(); _battle.AutoSimulate = false;
            var definition = AssetDatabase.LoadAssetAtPath<HeroDefinition>("Assets/Arena3Q/Content/Lords/Lord_MacAnhPreview.asset");
            Assert.That(definition, Is.Not.Null);
            var spare = AssetDatabase.LoadAssetAtPath<HeroDefinition>("Assets/Arena3Q/Content/Heroes/Hero_Swordsman.asset");
            _battle.StartBattle(_battle.Stage, new[] { new LineupEntry(definition, 60), new LineupEntry(spare) });
            Assert.That(_battle.TrySummon(SummonRequest.Quick(0)).Accepted, Is.True);
            _hero = _battle.GetHero(_battle.State.Cards[0].Hero);
            _hero.Skills.Get(SkillSlot.Skill2).CooldownLeft = _hero.Skills.Get(SkillSlot.Skill3).CooldownLeft = 100;
            _target = _battle.Registry.Get(_battle.EnemyCastleId);
            _hero.SetPosition(_target.Position + Vector2.left * (_hero.Radius + _target.Radius + 2));
            _battle.TrySelectHero(_hero.Id);
            // A move order keeps the brain from starting a second autonomous attack during assertions.
            _battle.MoveSelectedHero(_hero.Position + Vector2.left * .1f);
            yield return null;
        }

        private const string ReviewRoot="Logs/CodexFix/20261021/AfterAfterimage";
        [UnityTest] public IEnumerator CaptureExtendedAllTiers()
        {
            yield return CaptureBattlePhases(false,ReviewRoot+"/Sprite_Q1",1);
            yield return CaptureBattlePhases(true,ReviewRoot+"/Realtime_Q1",1);
        }
        private static float[] ReviewTimes(MacAnhAction action,MacAnhPerformance set)
        {
            if(action==MacAnhAction.Attack01)return new[]{.33f,.47f,.6f,.75f,.9f,1.1f,1.4f};
            if(action==MacAnhAction.Attack02)return new[]{.35f,.52f,.82f,1.1f,1.4f,1.8f,2.5f};
            if(action==MacAnhAction.DodgeAttack)return new[]{.25f,.28f,.35f,.42f,.82f,.95f,1.3f};
            if(action==MacAnhAction.Ultimate)return new[]{.8f,1.03f,1.5f,2.49f,2.52f,2.75f,3f,3.2f,3.4f,4.2f};
            var skill=set.Skills.First(b=>b.action==action).skill;
            float duration=skill.ZoneDuration;
            if(action==MacAnhAction.Skill03)duration=skill.Effects.Where(e=>e.kind==SkillEffectKind.Shield).Max(e=>e.duration);
            float end=skill.CastTime+duration;
            return new[]{.35f,skill.CastTime-.08f,skill.CastTime+.02f,skill.CastTime+.3f,3.2f,end-.4f,end-.1f,end+.2f};
        }
        [UnityTest] public IEnumerator ShaderNoisePausesWithSimulationClock()
        {
            var fx=MacAnhFxPool.For(_battle).Spawn(_hero.Definition.MacAnhPerformance.Effect("WindSlash"),Vector3.zero,
                Vector3.one,Quaternion.identity,MacAnhFxQuality.Medium,3f,100);
            _battle.Simulate(.3f);yield return null;
            foreach(var t in fx.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            var go=new GameObject("Codex isolated VFX camera",typeof(Camera));var camera=go.GetComponent<Camera>();
            camera.enabled=false;camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=2;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.08f,.08f,1);camera.cullingMask=1<<31;
            Directory.CreateDirectory(ReviewRoot+"/Pause");
            try
            {
                float clock=_battle.BattleTime;
                var a=IsolatedPixels(camera,ReviewRoot+"/Pause/Before.png");
                var control=IsolatedPixels(camera,ReviewRoot+"/Pause/SameFrame.png");
                yield return new WaitForSecondsRealtime(.5f);
                var b=IsolatedPixels(camera,ReviewRoot+"/Pause/AfterRealtimePause.png");
                int changed=0,controlChanged=0,max=0,visible=0;
                for(int i=0;i<a.Length;i++)
                {
                    int delta=Mathf.Abs(a[i]-b[i]);max=Mathf.Max(max,delta);if(delta>2)changed++;
                    if(Mathf.Abs(a[i]-control[i])>2)controlChanged++;
                    if(i%4!=3&&a[i]>70)visible++;
                }
                File.WriteAllText(ReviewRoot+"/Pause/Result.json","{\"battleTimeBefore\":"+clock.ToString(System.Globalization.CultureInfo.InvariantCulture)+
                    ",\"battleTimeAfter\":"+_battle.BattleTime.ToString(System.Globalization.CultureInfo.InvariantCulture)+
                    ",\"changedChannelsAbove2\":"+changed+",\"sameFrameChangedChannels\":"+controlChanged+",\"maxByteDelta\":"+max+",\"visibleChannels\":"+visible+"}");
                Assert.That(visible,Is.GreaterThan(100),"The isolated camera must actually render the effect");
                Assert.That(_battle.BattleTime,Is.EqualTo(clock));
                Assert.That(changed,Is.EqualTo(0),"Noise/dissolve must freeze while battle simulation is paused");
            }
            finally{Object.DestroyImmediate(go);fx.Stop();}
        }
        private static byte[] IsolatedPixels(Camera camera,string path)
        {
            var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var image=new Texture2D(256,256,TextureFormat.RGBA32,false);
            try
            {
                rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
                return image.GetRawTextureData<byte>().ToArray();
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
        [UnityTest] public IEnumerator FadeOutUsesBattleClockAndPoolReuseResetsVanish()
        {
            var pool=MacAnhFxPool.For(_battle);var prefab=_hero.Definition.MacAnhPerformance.Effect("WindSlash");
            var fx=pool.Spawn(prefab,Vector3.zero,Vector3.one,Quaternion.identity,MacAnhFxQuality.Medium,2f,100);
            int generation=fx.Generation;_battle.Simulate(.3f);yield return null;
            fx.FadeOut(.16f);yield return new WaitForSecondsRealtime(.25f);
            Assert.That(fx.IsRunning,Is.True,"A real-time pause cannot reclaim the fading effect");
            _battle.Simulate(.08f);yield return null;Assert.That(fx.IsRunning,Is.True);
            _battle.Simulate(.1f);yield return null;Assert.That(fx.IsRunning,Is.False);
            var reused=pool.Spawn(prefab,Vector3.zero,Vector3.one,Quaternion.identity,MacAnhFxQuality.Medium,2f,100);
            Assert.That(reused,Is.SameAs(fx));Assert.That(reused.Generation,Is.GreaterThan(generation));
            _battle.Simulate(.35f);yield return null;Assert.That(reused.IsRunning,Is.True);
            Assert.That(reused.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled&&r.name=="Frames"),Is.True,"Reused effect must draw after its previous vanish deadline");
            reused.Stop();
        }
        [UnityTest] public IEnumerator CancelAfterAuraSpawnThenReuseDoesNotStopNewGeneration()
        {
            _hero.ApplyStatus(StatusKind.Shield,100000,20);_target.ApplyStatus(StatusKind.Shield,100000,20);
            var binding=_hero.Definition.MacAnhPerformance.Skills.First(b=>b.action==MacAnhAction.Ultimate);
            var state=_hero.Skills.Get(binding.skill.Slot);state.Definition=binding.skill;state.Level=1;state.CooldownLeft=0;
            _hero.Skills.Rage.Add(1000);Assert.That(_hero.Skills.TryUltimate(_target.Position),Is.EqualTo(RejectReason.None));
            for(int i=0;i<90;i++)_battle.Simulate(1f/60f);yield return null;
            var aura=Object.FindObjectsByType<MacAnhFx>(FindObjectsSortMode.None).FirstOrDefault(f=>f.IsRunning&&f.Kind==MacAnhFxKind.UltimateAura);
            Assert.That(aura,Is.Not.Null,"Cancel must be tested after the aura actually exists");
            int oldGeneration=aura.Generation;_hero.ApplyStatus(StatusKind.Stun,1,2);_battle.Simulate(.02f);
            // It disperses over a short fade (never cut off, user 2026-10-22), then the pool has it back.
            for(int i=0;i<24;i++)_battle.Simulate(1f/60f);yield return null;
            Assert.That(aura.IsRunning&&aura.Generation==oldGeneration,Is.False,"Stun/cancel must reclaim the spawned aura");
            var reused=MacAnhFxPool.For(_battle).Spawn(_hero.Definition.MacAnhPerformance.Effect("UltimateAura"),Vector3.zero,
                Vector3.one,Quaternion.identity,MacAnhFxQuality.Medium,1f,100);
            Assert.That(reused,Is.SameAs(aura));Assert.That(reused.Generation,Is.GreaterThan(oldGeneration));
            _hero.Performance.Cancel();_battle.Simulate(.2f);yield return null;
            Assert.That(reused.IsRunning,Is.True,"Cancel from the previous owner cannot stop a reused generation");reused.Stop();
        }


        [Test] public void CapturedAfterimageHidesLegacyMeshesAndReuseRestoresThem()
        {
            var pool=MacAnhFxPool.For(_battle);
            var prefab=_hero.Definition.MacAnhPerformance.Effect("Afterimage");
            var fx=pool.Spawn(prefab,Vector3.zero,Vector3.one,Quaternion.identity,MacAnhFxQuality.Medium,1f,100);
            var layers=fx.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(layers.Length,Is.EqualTo(3),"The regression must exercise the legacy mesh prefab");
            var pose=new GameObject("Captured test pose",typeof(MeshRenderer));
            int cleanups=0;
            fx.SetCapturedAfterimage(pose,()=>{cleanups++;Object.DestroyImmediate(pose);});
            Assert.That(layers.Count(l=>l.gameObject.activeSelf),Is.Zero,"Captured route must hide all three legacy rings");
            fx.Stop();Assert.That(cleanups,Is.EqualTo(1));
            var reused=pool.Spawn(prefab,Vector3.zero,Vector3.one,Quaternion.identity,MacAnhFxQuality.Low,1f,100);
            Assert.That(reused,Is.SameAs(fx));
            Assert.That(layers.Count(l=>l.gameObject.activeSelf),Is.EqualTo(2),"Begin restores normal mesh layers for the new quality");
            reused.Stop();Assert.That(cleanups,Is.EqualTo(1),"Snapshot cleanup cannot leak to the next generation");
        }

private IEnumerator CaptureBattlePhases(bool realtime,string output,int quality)
        {
            Directory.CreateDirectory(output);
var timeline=new System.Collections.Generic.List<string>{"action,time,activeKinds"};
            var definition=_hero.Definition;
            var profile=new SerializedObject(definition.MacAnhPerformance);
            int previousQuality=profile.FindProperty("effectQuality").intValue;
profile.FindProperty("effectQuality").intValue=quality;
bool previousTrial=profile.FindProperty("enableBattle3DTrial").boolValue;
            profile.FindProperty("enableBattle3DTrial").boolValue=realtime;profile.ApplyModifiedPropertiesWithoutUndo();
            // The sprite route only exists in the flat 2.5D view; the 3D battle always shows the real model.
            if(!realtime){var flat=BattleViewConfig.CreateDefault();flat.SetPresentation3D(false);_battle.UseView(flat);}
            try
            {
            var camera=Camera.main;
            var controller=camera.GetComponent<BattleCameraController>();
            if(controller!=null)controller.enabled=false;
            var actions=new[]{MacAnhAction.DodgeAttack};
            foreach(var action in actions)
            {
                _battle.StartBattle(_battle.Stage,new[]{new LineupEntry(definition,1)});
                BattleView3D.ConfigureCamera(Camera.main,_battle);
                Assert.That(_battle.TrySummon(SummonRequest.Quick(0)).Accepted,Is.True);
                _hero=_battle.GetHero(_battle.State.Cards[0].Hero);
                _target=_battle.Registry.Get(_battle.EnemyCastleId);
                _hero.SetPosition(_target.Position+Vector2.left*(_hero.Radius+_target.Radius+2));
                // Keep this visual capture workload alive through recovery;
                // ordinary combat damage/cancellation remains covered above.
                _hero.ApplyStatus(StatusKind.Shield,100000,20);
                _target.ApplyStatus(StatusKind.Shield,100000,20);
                foreach(var unit in _battle.Registry.Units)
                    if(unit!=null && unit.Team!=_hero.Team)unit.ApplyStatus(StatusKind.Stun,1,20);
                foreach(SkillSlot slot in System.Enum.GetValues(typeof(SkillSlot)))
                {var state=_hero.Skills.Get(slot);state.CooldownLeft=1000;state.Level=0;}
                _battle.TrySelectHero(_hero.Id);
                yield return null;
                Assert.That(_hero.Performance.View.Body.enabled,Is.EqualTo(!realtime),"Requested presentation route must initialize.");
                bool locomotion=action==MacAnhAction.Idle||action==MacAnhAction.Run;
                if(action==MacAnhAction.Idle)_hero.ApplyStatus(StatusKind.Stun,1,5);
                else if(action==MacAnhAction.Run)_battle.MoveSelectedHero(_hero.Position+Vector2.left*3);
                else if(action==MacAnhAction.Attack01)_hero.Performance.BeginAttack(_target);
                else
                {
                    var binding=definition.MacAnhPerformance.Skills.First(b=>b.action==action);
                    var state=_hero.Skills.Get(binding.skill.Slot);
                    state.Definition=binding.skill;state.Level=1;state.CooldownLeft=0;
                    if(binding.skill.Slot==SkillSlot.Ultimate)
                    {
                        _hero.Skills.Rage.Add(1000);
                        Assert.That(_hero.Skills.TryUltimate(_target.Position),Is.EqualTo(RejectReason.None));
                    }
                    else _battle.AttackWithHero(_hero.Id,_target);
                }
                var times=ReviewTimes(action,definition.MacAnhPerformance);
float elapsed=0;int activeSamples=0;
                foreach(float time in times)
                {
                    while(elapsed+1e-5f<time)
                    {float step=Mathf.Min(1f/60f,time-elapsed);_battle.Simulate(step);elapsed+=step;}
                    yield return null;
                    if(MacAnhFxPool.For(_battle).ActiveCount>0)activeSamples++;
                    var focus=Vector3.Lerp(_hero.transform.position,_target.transform.position,.35f)+Vector3.up*.65f;
                    if(_battle.Presentation3D)
                        BattleView3D.PlaceCamera(camera.transform,_battle,Vector2.Lerp(_hero.Position,_target.Position,.35f)+Vector2.up*(.65f/_battle.GroundProjection));
                    else camera.transform.position=new Vector3(focus.x,focus.y,-10);
                    timeline.Add(action+","+time.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+
string.Join(";",Object.FindObjectsByType<MacAnhFx>(FindObjectsSortMode.None).Where(f=>f.IsRunning).Select(f=>f.Kind+":"+f.Generation)));
foreach(float zoom in new[]{4.5f})
                    {
                        camera.orthographicSize=zoom;
                        CaptureCamera(camera,output+"/"+action+"_t"+Mathf.RoundToInt(time*1000)+"_zoom"+Mathf.RoundToInt(zoom*10)+".png");
                    }
                }
                File.WriteAllLines(output+"/Timeline.csv",timeline);
if(action==MacAnhAction.Skill01||action==MacAnhAction.Skill02||action==MacAnhAction.Skill03||action==MacAnhAction.Ultimate)
{
var expected=action==MacAnhAction.Skill01?MacAnhFxKind.MagicArray:action==MacAnhAction.Skill02?MacAnhFxKind.Cyclone:action==MacAnhAction.Skill03?MacAnhFxKind.Shield:MacAnhFxKind.Ultimate;
Assert.That(Object.FindObjectsByType<MacAnhFx>(FindObjectsSortMode.None).Count(f=>f.IsRunning&&f.Kind==expected),Is.Zero,action+" must be reclaimed after its configured lifetime");
}
if(!locomotion)Assert.That(activeSamples,Is.GreaterThan(0),action+" must create visible-lifetime effects with the real battle clock.");
            }
            File.WriteAllText(output+"/Complete.txt","Seven skills, preparation/release/peak plus configured lifetime fade/cleanup, zoom 4.5. World camera captures; HUD excluded. Shield/stun fixture keeps subjects alive for recovery.\n"+System.DateTime.UtcNow.ToString("O"));
            }
            finally
            {
                profile.Update();profile.FindProperty("effectQuality").intValue=previousQuality;profile.FindProperty("enableBattle3DTrial").boolValue=previousTrial;
                profile.ApplyModifiedPropertiesWithoutUndo();
                _battle.UseView(null);
            }
        }
private static void CaptureCamera(Camera camera,string path)
        {
            var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(1440,810,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var image=new Texture2D(1440,810,TextureFormat.RGB24,false);
            try
            {
                rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,1440,810),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
}
}
#endif



