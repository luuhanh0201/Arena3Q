using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public class CommandRulesTests
    {
        [Test]
        public void Move_ToWalkableGround_IsAccepted()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateMove(new MoveCommand(hero.Id, new Vector2(5f, 5f)), state, TestBattles.Layout());

            Assert.That(result.Accepted, Is.True);
        }

        [Test]
        public void Move_OutsideBattlefield_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateMove(new MoveCommand(hero.Id, new Vector2(50f, 0f)), state, TestBattles.Layout());

            Assert.That(result.Reason, Is.EqualTo(RejectReason.OutsideBattlefield));
        }

        [Test]
        public void Move_IntoForbiddenZone_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateMove(new MoveCommand(hero.Id, Vector2.zero), state, TestBattles.Layout());

            Assert.That(result.Reason, Is.EqualTo(RejectReason.InForbiddenZone));
        }

        [Test]
        public void Move_ForDeadOrUnknownHero_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);
            state.MarkHeroDead(hero.Id);
            var destination = new Vector2(5f, 5f);

            Assert.That(CommandRules.ValidateMove(new MoveCommand(hero.Id, destination), state, TestBattles.Layout()).Reason,
                Is.EqualTo(RejectReason.HeroNotAvailable));
            Assert.That(CommandRules.ValidateMove(new MoveCommand(new UnitId(999), destination), state, TestBattles.Layout()).Reason,
                Is.EqualTo(RejectReason.HeroNotAvailable));
        }

        [Test]
        public void Attack_LivingEnemy_IsAccepted()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateAttack(new AttackCommand(hero.Id, state.EnemyCastle.Id), state, Team.Enemy, true);

            Assert.That(result.Accepted, Is.True);
        }

        [Test]
        public void Attack_FriendlyTarget_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateAttack(new AttackCommand(hero.Id, state.PlayerCastle.Id), state, Team.Player, true);

            Assert.That(result.Reason, Is.EqualTo(RejectReason.TargetNotHostile));
        }

        [Test]
        public void Attack_DeadOrMissingTarget_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            Assert.That(CommandRules.ValidateAttack(new AttackCommand(hero.Id, new UnitId(42)), state, Team.Enemy, false).Reason,
                Is.EqualTo(RejectReason.InvalidTarget));
            Assert.That(CommandRules.ValidateAttack(new AttackCommand(hero.Id, UnitId.None), state, Team.Enemy, true).Reason,
                Is.EqualTo(RejectReason.InvalidTarget));
        }

        [Test]
        public void Commands_AfterBattleOver_AreRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);
            state.Finish(BattleOutcome.Victory);

            Assert.That(CommandRules.ValidateMove(new MoveCommand(hero.Id, new Vector2(5f, 5f)), state, TestBattles.Layout()).Reason,
                Is.EqualTo(RejectReason.BattleOver));
        }

        // Walks like UnitBase.MoveTowards: head for the next waypoint, resolve the step against forbidden zones.
        private static List<Vector2> Walk(BattlefieldLayout layout, Vector2 from, Vector2 to, float radius, int maxSteps)
        {
            var path = new List<Vector2> { from };
            var position = from;
            for (var i = 0; i < maxSteps && Vector2.Distance(position, to) > 0.01f; i++)
            {
                var delta = layout.NextWaypoint(position, to, radius) - position;
                var next = position + delta.normalized * Mathf.Min(0.1f, delta.magnitude);
                position = layout.ResolveStep(position, next, radius);
                path.Add(position);
            }
            return path;
        }

        [Test]
        public void NextWaypoint_ClearLine_IsTheDestination()
        {
            var destination = new Vector2(6f, 5f);

            Assert.That(TestBattles.Layout().NextWaypoint(new Vector2(-6f, 5f), destination, 0.3f), Is.EqualTo(destination));
        }

        private static bool InsideGrown(Rect zone, Vector2 point, float radius) =>
            new Rect(zone.xMin - radius, zone.yMin - radius, zone.width + radius * 2f, zone.height + radius * 2f).Contains(point);

        [Test]
        public void ForbiddenZone_WalkingStraightAtIt_GoesAroundAndArrives()
        {
            var layout = TestBattles.Layout();
            var path = Walk(layout, new Vector2(-6f, 0f), new Vector2(6f, 0f), 0.3f, 1000);

            Assert.That(path.Any(p => InsideGrown(TestBattles.ForbiddenZone, p, 0.3f - 1e-3f)), Is.False, "never enters the zone");
            Assert.That(Vector2.Distance(path[path.Count - 1], new Vector2(6f, 0f)), Is.LessThan(0.02f), "still arrives");
        }

        [Test]
        public void ForbiddenZone_TouchingMapEdge_IsPassedOnTheOpenSide()
        {
            // Zone glued to the top of the map: the only way round is below it.
            var zone = new Rect(-2f, 5f, 5f, 8f);
            var layout = new BattlefieldLayout(new Rect(-24f, -13f, 48f, 26f), TestBattles.SummonZone, new[] { zone });
            var path = Walk(layout, new Vector2(-6f, 9f), new Vector2(6f, 9f), 0.45f, 2000);

            Assert.That(path.Any(p => InsideGrown(zone, p, 0.45f - 1e-3f)), Is.False);
            Assert.That(Vector2.Distance(path[path.Count - 1], new Vector2(6f, 9f)), Is.LessThan(0.02f));
            Assert.That(path.Min(p => p.y), Is.LessThan(5f), "went round the bottom");
        }

        [Test]
        public void ForbiddenZone_GlancingStep_SlidesAlongTheEdge()
        {
            var layout = TestBattles.Layout();
            var from = new Vector2(-2.31f, -1f);
            var resolved = layout.ResolveStep(from, from + new Vector2(0.1f, 0.1f), 0.3f);

            Assert.That(resolved.x, Is.LessThan(-2.3f + 1e-3f), "does not cross the left face");
            Assert.That(resolved.y, Is.GreaterThan(-1f), "keeps the tangential part of the step");
        }

        [Test]
        public void ForbiddenZone_PointInside_IsPushedToNearestEdge()
        {
            var pushed = TestBattles.Layout().PushOut(new Vector2(1.5f, 0.2f), 0.3f);

            Assert.That(pushed.x, Is.GreaterThan(2.3f));
            Assert.That(pushed.y, Is.EqualTo(0.2f));
        }

        [Test]
        public void Layout_SummonZoneExcludesForbiddenAndOutsidePoints()
        {
            var layout = new BattlefieldLayout(TestBattles.Bounds, TestBattles.SummonZone, new[] { new Rect(-12f, -1f, 2f, 2f) });

            Assert.That(layout.IsValidSummonPosition(new Vector2(-8f, 5f)), Is.True);
            Assert.That(layout.IsValidSummonPosition(new Vector2(-11f, 0f)), Is.False, "forbidden zone inside summon zone");
            Assert.That(layout.IsValidSummonPosition(new Vector2(0f, 0f)), Is.False, "outside summon zone");
        }
    }
}
