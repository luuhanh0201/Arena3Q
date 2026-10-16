using System.Collections.Generic;
using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public class HeroBrainTests
    {
        private static readonly UnitId Hero = new UnitId(1);
        private static readonly UnitId EnemyA = new UnitId(10);
        private static readonly UnitId EnemyB = new UnitId(11);
        private static readonly UnitId Castle = new UnitId(20);
        private static readonly Vector2 Destination = new Vector2(10f, 0f);

        private FakeHeroSensor _sensor;

        [SetUp]
        public void SetUp()
        {
            _sensor = new FakeHeroSensor { EnemyCastle = Castle };
        }

        private static HeroBrain Brain(StageType stageType = StageType.Attack, float idleSeconds = 5f,
            bool resetOnOutOfRangeDamage = false, bool selfDefendWhileAttacking = true) =>
            new HeroBrain(new HeroBrainSettings
            {
                StageType = stageType,
                IdleBeforeAdvanceSeconds = idleSeconds,
                IdleTimerResetsOnOutOfRangeDamage = resetOnOutOfRangeDamage,
                SelfDefendWhileAttacking = selfDefendWhileAttacking
            });

        private HeroIntent Tick(HeroBrain brain, float seconds, float step = 0.1f)
        {
            var intent = HeroIntent.Hold;
            var steps = Mathf.RoundToInt(seconds / step);
            for (var i = 0; i < steps; i++)
                intent = brain.Tick(step, _sensor);
            return intent;
        }

        // ---- Idle and the 5-second rule ----

        [Test]
        public void NewHero_StartsIdle()
        {
            Assert.That(Brain().State, Is.EqualTo(HeroState.Idle));
        }

        [Test]
        public void Idle_NoTargetFor5Seconds_StartsAdvancing()
        {
            var brain = Brain();

            Tick(brain, 4.9f);
            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));

            var intent = Tick(brain, 0.2f);
            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Engage));
            Assert.That(intent.Target, Is.EqualTo(Castle), "Attack stage advances towards the enemy castle");
        }

        [Test]
        public void Idle_AttacksEnemyInRangeWithoutChasing()
        {
            var brain = Brain();
            _sensor.EnemyInAttackRange = EnemyA;

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Engage));
            Assert.That(intent.Target, Is.EqualTo(EnemyA));
            Assert.That(intent.MayChase, Is.False);
            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));
        }

        [Test]
        public void Idle_DoesNotChaseEnemyOnlyInAggroRange()
        {
            var brain = Brain();
            _sensor.EnemyInAggroRange = EnemyA;

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Hold));
        }

        [Test]
        public void Idle_EngagementResetsTimer_ThenNeedsFull5SecondsAgain()
        {
            var brain = Brain();
            Tick(brain, 4f);

            _sensor.EnemyInAttackRange = EnemyA;
            Tick(brain, 2f);
            Assert.That(brain.IdleTimer, Is.Zero, "timer stays at zero while a target is present");

            _sensor.Kill(EnemyA);
            Tick(brain, 4.9f);
            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));

            Tick(brain, 0.2f);
            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
        }

        [Test]
        public void Idle_OutOfRangeDamage_DoesNotResetTimer_ByDefault()
        {
            var brain = Brain();
            _sensor.TookDamageFromOutOfRange = true;

            Tick(brain, 5.1f);

            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
        }

        [Test]
        public void Idle_OutOfRangeDamage_ResetsTimer_WhenOptionEnabled()
        {
            var brain = Brain(resetOnOutOfRangeDamage: true);
            _sensor.TookDamageFromOutOfRange = true;

            Tick(brain, 10f);

            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));
            Assert.That(brain.IdleTimer, Is.Zero);
        }

        [Test]
        public void Idle_NewCommand_CancelsTimer()
        {
            var brain = Brain();
            Tick(brain, 4f);

            brain.Issue(new MoveCommand(Hero, Destination));

            Assert.That(brain.State, Is.EqualTo(HeroState.Moving));
            Assert.That(brain.IdleTimer, Is.Zero);
        }

        [Test]
        public void IdleDelay_IsConfigurable()
        {
            var brain = Brain(idleSeconds: 2f);

            Tick(brain, 2.1f);

            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
        }

        // ---- Moving: IgnoreUntilDestination ----

        [Test]
        public void Moving_IgnoresEnemiesInRange()
        {
            var brain = Brain();
            brain.Issue(new MoveCommand(Hero, Destination));
            _sensor.EnemyInAttackRange = EnemyA;
            _sensor.EnemyInAggroRange = EnemyA;

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Moving));
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.MoveTo));
            Assert.That(intent.Destination, Is.EqualTo(Destination));
        }

        [Test]
        public void Moving_ShotFromFar_KeepsWalking()
        {
            var brain = Brain();
            brain.Issue(new MoveCommand(Hero, Destination));
            _sensor.TookDamageFromOutOfRange = true;

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Moving));
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.MoveTo));
        }

        [Test]
        public void Moving_AttackedUpClose_DefendsThenResumesToSavedDestination()
        {
            var brain = Brain();
            brain.Issue(new MoveCommand(Hero, Destination));

            _sensor.CloseAttacker = EnemyA;
            var defend = brain.Tick(0.1f, _sensor);
            Assert.That(brain.State, Is.EqualTo(HeroState.Combat));
            Assert.That(defend.Target, Is.EqualTo(EnemyA));

            _sensor.Kill(EnemyA);
            var resume = brain.Tick(0.1f, _sensor);
            Assert.That(brain.State, Is.EqualTo(HeroState.Moving));
            Assert.That(resume.Kind, Is.EqualTo(HeroIntentKind.MoveTo));
            Assert.That(resume.Destination, Is.EqualTo(Destination));
        }

        [Test]
        public void Combat_SwitchesToNextCloseAttacker()
        {
            var brain = Brain();
            brain.Issue(new MoveCommand(Hero, Destination));
            _sensor.CloseAttacker = EnemyA;
            brain.Tick(0.1f, _sensor);

            _sensor.CloseAttacker = EnemyB;
            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Combat));
            Assert.That(intent.Target, Is.EqualTo(EnemyB));
        }

        [Test]
        public void Moving_ArrivesAtDestination_GoesIdle_ThenAdvancesAfter5Seconds()
        {
            var brain = Brain();
            brain.Issue(new MoveCommand(Hero, Destination));

            _sensor.Position = Destination;
            var intent = brain.Tick(0.1f, _sensor);
            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Hold));
            Assert.That(brain.CurrentCommand, Is.EqualTo(HeroCommandKind.None));

            Tick(brain, 5.1f);
            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
        }

        [Test]
        public void NewMoveCommand_ReplacesDestination()
        {
            var brain = Brain();
            brain.Issue(new MoveCommand(Hero, Destination));
            var other = new Vector2(-5f, 3f);

            brain.Issue(new MoveCommand(Hero, other));
            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(intent.Destination, Is.EqualTo(other));
        }

        [Test]
        public void CommandDuringCombat_ReplacesInterruptedOrder()
        {
            var brain = Brain();
            brain.Issue(new MoveCommand(Hero, Destination));
            _sensor.CloseAttacker = EnemyA;
            brain.Tick(0.1f, _sensor);

            brain.Issue(new AttackCommand(Hero, Castle));
            _sensor.CloseAttacker = UnitId.None;
            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Attacking));
            Assert.That(intent.Target, Is.EqualTo(Castle));
        }

        // ---- Attacking ----

        [Test]
        public void Attacking_ChasesTargetIgnoringOtherEnemies()
        {
            var brain = Brain();
            brain.Issue(new AttackCommand(Hero, EnemyA));
            _sensor.EnemyInAttackRange = EnemyB;

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Attacking));
            Assert.That(intent.Target, Is.EqualTo(EnemyA));
            Assert.That(intent.MayChase, Is.True);
        }

        [Test]
        public void Attacking_TargetDies_GoesIdle()
        {
            var brain = Brain();
            brain.Issue(new AttackCommand(Hero, EnemyA));
            brain.Tick(0.1f, _sensor);

            _sensor.Kill(EnemyA);
            brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));
            Assert.That(brain.AttackTarget, Is.EqualTo(UnitId.None));
        }

        [Test]
        public void Attacking_CloseAttacker_DefendsThenResumesAttack()
        {
            var brain = Brain();
            brain.Issue(new AttackCommand(Hero, Castle));

            _sensor.CloseAttacker = EnemyA;
            brain.Tick(0.1f, _sensor);
            Assert.That(brain.State, Is.EqualTo(HeroState.Combat));

            _sensor.Kill(EnemyA);
            var intent = brain.Tick(0.1f, _sensor);
            Assert.That(brain.State, Is.EqualTo(HeroState.Attacking));
            Assert.That(intent.Target, Is.EqualTo(Castle));
        }

        [Test]
        public void Attacking_CloseAttackerIsTheTarget_StaysAttacking()
        {
            var brain = Brain();
            brain.Issue(new AttackCommand(Hero, EnemyA));
            _sensor.CloseAttacker = EnemyA;

            brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Attacking));
        }

        [Test]
        public void Attacking_SelfDefenceCanBeDisabled()
        {
            var brain = Brain(selfDefendWhileAttacking: false);
            brain.Issue(new AttackCommand(Hero, Castle));
            _sensor.CloseAttacker = EnemyA;

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Attacking));
            Assert.That(intent.Target, Is.EqualTo(Castle));
        }

        // ---- Advancing ----

        [Test]
        public void Advancing_FightsEnemyInAggroUntilItDies_ThenContinues()
        {
            var brain = Brain(idleSeconds: 0f);
            _sensor.EnemyInAggroRange = EnemyA;
            Assert.That(brain.Tick(0.1f, _sensor).Target, Is.EqualTo(EnemyA));

            _sensor.EnemyInAggroRange = EnemyB;
            Assert.That(brain.Tick(0.1f, _sensor).Target, Is.EqualTo(EnemyA), "keeps its target until it dies");

            _sensor.Kill(EnemyA);
            Assert.That(brain.Tick(0.1f, _sensor).Target, Is.EqualTo(EnemyB));

            _sensor.Kill(EnemyB);
            Assert.That(brain.Tick(0.1f, _sensor).Target, Is.EqualTo(Castle));
            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
        }

        [Test]
        public void Advancing_DefenseStage_HeadsForNearestEnemy()
        {
            var brain = Brain(StageType.Defense, idleSeconds: 0f);
            _sensor.NearestEnemy = EnemyB;

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
            Assert.That(intent.Target, Is.EqualTo(EnemyB));
            Assert.That(intent.Target, Is.Not.EqualTo(Castle), "Defense stages do not march on the enemy castle");
        }

        [Test]
        public void Advancing_DefenseStage_NoEnemies_StandsIdle()
        {
            var brain = Brain(StageType.Defense, idleSeconds: 0f);

            var intent = brain.Tick(0.1f, _sensor);
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Hold));
            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));

            // A zero idle delay must not loop Idle <-> Advancing forever.
            Assert.DoesNotThrow(() => Tick(brain, 1f));
        }

        [Test]
        public void Advancing_NewCommandTakesOver()
        {
            var brain = Brain(idleSeconds: 0f);
            brain.Tick(0.1f, _sensor);

            brain.Issue(new MoveCommand(Hero, Destination));

            Assert.That(brain.State, Is.EqualTo(HeroState.Moving));
        }

        // ---- Dead ----

        [Test]
        public void Dead_IgnoresCommandsAndHolds()
        {
            var brain = Brain();
            brain.Issue(new AttackCommand(Hero, EnemyA));

            brain.Kill();
            brain.Issue(new MoveCommand(Hero, Destination));
            _sensor.EnemyInAttackRange = EnemyB;
            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Dead));
            Assert.That(brain.CurrentCommand, Is.EqualTo(HeroCommandKind.None));
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Hold));
        }

        [Test]
        public void StateChanged_ReportsTransitions()
        {
            var brain = Brain();
            var transitions = new List<(HeroState, HeroState)>();
            brain.StateChanged += (from, to) => transitions.Add((from, to));

            brain.Issue(new MoveCommand(Hero, Destination));
            _sensor.Position = Destination;
            brain.Tick(0.1f, _sensor);
            brain.Kill();

            Assert.That(transitions, Is.EqualTo(new[]
            {
                (HeroState.Idle, HeroState.Moving),
                (HeroState.Moving, HeroState.Idle),
                (HeroState.Idle, HeroState.Dead)
            }));
        }

        [Test]
        public void HeroRuntime_SyncFrom_CopiesBehaviourButNotDeath()
        {
            var state = TestBattles.State();
            var runtime = TestBattles.Summon(state, 0);
            var brain = Brain();
            brain.Issue(new MoveCommand(runtime.Id, Destination));

            runtime.SyncFrom(brain);
            Assert.That(runtime.State, Is.EqualTo(HeroState.Moving));
            Assert.That(runtime.CurrentCommand, Is.EqualTo(HeroCommandKind.Move));
            Assert.That(runtime.Destination, Is.EqualTo(Destination));

            brain.Kill();
            runtime.SyncFrom(brain);
            Assert.That(runtime.IsAlive, Is.True, "death is recorded only through BattleRuntimeState.MarkHeroDead");
        }
    }
}
