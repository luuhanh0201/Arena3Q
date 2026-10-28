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
        // Phòng thủ area in front of the own castle (the summon zone), chase margin 3.
        private static readonly Rect DefendArea = new Rect(-14f, -8f, 8f, 16f);

        private FakeHeroSensor _sensor;

        [SetUp]
        public void SetUp()
        {
            _sensor = new FakeHeroSensor { EnemyCastle = Castle };
        }

        private static HeroBrain Brain(StageType stageType = StageType.Attack, bool selfDefendWhileAttacking = false) =>
            new HeroBrain(new HeroBrainSettings
            {
                StageType = stageType,
                SelfDefendWhileAttacking = selfDefendWhileAttacking,
                DefendArea = DefendArea,
                DefendChaseMargin = 3f
            });

        private HeroBrain Brain(ArmyStance stance, StageType stageType = StageType.Attack)
        {
            var brain = Brain(stageType);
            brain.SetStance(stance, _sensor.Position);
            return brain;
        }

        private HeroIntent Tick(HeroBrain brain, float seconds, float step = 0.1f)
        {
            var intent = HeroIntent.Hold;
            var steps = Mathf.RoundToInt(seconds / step);
            for (var i = 0; i < steps; i++)
                intent = brain.Tick(step, _sensor);
            return intent;
        }

        // ---- Giữ (Hold): the stance every battle starts in; no 5-second rule any more ----

        [Test]
        public void NewHero_StartsIdle_InHold()
        {
            var brain = Brain();
            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));
            Assert.That(brain.Stance, Is.EqualTo(ArmyStance.Hold));
        }

        [Test]
        public void Hold_NoTarget_StaysIdleForever()
        {
            var brain = Brain();

            var intent = Tick(brain, 30f);

            Assert.That(brain.State, Is.EqualTo(HeroState.Idle), "the old 5-second advance is gone (spec §5, 2026-10-28)");
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Hold));
        }

        [Test]
        public void Hold_AttacksEnemyInRangeWithoutChasing()
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
        public void Hold_DoesNotChaseEnemyOnlyInAggroRange()
        {
            var brain = Brain();
            _sensor.EnemyInAggroRange = EnemyA;
            _sensor.NearestEnemy = EnemyA;

            var intent = Tick(brain, 10f);

            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Hold));
        }

        [Test]
        public void Hold_StopsAnAdvancingHeroWhereItStands()
        {
            var brain = Brain(ArmyStance.Attack);
            brain.Tick(0.1f, _sensor);
            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));

            brain.SetStance(ArmyStance.Hold, new Vector2(3f, 2f));
            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Hold));
        }

        // ---- Tấn công (Attack) ----

        [Test]
        public void Attack_AdvancesOnTheEnemyCastle()
        {
            var brain = Brain(ArmyStance.Attack);

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Engage));
            Assert.That(intent.Target, Is.EqualTo(Castle), "Attack stage advances towards the enemy castle");
            Assert.That(intent.MayChase, Is.True);
        }

        [Test]
        public void Attack_FightsEnemyInAggroUntilItDies_ThenContinues()
        {
            var brain = Brain(ArmyStance.Attack);
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
        public void Attack_DefenseStage_HeadsForNearestEnemy()
        {
            var brain = Brain(ArmyStance.Attack, StageType.Defense);
            _sensor.NearestEnemy = EnemyB;

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
            Assert.That(intent.Target, Is.EqualTo(EnemyB));
            Assert.That(intent.Target, Is.Not.EqualTo(Castle), "Defense stages do not march on the enemy castle");
        }

        [Test]
        public void Attack_DefenseStage_NoEnemies_WaitsAndSetsOffWhenOneAppears()
        {
            var brain = Brain(ArmyStance.Attack, StageType.Defense);

            Assert.That(Tick(brain, 1f).Kind, Is.EqualTo(HeroIntentKind.Hold));

            _sensor.NearestEnemy = EnemyA;
            var intent = brain.Tick(0.1f, _sensor);
            Assert.That(intent.Target, Is.EqualTo(EnemyA));
        }

        // ---- Phòng thủ (Defend) ----

        [Test]
        public void Defend_FallsBackToTheNearestPointOfTheDefenceArea()
        {
            _sensor.Position = new Vector2(10f, 20f);
            var brain = Brain(ArmyStance.Defend);

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Moving));
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.MoveTo));
            Assert.That(intent.Destination, Is.EqualTo(new Vector2(-6f, 8f)), "clamped into the area (xMax, yMax)");

            _sensor.Position = intent.Destination;
            Assert.That(brain.Tick(0.1f, _sensor).Kind, Is.EqualTo(HeroIntentKind.Hold));
            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));
        }

        [Test]
        public void Defend_InsideTheArea_StaysWhereItIs()
        {
            _sensor.Position = new Vector2(-10f, 0f);
            var brain = Brain(ArmyStance.Defend);

            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));
            Assert.That(brain.DefendPoint, Is.EqualTo(new Vector2(-10f, 0f)));
            Assert.That(Tick(brain, 5f).Kind, Is.EqualTo(HeroIntentKind.Hold));
        }

        [Test]
        public void Defend_ChasesAnIntruder_ThenGoesBack()
        {
            _sensor.Position = new Vector2(-10f, 0f);
            var brain = Brain(ArmyStance.Defend);
            _sensor.PlaceEnemy(EnemyA, new Vector2(-4f, 0f)); // 2 beyond the area, within the chase margin

            var chase = brain.Tick(0.1f, _sensor);
            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
            Assert.That(chase.Target, Is.EqualTo(EnemyA));
            Assert.That(chase.MayChase, Is.True);

            _sensor.Position = new Vector2(-5f, 0f);
            _sensor.PlaceEnemy(EnemyA, new Vector2(5f, 0f)); // ran off beyond the margin
            var back = brain.Tick(0.1f, _sensor);
            Assert.That(brain.State, Is.EqualTo(HeroState.Moving));
            Assert.That(back.Destination, Is.EqualTo(new Vector2(-10f, 0f)), "back to its defence point");
        }

        [Test]
        public void Defend_IgnoresEnemiesFarFromTheArea()
        {
            _sensor.Position = new Vector2(-10f, 0f);
            var brain = Brain(ArmyStance.Defend);
            _sensor.PlaceEnemy(EnemyA, new Vector2(20f, 0f));
            _sensor.EnemyInAggroRange = EnemyA;

            Assert.That(Tick(brain, 5f).Kind, Is.EqualTo(HeroIntentKind.Hold));
        }

        [Test]
        public void Stance_ReplacesAScriptedOrder()
        {
            var brain = Brain();
            brain.Issue(new MoveCommand(Hero, Destination));

            brain.SetStance(ArmyStance.Attack, Vector2.zero);

            Assert.That(brain.State, Is.EqualTo(HeroState.Advancing));
            Assert.That(brain.CurrentCommand, Is.EqualTo(HeroCommandKind.None));
        }

        // ---- Moving: IgnoreUntilDestination (scripted orders and the Phòng thủ fall-back) ----

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
            Assert.That(defend.MayChase, Is.False, "self-defence fights in place (spec §6 rule 5)");

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
        public void Moving_ArrivesAtDestination_GoesIdle_AndHolds()
        {
            var brain = Brain();
            brain.Issue(new MoveCommand(Hero, Destination));

            _sensor.Position = Destination;
            var intent = brain.Tick(0.1f, _sensor);
            Assert.That(brain.State, Is.EqualTo(HeroState.Idle));
            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.Hold));
            Assert.That(brain.CurrentCommand, Is.EqualTo(HeroCommandKind.None));

            Tick(brain, 10f);
            Assert.That(brain.State, Is.EqualTo(HeroState.Idle), "Giữ: no automatic advance");
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

        // ---- Attacking (scripted attack orders) ----

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
        public void Attacking_SelfDefenceOption_DefendsThenResumesAttack()
        {
            var brain = Brain(selfDefendWhileAttacking: true);
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
        public void Attacking_ByDefault_OrderTakesPriorityOverCloseAttacker()
        {
            var brain = Brain();
            brain.Issue(new AttackCommand(Hero, Castle));
            _sensor.CloseAttacker = EnemyA;

            var intent = brain.Tick(0.1f, _sensor);

            Assert.That(brain.State, Is.EqualTo(HeroState.Attacking));
            Assert.That(intent.Target, Is.EqualTo(Castle));
        }

        [Test]
        public void Advancing_NewCommandTakesOver()
        {
            var brain = Brain(ArmyStance.Attack);
            brain.Tick(0.1f, _sensor);

            brain.Issue(new MoveCommand(Hero, Destination));

            Assert.That(brain.State, Is.EqualTo(HeroState.Moving));
        }

        // ---- Dead ----

        [Test]
        public void Dead_IgnoresCommandsAndStancesAndHolds()
        {
            var brain = Brain();
            brain.Issue(new AttackCommand(Hero, EnemyA));

            brain.Kill();
            brain.Issue(new MoveCommand(Hero, Destination));
            brain.SetStance(ArmyStance.Attack, Vector2.zero);
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
