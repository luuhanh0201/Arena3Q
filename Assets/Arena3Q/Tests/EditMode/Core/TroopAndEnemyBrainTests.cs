using System.Collections.Generic;
using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    internal sealed class FakeTroopSensor : ITroopSensor
    {
        public readonly Dictionary<UnitId, Vector2> Positions = new Dictionary<UnitId, Vector2>();
        public Vector2 Position { get; set; }
        public Vector2 SlotPosition { get; set; }
        public Vector2 HeroPosition { get; set; }
        public UnitId HeroTarget { get; set; }
        public UnitId EnemyInAttackRange { get; set; }
        public UnitId EnemyInAggroRange { get; set; }
        public UnitId NearestEnemy { get; set; }
        public UnitId EnemyCastle { get; set; }
        public UnitId CloseAttacker { get; set; }
        public Vector2 RetreatPoint { get; set; }
        public bool HeroTargetInAttackRange { get; set; }
        public float DistanceToHeroTarget { get; set; } = float.PositiveInfinity;
        public UnitId EnemyNearestToHeroTarget { get; set; }
        public bool IsAlive(UnitId unit) => Positions.ContainsKey(unit);
        public Vector2 GetPosition(UnitId unit) => Positions.TryGetValue(unit, out var p) ? p : Vector2.zero;
    }

    internal sealed class FakeEnemySensor : IEnemySensor
    {
        public readonly Dictionary<UnitId, Vector2> Positions = new Dictionary<UnitId, Vector2>();
        public Vector2 Position { get; set; }
        public UnitId OpponentInAttackRange { get; set; }
        public UnitId OpponentInAggroRange { get; set; }
        public UnitId IntruderNearPost { get; set; }
        public UnitId CloseAttacker { get; set; }
        public UnitId PlayerCastle { get; set; }
        public bool IsAlive(UnitId unit) => Positions.ContainsKey(unit);
        public Vector2 GetPosition(UnitId unit) => Positions.TryGetValue(unit, out var p) ? p : Vector2.zero;
    }

    public class TroopBrainTests
    {
        private static readonly UnitId Enemy = new UnitId(10);
        private static readonly UnitId Target = new UnitId(11);
        private static readonly UnitId Castle = new UnitId(20);

        private FakeTroopSensor _sensor;
        private TroopBrain _brain;

        [SetUp]
        public void SetUp()
        {
            _sensor = new FakeTroopSensor { SlotPosition = new Vector2(1f, 0f), EnemyCastle = Castle };
            _sensor.Positions[Castle] = new Vector2(20f, 0f);
            _brain = new TroopBrain(new TroopBrainSettings { LeashRadius = 4f });
        }

        [TestCase(HeroState.Idle, SquadMode.HoldFormation)]
        [TestCase(HeroState.Moving, SquadMode.MarchFormation)]
        [TestCase(HeroState.Attacking, SquadMode.FocusTarget)]
        [TestCase(HeroState.Advancing, SquadMode.Skirmish)]
        [TestCase(HeroState.Combat, SquadMode.Skirmish)]
        public void SquadMode_FollowsHeroState(HeroState heroState, SquadMode expected)
        {
            Assert.That(SquadModes.FromHero(heroState, OnHeroDeathTroopBehavior.KeepFighting), Is.EqualTo(expected));
        }

        [TestCase(OnHeroDeathTroopBehavior.KeepFighting, SquadMode.Leaderless)]
        [TestCase(OnHeroDeathTroopBehavior.Retreat, SquadMode.Retreat)]
        [TestCase(OnHeroDeathTroopBehavior.Disband, SquadMode.Disband)]
        public void SquadMode_AfterHeroDeath_FollowsStageSetting(OnHeroDeathTroopBehavior behavior, SquadMode expected)
        {
            Assert.That(SquadModes.FromHero(HeroState.Dead, behavior), Is.EqualTo(expected));
        }

        [Test]
        public void March_KeepsFormationAndIgnoresEnemies()
        {
            _sensor.EnemyInAttackRange = Enemy;
            _sensor.EnemyInAggroRange = Enemy;

            var intent = _brain.Tick(SquadMode.MarchFormation, _sensor);

            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.MoveTo));
            Assert.That(intent.Destination, Is.EqualTo(_sensor.SlotPosition));
        }

        [Test]
        public void March_DefendsAgainstCloseAttacker()
        {
            _sensor.CloseAttacker = Enemy;

            var intent = _brain.Tick(SquadMode.MarchFormation, _sensor);

            Assert.That(intent.Target, Is.EqualTo(Enemy));
            Assert.That(intent.MayChase, Is.False, "fights back in place, like the hero");
        }

        [Test]
        public void Focus_AttacksTheHerosOrderedTarget()
        {
            _sensor.HeroTarget = Target;
            _sensor.Positions[Target] = new Vector2(5f, 0f);
            _sensor.EnemyInAttackRange = Enemy;

            var intent = _brain.Tick(SquadMode.FocusTarget, _sensor);

            Assert.That(intent.Target, Is.EqualTo(Target));
            Assert.That(intent.MayChase, Is.True);
        }

        // ---- FocusTarget: unreachable ordered target (spec §7, provisional) ----

        private HeroIntent TickFocus(float seconds, float distance)
        {
            _sensor.DistanceToHeroTarget = distance;
            var intent = HeroIntent.Hold;
            for (var t = 0f; t < seconds - 1e-4f; t += 0.1f)
                intent = _brain.Tick(SquadMode.FocusTarget, _sensor, 0.1f);
            return intent;
        }

        private void OrderTargetWithNeighbour()
        {
            _sensor.HeroTarget = Target;
            _sensor.Positions[Target] = new Vector2(5f, 0f);
            _sensor.EnemyNearestToHeroTarget = Enemy;
            _sensor.Positions[Enemy] = new Vector2(6f, 0f);
        }

        [Test]
        public void Focus_NoProgressTowardsTarget_AttacksEnemyNearestToIt()
        {
            OrderTargetWithNeighbour();

            Assert.That(TickFocus(1.4f, 3f).Target, Is.EqualTo(Target), "still trying before the timeout");
            var intent = TickFocus(0.3f, 3f);

            Assert.That(intent.Target, Is.EqualTo(Enemy));
            Assert.That(_brain.FallbackTarget, Is.EqualTo(Enemy));
        }

        [Test]
        public void Focus_StillApproaching_KeepsOrderedTarget()
        {
            OrderTargetWithNeighbour();

            var intent = HeroIntent.Hold;
            for (var distance = 6f; distance > 1f; distance -= 0.1f)
            {
                _sensor.DistanceToHeroTarget = distance;
                intent = _brain.Tick(SquadMode.FocusTarget, _sensor, 0.1f);
            }

            Assert.That(intent.Target, Is.EqualTo(Target));
            Assert.That(_brain.FallbackTarget, Is.EqualTo(UnitId.None));
        }

        [Test]
        public void Focus_TargetInRange_NeverFallsBack()
        {
            OrderTargetWithNeighbour();
            _sensor.HeroTargetInAttackRange = true;

            Assert.That(TickFocus(5f, 0.2f).Target, Is.EqualTo(Target));
        }

        [Test]
        public void Focus_SubstituteDies_RetriesOrderedTarget()
        {
            OrderTargetWithNeighbour();
            TickFocus(2f, 3f);
            Assert.That(_brain.FallbackTarget, Is.EqualTo(Enemy));

            _sensor.Positions.Remove(Enemy);
            _sensor.EnemyNearestToHeroTarget = UnitId.None;
            var intent = _brain.Tick(SquadMode.FocusTarget, _sensor, 0.1f);

            Assert.That(intent.Target, Is.EqualTo(Target));
            Assert.That(_brain.FallbackTarget, Is.EqualTo(UnitId.None));
        }

        [Test]
        public void Focus_NoEnemyNearTarget_KeepsOrderedTarget()
        {
            _sensor.HeroTarget = Target;
            _sensor.Positions[Target] = new Vector2(5f, 0f);

            Assert.That(TickFocus(3f, 3f).Target, Is.EqualTo(Target));
        }

        [Test]
        public void Focus_NewOrder_ResetsFallback()
        {
            OrderTargetWithNeighbour();
            TickFocus(2f, 3f);

            var other = new UnitId(12);
            _sensor.HeroTarget = other;
            _sensor.Positions[other] = new Vector2(-5f, 0f);
            var intent = _brain.Tick(SquadMode.FocusTarget, _sensor, 0.1f);

            Assert.That(intent.Target, Is.EqualTo(other));
            Assert.That(_brain.FallbackTarget, Is.EqualTo(UnitId.None));
        }

        [Test]
        public void Skirmish_FightsEnemiesNearTheHero()
        {
            _sensor.EnemyInAggroRange = Enemy;
            _sensor.Positions[Enemy] = new Vector2(3f, 0f);

            Assert.That(_brain.Tick(SquadMode.Skirmish, _sensor).Target, Is.EqualTo(Enemy));
        }

        [Test]
        public void Skirmish_IgnoresEnemiesBeyondTheLeash()
        {
            _sensor.EnemyInAggroRange = Enemy;
            _sensor.Positions[Enemy] = new Vector2(9f, 0f);

            var intent = _brain.Tick(SquadMode.Skirmish, _sensor);

            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.MoveTo), "returns to formation instead");
        }

        [Test]
        public void Skirmish_PastTheLeash_RunsBackUntilWellInside()
        {
            _sensor.EnemyInAggroRange = Enemy;
            _sensor.Positions[Enemy] = new Vector2(3f, 0f);

            _sensor.Position = new Vector2(5f, 0f);
            Assert.That(_brain.Tick(SquadMode.Skirmish, _sensor).Kind, Is.EqualTo(HeroIntentKind.MoveTo));

            _sensor.Position = new Vector2(3f, 0f);
            Assert.That(_brain.Tick(SquadMode.Skirmish, _sensor).Kind, Is.EqualTo(HeroIntentKind.MoveTo),
                "still returning: not yet inside half the leash");

            _sensor.Position = new Vector2(1.5f, 0f);
            Assert.That(_brain.Tick(SquadMode.Skirmish, _sensor).Target, Is.EqualTo(Enemy));
        }

        [Test]
        public void Skirmish_HelpsWithTheHerosCurrentTarget()
        {
            _sensor.HeroTarget = Castle;

            Assert.That(_brain.Tick(SquadMode.Skirmish, _sensor).Target, Is.EqualTo(Castle));
        }

        [Test]
        public void Leaderless_AdvancesOnTheEnemyCastle()
        {
            var intent = _brain.Tick(SquadMode.Leaderless, _sensor);

            Assert.That(intent.Target, Is.EqualTo(Castle));
        }

        [Test]
        public void Retreat_WalksToTheRetreatPoint()
        {
            _sensor.RetreatPoint = new Vector2(-18f, 0f);
            _sensor.EnemyInAttackRange = Enemy;

            var intent = _brain.Tick(SquadMode.Retreat, _sensor);

            Assert.That(intent.Destination, Is.EqualTo(_sensor.RetreatPoint));
        }
    }

    public class EnemyBrainTests
    {
        private static readonly UnitId Intruder = new UnitId(1);
        private static readonly UnitId PlayerCastle = new UnitId(2);
        private static readonly Vector2 Post = new Vector2(10f, 0f);

        [Test]
        public void Guard_ChargesIntruderThenReturnsToPost()
        {
            var sensor = new FakeEnemySensor { Position = Post, IntruderNearPost = Intruder };
            sensor.Positions[Intruder] = new Vector2(7f, 0f);
            var brain = new EnemyBrain(new EnemyBrainSettings { Role = EnemyRole.Guard, Post = Post, GuardRadius = 4f });

            Assert.That(brain.Tick(sensor).Target, Is.EqualTo(Intruder));

            // The intruder flees far beyond the guard radius: give up and go home.
            sensor.Positions[Intruder] = new Vector2(0f, 0f);
            sensor.IntruderNearPost = UnitId.None;
            sensor.Position = new Vector2(7f, 0f);
            var intent = brain.Tick(sensor);

            Assert.That(intent.Kind, Is.EqualTo(HeroIntentKind.MoveTo));
            Assert.That(intent.Destination, Is.EqualTo(Post));
        }

        [Test]
        public void Guard_AtPost_HoldsWithoutIntruders()
        {
            var sensor = new FakeEnemySensor { Position = Post };
            var brain = new EnemyBrain(new EnemyBrainSettings { Role = EnemyRole.Guard, Post = Post });

            Assert.That(brain.Tick(sensor).Kind, Is.EqualTo(HeroIntentKind.Hold));
        }

        [Test]
        public void Assault_MarchesOnPlayerCastle_FightingWhatItMeets()
        {
            var sensor = new FakeEnemySensor { PlayerCastle = PlayerCastle };
            sensor.Positions[PlayerCastle] = new Vector2(-20f, 0f);
            var brain = new EnemyBrain(new EnemyBrainSettings { Role = EnemyRole.Assault });

            Assert.That(brain.Tick(sensor).Target, Is.EqualTo(PlayerCastle));

            sensor.OpponentInAggroRange = Intruder;
            sensor.Positions[Intruder] = new Vector2(2f, 0f);
            Assert.That(brain.Tick(sensor).Target, Is.EqualTo(Intruder));
        }
    }

    public class WaveTrackerTests
    {
        [Test]
        public void Waves_SpawnInOrderWhenDue()
        {
            var tracker = new WaveTracker(new[] { 5f, 30f });

            Assert.That(tracker.TakeDueWave(4f), Is.EqualTo(-1));
            Assert.That(tracker.TakeDueWave(5f), Is.EqualTo(0));
            Assert.That(tracker.TakeDueWave(5f), Is.EqualTo(-1));
            Assert.That(tracker.CurrentWave, Is.EqualTo(0));
            Assert.That(tracker.TakeDueWave(100f), Is.EqualTo(1));
            Assert.That(tracker.AllSpawned, Is.True);
        }

        [Test]
        public void Cleared_OnlyAfterAllWavesSpawnedAndKilled()
        {
            var tracker = new WaveTracker(new[] { 0f, 10f });
            tracker.TakeDueWave(0f);
            Assert.That(tracker.IsCleared(0), Is.False, "a wave is still to come");

            tracker.TakeDueWave(10f);
            Assert.That(tracker.IsCleared(3), Is.False);
            Assert.That(tracker.IsCleared(0), Is.True);
        }

        [Test]
        public void NoWaves_IsNeverCleared()
        {
            Assert.That(new WaveTracker(new float[0]).IsCleared(0), Is.False);
        }
    }

    public class ViewMathTests
    {
        private static readonly Rect Bounds = new Rect(-24f, -13f, 48f, 26f);

        [Test]
        public void Camera_IsClampedInsideMapBounds()
        {
            var center = CameraMath.ClampCenter(Bounds, new Vector2(100f, -100f), 5f, 16f / 9f);

            Assert.That(center.x, Is.EqualTo(24f - 5f * 16f / 9f).Within(1e-4f));
            Assert.That(center.y, Is.EqualTo(-13f + 5f).Within(1e-4f));
        }

        [Test]
        public void Camera_WiderThanMap_IsCentred()
        {
            var center = CameraMath.ClampCenter(new Rect(0f, 0f, 4f, 4f), new Vector2(9f, 9f), 5f, 1f);

            Assert.That(center, Is.EqualTo(new Vector2(2f, 2f)));
        }

        [Test]
        public void MaxOrthographicSize_FitsTheMap()
        {
            Assert.That(CameraMath.MaxOrthographicSize(Bounds, 16f / 9f), Is.EqualTo(13f).Within(1e-4f));
        }

        [Test]
        public void EdgeIcon_PointsTowardsTheOffscreenUnit()
        {
            var right = EdgeIndicatorMath.ClampToEdge(new Vector2(3f, 0.5f), 0.05f);
            Assert.That(right.x, Is.EqualTo(0.95f).Within(1e-4f));
            Assert.That(right.y, Is.EqualTo(0.5f).Within(1e-4f));

            var corner = EdgeIndicatorMath.ClampToEdge(new Vector2(-1f, -1f), 0.05f);
            Assert.That(corner.x, Is.EqualTo(0.05f).Within(1e-4f));
            Assert.That(corner.y, Is.EqualTo(0.05f).Within(1e-4f));

            Assert.That(EdgeIndicatorMath.IsOnScreen(new Vector2(0.5f, 0.5f)), Is.True);
            Assert.That(EdgeIndicatorMath.IsOnScreen(new Vector2(1.2f, 0.5f)), Is.False);
        }

        [Test]
        public void EdgeIcon_StaysInsideTheSafeArea_AboveTheCardBar()
        {
            var safe = new Rect(0.05f, 0.25f, 0.9f, 0.7f);

            var below = EdgeIndicatorMath.ClampToArea(new Vector2(0.5f, -2f), safe);
            Assert.That(below.y, Is.EqualTo(0.25f).Within(1e-4f), "pushed up to the top of the card bar");
            Assert.That(below.x, Is.EqualTo(0.5f).Within(1e-4f));

            var belowLeft = EdgeIndicatorMath.ClampToArea(new Vector2(-1f, -1f), safe);
            Assert.That(belowLeft.y, Is.GreaterThanOrEqualTo(0.25f - 1e-4f));
            Assert.That(belowLeft.x, Is.GreaterThanOrEqualTo(0.05f - 1e-4f));

            Assert.That(EdgeIndicatorMath.ClampToArea(new Vector2(3f, 0.5f), safe).x, Is.EqualTo(0.95f).Within(1e-4f));
        }

        [Test]
        public void Formation_SlotsAreDistinct()
        {
            var slots = new HashSet<Vector2>();
            for (var i = 0; i < 18; i++)
                slots.Add(FormationMath.SlotOffset(i, 1f));

            Assert.That(slots, Has.Count.EqualTo(18));
            Assert.That(FormationMath.SlotOffset(0, 1f).magnitude, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(FormationMath.SlotOffset(6, 1f).magnitude, Is.EqualTo(2f).Within(1e-4f), "second ring");
        }

        [Test]
        public void Gesture_TapVersusDrag()
        {
            Assert.That(GestureRules.IsTap(Vector2.zero, new Vector2(5f, 5f), 0.2f, 20f, 0.4f), Is.True);
            Assert.That(GestureRules.IsTap(Vector2.zero, new Vector2(50f, 0f), 0.2f, 20f, 0.4f), Is.False, "moved too far");
            Assert.That(GestureRules.IsTap(Vector2.zero, Vector2.zero, 1f, 20f, 0.4f), Is.False, "held too long");
        }
    }
}
