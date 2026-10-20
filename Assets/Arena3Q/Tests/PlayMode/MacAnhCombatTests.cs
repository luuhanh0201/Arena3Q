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
    public class MacAnhCombatTests
    {
        private BattleController _battle;
        private HeroUnit _hero;
        private UnitBase _target;
        [UnitySetUp] public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Battle"); yield return null;
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
        [Test] public void BasicAttackHasNoEarlyDamage_ThenHitsOneCollision()
        {
            var health = _target.Health;
            _hero.Performance.BeginAttack(_target);
            for (var i = 0; i < 27; i++) _battle.Simulate(1f / 60f);
            Assert.That(_target.Health, Is.EqualTo(health), "Damage must wait for frame 14 and projectile collision.");
            for (var i = 0; i < 24; i++) _battle.Simulate(1f / 60f);
            Assert.That(_target.Health, Is.LessThan(health));
            var after = _target.Health;
            _hero.ApplyStatus(StatusKind.Stun, 1, 2);
            for (var i = 0; i < 60; i++) _battle.Simulate(1f / 60f);
            Assert.That(_target.Health, Is.EqualTo(after), "A projectile is consumed by its first collision.");
        }
        [Test] public void StunBeforeReleaseCancelsProjectileAndDamage()
        {
            var health = _target.Health;
            _hero.Performance.BeginAttack(_target); _battle.Simulate(.3f);
            _hero.ApplyStatus(StatusKind.Stun, 1, 2);
            for (var i = 0; i < 60; i++) _battle.Simulate(1f / 60f);
            Assert.That(_target.Health, Is.EqualTo(health));
            Assert.That(_hero.Performance.IsAttacking, Is.False);
        }
        [Test] public void SampledSocketsExistAndVfxPoolClearsOnRestart()
        {
            Assert.That(_hero.Performance.View.TrySocket(MacAnhSocket.FanTip, out var socket), Is.True);
            Assert.That(float.IsNaN(socket.x) || float.IsNaN(socket.y), Is.False);
            _hero.Performance.BeginAttack(_target); _hero.Performance.Tick(.48f);
            var pool = MacAnhFxPool.For(_battle); Assert.That(pool.ActiveCount, Is.GreaterThan(0));
            _battle.StartBattle(_battle.Stage, new[] { new LineupEntry(_hero.Definition, 60) });
            Assert.That(pool.ActiveCount, Is.Zero);
        }
        private void Equip(SkillSlot slot, string id)
        {
            var state = _hero.Skills.Get(slot);
            state.Definition = AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Arena3Q/Content/Skills/MacAnh/Skill_" + id + ".asset");
            Assert.That(state.Definition, Is.Not.Null); state.Level = 1; state.CooldownLeft = 0;
            _battle.AttackWithHero(_hero.Id, _target);
        }
        [Test] public void TripleFeatherReleasesThreeProjectiles_AtFrames15_24_33()
        {
            Equip(SkillSlot.Skill2, "tam_vu_lien_kich"); _battle.Simulate(.01f);
            var pool = MacAnhFxPool.For(_battle); _hero.Skills.Tick(.49f);
            Assert.That(pool.ActiveCount, Is.Zero);
            _hero.Skills.Tick(.011f); Assert.That(pool.ActiveCount, Is.EqualTo(1));
            _hero.Skills.Tick(.29f); Assert.That(pool.ActiveCount, Is.EqualTo(1));
            _hero.Skills.Tick(.011f); Assert.That(pool.ActiveCount, Is.EqualTo(2));
            _hero.Skills.Tick(.3f); Assert.That(pool.ActiveCount, Is.EqualTo(3));
            _hero.Skills.Tick(0); Assert.That(pool.ActiveCount, Is.EqualTo(3), "Re-entering the cast tick must not duplicate events.");
        }
        [Test] public void ShieldWaitsForFrame27_AndVisualStopsWhenShieldBreaks()
        {
            Equip(SkillSlot.Skill3, "thien_muu_ho_tran"); _battle.Simulate(.01f);
            _hero.Skills.Tick(.89f); Assert.That(_hero.Statuses.Shield, Is.Zero);
            _hero.Skills.Tick(.011f); Assert.That(_hero.Statuses.Shield, Is.GreaterThan(0));
            var pool = MacAnhFxPool.For(_battle); Assert.That(pool.ActiveCount, Is.EqualTo(1));
            _hero.TakeDamage(_hero.Statuses.Shield + 1, null, true); _hero.Performance.Tick(0);
            Assert.That(pool.ActiveCount, Is.Zero);
        }
        [Test] public void UltimateCannotDealDamageBeforeFrame75_StunPreservesRage()
        {
            _hero.Skills.Rage.Add(1000); var health = _target.Health;
            Assert.That(_hero.Skills.TryUltimate(_target.Position), Is.EqualTo(RejectReason.None));
            _hero.Skills.Tick(2.49f);
            Assert.That(_target.Health, Is.EqualTo(health)); Assert.That(_hero.Skills.Rage.IsFull, Is.True);
            _hero.ApplyStatus(StatusKind.Stun, 1, 1); _hero.Skills.Tick(.02f);
            Assert.That(_target.Health, Is.EqualTo(health)); Assert.That(_hero.Skills.Rage.IsFull, Is.True);
            Assert.That(MacAnhFxPool.For(_battle).ActiveCount, Is.Zero, "Cancelled telegraph must be reclaimed.");
        }
        [Test] public void UltimateHitsAtFrame75OnceAndConsumesRage()
        {
            _hero.Skills.Rage.Add(1000); var health = _target.Health;
            Assert.That(_hero.Skills.TryUltimate(_target.Position), Is.EqualTo(RejectReason.None));
            _hero.Skills.Tick(2.49f); Assert.That(_target.Health, Is.EqualTo(health));
            _hero.Skills.Tick(.011f); Assert.That(_target.Health, Is.LessThan(health));
            Assert.That(_hero.Skills.Rage.Value, Is.Zero);
            var after = _target.Health; _hero.Skills.Tick(0); Assert.That(_target.Health, Is.EqualTo(after));
        }
        [UnityTest] public IEnumerator ReleasedProjectileRemainsSafeAfterCasterDies()
        {
            var health = _target.Health; _hero.Performance.BeginAttack(_target); _hero.Performance.Tick(.47f);
            _hero.Die(); yield return null;
            for (var i = 0; i < 25; i++) _battle.Simulate(1f / 60f);
            Assert.That(_target.Health, Is.LessThan(health), "A released projectile retains its snapshot without a destroyed source.");
        }
        [UnityTest] public IEnumerator PooledEffectsPauseWithTheBattleClock()
        {
            var pool = MacAnhFxPool.For(_battle);
            pool.Spawn(_hero.Definition.MacAnhPerformance.Effect("Impact"), Vector3.zero, Vector3.one, Quaternion.identity,
                MacAnhFxQuality.Low, .05f, 10);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(pool.ActiveCount, Is.EqualTo(1), "Paused gameplay must not recycle an effect still owned by a cast.");
            _battle.Simulate(.1f); yield return null;
            Assert.That(pool.ActiveCount, Is.Zero);
        }
        [UnityTest] public IEnumerator CaptureAllBattleSkillPhasesWithSimulationClock()
        {yield return CaptureBattlePhases(false,"Logs/MacAnhBattleQa");}
        [UnityTest] public IEnumerator CaptureRealtimeBattleComparison()
        {yield return CaptureBattlePhases(true,"Logs/MacAnhRealtimeBattleQa");}
        private IEnumerator CaptureBattlePhases(bool realtime,string output)
        {
            Directory.CreateDirectory(output);
            var definition=_hero.Definition;
            var profile=new SerializedObject(definition.MacAnhPerformance);
            bool previousTrial=profile.FindProperty("enableBattle3DTrial").boolValue;
            profile.FindProperty("enableBattle3DTrial").boolValue=realtime;profile.ApplyModifiedPropertiesWithoutUndo();
            try
            {
            var camera=Camera.main;
            var controller=camera.GetComponent<BattleCameraController>();
            if(controller!=null)controller.enabled=false;
            var actions=new[]{MacAnhAction.Idle,MacAnhAction.Run,MacAnhAction.Attack01,MacAnhAction.Attack02,MacAnhAction.Skill01,
                MacAnhAction.Skill02,MacAnhAction.Skill03,MacAnhAction.Ultimate,MacAnhAction.DodgeAttack};
            foreach(var action in actions)
            {
                _battle.StartBattle(_battle.Stage,new[]{new LineupEntry(definition,1)});
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
                var times=locomotion?new[]{.1f,.3f,.5f}:action==MacAnhAction.Ultimate?new[]{.8f,1.5f,2.52f,2.75f,3.4f,4.2f}:
                    action==MacAnhAction.DodgeAttack?new[]{.28f,.35f,.42f,.82f,.95f,1.3f}:
                    action==MacAnhAction.Attack01?new[]{.33f,.49f,.6f,.75f,1.1f}:
                    new[]{.35f,.52f,.82f,1.02f,1.25f,1.65f,2.4f,3.2f};
                float elapsed=0;int activeSamples=0;
                foreach(float time in times)
                {
                    while(elapsed+1e-5f<time)
                    {float step=Mathf.Min(1f/60f,time-elapsed);_battle.Simulate(step);elapsed+=step;}
                    yield return null;
                    if(MacAnhFxPool.For(_battle).ActiveCount>0)activeSamples++;
                    var focus=Vector3.Lerp(_hero.transform.position,_target.transform.position,.35f)+Vector3.up*.65f;
                    camera.transform.position=new Vector3(focus.x,focus.y,-10);
                    foreach(float zoom in new[]{4.5f,3.5f})
                    {
                        camera.orthographicSize=zoom;
                        CaptureCamera(camera,output+"/"+action+"_t"+Mathf.RoundToInt(time*1000)+"_zoom"+Mathf.RoundToInt(zoom*10)+".png");
                    }
                }
                if(!locomotion)Assert.That(activeSamples,Is.GreaterThan(0),action+" must create visible-lifetime effects with the real battle clock.");
            }
            File.WriteAllText(output+"/Complete.txt","Idle/Run + seven skills, preparation/release/flight/impact/recovery samples, zoom 4.5 and 3.5. World camera captures; HUD excluded. Shield/stun fixture keeps subjects alive for recovery.\n"+System.DateTime.UtcNow.ToString("O"));
            }
            finally
            {
                profile.Update();profile.FindProperty("enableBattle3DTrial").boolValue=previousTrial;
                profile.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        [UnityTest] public IEnumerator NewEffectDoesNotInheritTimeBeforeItWasSpawned()
        {
            _hero.ApplyStatus(StatusKind.Stun,1,3);
            var pool=MacAnhFxPool.For(_battle);
            _battle.Simulate(1f);
            var fx=pool.Spawn(_hero.Definition.MacAnhPerformance.Effect("Impact"),Vector3.zero,
                Vector3.one,Quaternion.identity,MacAnhFxQuality.Low,.15f,10);
            _battle.Simulate(.02f);yield return null;
            Assert.That(fx.IsRunning,Is.True,"A newly spawned effect must not inherit the previous second of simulation.");
            _battle.Simulate(.2f);yield return null;
            Assert.That(fx.IsRunning,Is.False);
        }
        [UnityTest] public IEnumerator SpatialAfterimageKeepsPoseUprightAndFadesOnBattleClock()
        {
            _hero.ApplyStatus(StatusKind.Stun,1,5);
            var pool=MacAnhFxPool.For(_battle);
            var fx=pool.Spawn(_hero.Definition.MacAnhPerformance.Effect("Afterimage"),_hero.transform.position,
                Vector3.one,Quaternion.identity,MacAnhFxQuality.Low,.35f,10);
            fx.SetAfterimage(_hero.Performance.View.Body.sprite,_hero.Performance.View.Body.sharedMaterial,10);
            _battle.Simulate(.1f);yield return null;
            var pose=fx.GetComponentInChildren<SpriteRenderer>();
            // Upright = facing the camera (the battle's billboard rotation; identity in the flat view).
            Assert.That(Quaternion.Angle(pose.transform.rotation,_battle.UprightRotation),Is.LessThan(.01f));
            float bright=pose.color.a;Assert.That(bright,Is.GreaterThan(.1f));
            yield return new WaitForSecondsRealtime(.1f);Assert.That(pose.color.a,Is.EqualTo(bright).Within(.001f));
            _battle.Simulate(.18f);yield return null;Assert.That(pose.color.a,Is.LessThan(bright));
            _battle.Simulate(.1f);yield return null;Assert.That(fx.IsRunning,Is.False);
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
