#if UNITY_EDITOR
using System.Collections;
using System.Linq;
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
    public class LuBuAnimationTests
    {
        private BattleController _battle;
        private HeroUnit _hero;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;
            _battle = Object.FindAnyObjectByType<BattleController>();
            _battle.AutoSimulate = false;
            var index = Enumerable.Range(0, _battle.Cards.Count).First(i => _battle.Cards[i].HeroId == "lu_bu");
            Assert.That(_battle.TrySummon(SummonRequest.Quick(index)).Accepted, Is.True);
            _hero = _battle.GetHero(_battle.State.Cards[index].Hero);
            yield return null;
        }

        [Test]
        public void HasNamedFactionAndEveryDirectionalClip()
        {
            var hero = _hero.Definition;
            Assert.That(hero.Kind, Is.EqualTo(HeroKind.Named));
            Assert.That(hero.Faction, Is.Not.Null);
            foreach (HeroAnimationState state in System.Enum.GetValues(typeof(HeroAnimationState)))
            {
                var clip = hero.Animations.Find(state);
                Assert.That(clip, Is.Not.Null, state.ToString());
                Assert.That(clip.frames.Length, Is.EqualTo(hero.Animations.DirectionCount * clip.framesPerDirection));
                Assert.That(clip.frames.All(s => s != null), Is.True);
                Assert.That(clip.duration, Is.GreaterThan(0f));
                for (var direction = 0; direction < hero.Animations.DirectionCount; direction++)
                {
                    Assert.That(clip.Frame(direction, 0f), Is.EqualTo(clip.frames[direction * clip.framesPerDirection]));
                    if (!clip.loop)
                        Assert.That(clip.Frame(direction, clip.duration + 100f),
                            Is.EqualTo(clip.frames[(direction + 1) * clip.framesPerDirection - 1]));
                }
            }
        }

        [UnityTest]
        public IEnumerator MovingUsesRun_StunUsesIdle()
        {
            var body = _hero.transform.Find("Body").GetComponent<SpriteRenderer>();
            Assert.That(_battle.TrySelectHero(_hero.Id), Is.True);
            Assert.That(_battle.MoveSelectedHero(new Vector2(-6f, -8f)).Accepted, Is.True);
            _battle.Simulate(0.05f);
            yield return null;
            Assert.That(body.sprite.name, Does.StartWith("Run_"));
            _hero.ApplyStatus(StatusKind.Stun, 1f, 1f);
            _battle.Simulate(0.05f);
            yield return null;
            Assert.That(body.sprite.name, Does.StartWith("Idle_"));
        }

        [UnityTest]
        public IEnumerator CrowdNudgeDoesNotStartRunning_VisualOffsetIsBounded()
        {
            var body = _hero.transform.Find("Body").GetComponent<SpriteRenderer>();
            _hero.SetPosition(_hero.Position + Vector2.right * 0.1f);
            var position = _hero.Position;
            yield return null;
            Assert.That(_hero.LocomotionVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(body.sprite.name, Does.StartWith("Idle_"));
            Assert.That(_hero.Position, Is.EqualTo(position));
            Assert.That(Vector3.Distance(body.transform.position, _hero.transform.position),
                Is.LessThanOrEqualTo(_hero.Definition.Animations.MaxVisualOffset + 0.001f));
        }

        [UnityTest]
        public IEnumerator SlowMovementUsesWalk_RecoversToRun()
        {
            var body = _hero.transform.Find("Body").GetComponent<SpriteRenderer>();
            _battle.TrySelectHero(_hero.Id);
            Assert.That(_battle.MoveSelectedHero(new Vector2(-6f, -8f)).Accepted, Is.True);
            _hero.ApplyStatus(StatusKind.Slow, 0.75f, 0.15f);
            _battle.Simulate(0.05f);
            yield return null;
            Assert.That(body.sprite.name, Does.StartWith("Walk_"));
            for (var i = 0; i < 5; i++)
            {
                _battle.Simulate(0.05f);
                yield return null;
            }
            Assert.That(body.sprite.name, Does.StartWith("Run_"));
        }

        [UnityTest]
        public IEnumerator BasicAttackShowsTheStrike_AfterMovingIntoRange()
        {
            _hero.Skills.Get(SkillSlot.Skill2).CooldownLeft = 100f;
            _hero.Skills.Get(SkillSlot.Skill3).CooldownLeft = 100f;
            var castle = _battle.Registry.Get(_battle.EnemyCastleId);
            _hero.SetPosition(castle.Position + Vector2.left * (_hero.Radius + castle.Radius + _hero.AttackRange * 0.5f));
            Assert.That(_battle.AttackWithHero(_hero.Id, castle).Accepted, Is.True);
            var health = castle.Health;
            _battle.Simulate(0.05f);
            yield return null;
            Assert.That(castle.Health, Is.LessThan(health));
            var body = _hero.transform.Find("Body").GetComponent<SpriteRenderer>();
            Assert.That(body.sprite.name, Does.StartWith("Attack"));
        }

        [UnityTest]
        public IEnumerator DeathRemovesCombatUnitImmediately_LeavesAnimatedPicture()
        {
            var id = _hero.Id;
            var body = _hero.transform.Find("Body");
            var world = _hero.transform.parent;
            _hero.Die();
            Assert.That(_battle.Registry.IsAlive(id), Is.False);
            Assert.That(_battle.State.Cards[_hero.CardIndex].State, Is.EqualTo(CardState.Dead));
            yield return null;
            Assert.That(_hero == null, Is.True);
            Assert.That(body != null, Is.True);
            Assert.That(body.parent, Is.EqualTo(world));
            Assert.That(body.GetComponent<SpriteRenderer>().sprite.name, Does.StartWith("Defeat_"));
        }

        [UnityTest]
        public IEnumerator VictoryPlaysAfterSimulationStops()
        {
            var body = _hero.transform.Find("Body").GetComponent<SpriteRenderer>();
            _battle.Registry.Get(_battle.EnemyCastleId).TakeDamage(1000000f, _hero, true);
            _battle.Simulate(0.05f);
            yield return null;
            Assert.That(_battle.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(body.sprite.name, Does.StartWith("Victory_"));
            var before = _battle.BattleTime;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(_battle.BattleTime, Is.EqualTo(before));
            Assert.That(body.sprite, Is.Not.EqualTo(_hero.Definition.Animations.Find(HeroAnimationState.Victory).Frame(0, 0f)));
        }
    }
}
#endif
