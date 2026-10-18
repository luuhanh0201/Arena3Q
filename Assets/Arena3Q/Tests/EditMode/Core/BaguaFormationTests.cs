using System.Collections.Generic;
using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>Bát Quái Trận rules (docs/BanDo/BatQuaiTran.md), tests (a)–(e) of §7 and the altar / gate rules.</summary>
    public class BaguaFormationTests
    {
        private const float HeroRadius = 0.45f;
        private const float SoldierRadius = 0.3f;
        private const float Ring = 5.7f;
        private static readonly Rect Bounds = new Rect(-24f, -13f, 48f, 26f);
        private static readonly Vector2 PlayerCastle = new Vector2(-19.5f, 0f);
        private static readonly Vector2 EnemyCastle = new Vector2(19.5f, 0f);

        /// <summary>The ring of BatTran_Layout.json: pillars at 22.5° + k·45°, gates at k·45° with rotY = 90° − k·45°.</summary>
        private static BaguaFormationGeometry Geometry()
        {
            var geometry = new BaguaFormationGeometry();
            for (var k = 0; k < 8; k++)
            {
                var pillarAngle = (22.5f + k * 45f) * Mathf.Deg2Rad;
                geometry.Pillars.Add((new Vector2(Mathf.Cos(pillarAngle), Mathf.Sin(pillarAngle)) * Ring, 1f));
                var gateAngle = k * 45f * Mathf.Deg2Rad;
                geometry.Gates.Add(new BaguaGateSpot(new Vector2(Mathf.Cos(gateAngle), Mathf.Sin(gateAngle)) * Ring,
                    BaguaGateSpot.AcrossFromRotY(90f - k * 45f)));
            }
            return geometry;
        }

        private static BaguaFormation Formation() => new BaguaFormation(Geometry(), new BaguaFormationSettings());

        private static BattlefieldLayout Layout(BaguaFormation formation) =>
            new BattlefieldLayout(Bounds, new Rect(-17.5f, -9f, 6.5f, 18f), null, formation.Obstacles);

        private static Vector2 Outward(int gate) =>
            new Vector2(Mathf.Cos(gate * 45f * Mathf.Deg2Rad), Mathf.Sin(gate * 45f * Mathf.Deg2Rad));

        private static List<Vector2> Walk(BattlefieldLayout layout, Vector2 from, Vector2 to, float radius, int maxSteps,
            System.Action<int, Vector2> onStep = null)
        {
            var path = new List<Vector2> { from };
            var position = from;
            for (var i = 0; i < maxSteps && Vector2.Distance(position, to) > 0.01f; i++)
            {
                onStep?.Invoke(i, position);
                var delta = layout.NextWaypoint(position, to, radius) - position;
                var next = position + delta.normalized * Mathf.Min(0.1f, delta.magnitude);
                position = layout.ResolveStep(position, next, radius);
                path.Add(position);
            }
            return path;
        }

        // ---- Geometry ----

        [Test]
        public void Gates_SitBetweenPillars_AndRunAcrossTheGap()
        {
            var geometry = Geometry();
            Assert.That(geometry.Gates[0].Position, Is.EqualTo(new Vector2(Ring, 0f)), "gate 0 faces east (enemy side)");
            Assert.That(Vector2.Distance(geometry.Gates[2].Position, new Vector2(0f, Ring)), Is.LessThan(1e-4f), "gate 2 north");
            Assert.That(Mathf.Abs(geometry.Gates[0].Across.y), Is.EqualTo(1f).Within(1e-4f), "east gap runs north-south");
            Assert.That(Mathf.Abs(geometry.Gates[2].Across.x), Is.EqualTo(1f).Within(1e-4f), "north gap runs east-west");
            var gap = Vector2.Distance(geometry.Pillars[0].position, geometry.Pillars[7].position) - 2f;
            Assert.That(gap, Is.EqualTo(2.36f).Within(0.01f), "~2.36 between pillar edges");
        }

        [Test]
        public void Pillars_AlwaysBlock_AltarDoesNot()
        {
            var formation = Formation();
            var layout = Layout(formation);
            foreach (var (position, _) in formation.Geometry.Pillars)
                Assert.That(layout.IsBlocked(position, 0f), Is.True);
            Assert.That(layout.IsWalkable(Vector2.zero), Is.True, "the altar is a capture zone, not an obstacle");
            for (var i = 0; i < 8; i++)
                formation.RotateNow();
            foreach (var (position, _) in formation.Geometry.Pillars)
                Assert.That(layout.IsBlocked(position, 0f), Is.True, "pillars never disappear");
        }

        // ---- (a) a closed gate lets nobody through ----

        [Test]
        public void ClosedGate_BlocksTheWholeGap()
        {
            var formation = Formation();
            Assert.That(formation.KindOf(3), Is.EqualTo(BaguaGateKind.Closed), "gates 3 & 7 start as Đỗ");
            var layout = Layout(formation);
            var gate = formation.Geometry.Gates[3];
            var outward = Outward(3);
            for (var offset = -1.3f; offset <= 1.3f; offset += 0.1f)
            {
                var along = gate.Position + gate.Across * offset;
                Assert.That(layout.IsSegmentBlocked(along + outward * 2f, along - outward * 2f, SoldierRadius), Is.True,
                    $"a soldier cannot pass {offset:0.0} along the closed gap");
            }
            var route = Walk(layout, gate.Position + outward * 2.5f, Vector2.zero, SoldierRadius, 600);
            Assert.That(Vector2.Distance(route[route.Count - 1], Vector2.zero), Is.LessThan(0.05f), "still reaches the altar");
            Assert.That(route.Exists(p => Vector2.Distance(p, gate.Position) < 0.5f), Is.False, "but not through gate 3");

            var open = formation.Geometry.Gates[0];
            Assert.That(layout.IsSegmentBlocked(open.Position + Outward(0) * 2f, open.Position - Outward(0) * 2f, HeroRadius),
                Is.False, "an open gate lets a hero through");
        }

        // ---- (b) routes change as soon as a gate closes ----

        [Test]
        public void ClosingAGate_RebuildsRoutes_AndUnitsTakeAnotherGate()
        {
            var formation = Formation();
            var layout = Layout(formation);
            var start = new Vector2(-10f, 0f);
            var closedHalfway = false;
            var route = Walk(layout, start, Vector2.zero, HeroRadius, 800, (step, position) =>
            {
                if (closedHalfway || position.x < -8f)
                    return;
                closedHalfway = true;
                Assert.That(formation.SetForcedClosed(4, true) & BaguaFormationEvents.ObstaclesChanged,
                    Is.Not.EqualTo(BaguaFormationEvents.None));
                layout.RebuildRoutes();
            });
            Assert.That(closedHalfway, Is.True);
            Assert.That(Vector2.Distance(route[route.Count - 1], Vector2.zero), Is.LessThan(0.05f), "reached the altar");
            var gate4 = formation.Geometry.Gates[4].Position;
            Assert.That(route.Exists(p => Vector2.Distance(p, gate4) < 0.5f), Is.False, "went round the closed west gate");
        }

        // ---- (c) nobody is stuck in a gap when the slab rises ----

        [Test]
        public void RisingSlab_PushesUnitsInTheGapToTheNearerSide()
        {
            var formation = Formation();
            var layout = Layout(formation);
            var gate = formation.Geometry.Gates[4];
            var outward = Outward(4);
            formation.SetForcedClosed(4, true);
            layout.RebuildRoutes();
            foreach (var depth in new[] { -0.3f, -0.05f, 0.05f, 0.3f })
            {
                foreach (var along in new[] { -0.6f, 0f, 0.6f })
                {
                    var caught = gate.Position + outward * depth + gate.Across * along;
                    var free = layout.PushOut(caught, HeroRadius);
                    Assert.That(layout.IsBlocked(free, HeroRadius), Is.False, $"free after the push ({depth}, {along})");
                    Assert.That(Vector2.Dot(free - gate.Position, outward) * depth, Is.GreaterThan(0f),
                        "pushed to the side it was nearer");
                    Assert.That(Vector2.Distance(free, caught), Is.LessThan(1.2f), "a short push");
                }
            }
        }

        [Test]
        public void Slab_BlocksFromTheStartOfTheRise_UntilTheEndOfTheLowering()
        {
            var formation = Formation();
            Assert.That(formation.IsSlabBlocking(0), Is.False);
            formation.SetForcedClosed(0, true);
            Assert.That(formation.IsSlabBlocking(0), Is.True, "on at the first moment of the rise");
            formation.Tick(0.3f, false, false);
            Assert.That(formation.SlabRaise(0), Is.EqualTo(0.5f).Within(1e-3f), "0.6 s rise");
            formation.Tick(0.3f, false, false);
            Assert.That(formation.SlabRaise(0), Is.EqualTo(1f));

            formation.SetForcedClosed(0, false);
            formation.Tick(0.5f, false, false);
            Assert.That(formation.IsSlabBlocking(0), Is.True, "still blocking while it goes down");
            var events = formation.Tick(0.11f, false, false);
            Assert.That(formation.IsSlabBlocking(0), Is.False, "off at the end of the lowering");
            Assert.That(events & BaguaFormationEvents.ObstaclesChanged, Is.Not.EqualTo(BaguaFormationEvents.None));
        }

        // ---- (d) both sides always meet the same gate types ----

        [Test]
        public void Rotation_IsClockwise_AndOppositeGatesStayEqual()
        {
            var formation = Formation();
            var start = new[]
            {
                BaguaGateKind.Haste, BaguaGateKind.Life, BaguaGateKind.Death, BaguaGateKind.Closed,
                BaguaGateKind.Haste, BaguaGateKind.Life, BaguaGateKind.Death, BaguaGateKind.Closed
            };
            for (var k = 0; k < 8; k++)
                Assert.That(formation.KindOf(k), Is.EqualTo(start[k]));

            var events = formation.Tick(17.1f, false, false);
            Assert.That(events & BaguaFormationEvents.WarningStarted, Is.Not.EqualTo(BaguaFormationEvents.None), "3 s before");
            Assert.That(formation.IsWarning, Is.True);
            events = formation.Tick(3f, false, false);
            Assert.That(events & BaguaFormationEvents.Rotated, Is.Not.EqualTo(BaguaFormationEvents.None), "every 20 s");
            Assert.That(formation.KindOf(0), Is.EqualTo(BaguaGateKind.Life), "gate k takes gate k+1's type");
            Assert.That(formation.KindOf(7), Is.EqualTo(BaguaGateKind.Haste), "Khai moves east → south-east: clockwise");
            Assert.That(formation.KindOf(3), Is.EqualTo(BaguaGateKind.Haste));

            for (var rotation = 0; rotation < 16; rotation++)
            {
                for (var k = 0; k < 4; k++)
                    Assert.That(formation.KindOf(k), Is.EqualTo(formation.KindOf(formation.OppositeOf(k))), $"rotation {rotation}");
                formation.RotateNow();
            }
        }

        // ---- (e) the middle lane is ~39 with gates 0 and 4 open ----

        [Test]
        public void MiddleLane_IsStraight39_WhenGates0And4AreOpen()
        {
            var formation = Formation();
            var layout = Layout(formation);
            Assert.That(formation.IsGateClosed(0) || formation.IsGateClosed(4), Is.False);
            Assert.That(layout.RouteLength(PlayerCastle, EnemyCastle, HeroRadius), Is.EqualTo(39f).Within(0.01f));

            formation.SetForcedClosed(0, true);
            formation.SetForcedClosed(4, true);
            layout.RebuildRoutes();
            var detour = layout.RouteLength(PlayerCastle, EnemyCastle, HeroRadius);
            Assert.That(detour, Is.GreaterThan(39.5f).And.LessThan(50f), "round the ring or through another gate");
        }

        // ---- Altar ----

        [Test]
        public void Altar_IsHeldAfter5sAlone_PausedWhenContested_ResetWhenLeft()
        {
            var formation = Formation();
            formation.Tick(3f, true, false);
            Assert.That(formation.CapturingTeam, Is.EqualTo(Team.Player));
            formation.Tick(1f, true, true);
            Assert.That(formation.IsAltarContested, Is.True);
            Assert.That(formation.CaptureProgress, Is.EqualTo(0.6f).Within(1e-3f), "paused, not reset");
            formation.Tick(1.9f, true, false);
            Assert.That(formation.AltarOwner, Is.Null, "4.9 s of being alone");
            var events = formation.Tick(0.2f, true, false);
            Assert.That(formation.AltarOwner, Is.EqualTo(Team.Player));
            Assert.That(events & BaguaFormationEvents.AltarOwnerChanged, Is.Not.EqualTo(BaguaFormationEvents.None));

            formation.Tick(4f, false, true);
            formation.Tick(0.5f, false, false);
            Assert.That(formation.CaptureProgress, Is.EqualTo(0f), "leaving resets the count");
            Assert.That(formation.AltarOwner, Is.EqualTo(Team.Player), "nobody there: the old holder keeps it");
        }

        [Test]
        public void HolderAtRotation_IsBlessed_ForTheNextCycle()
        {
            var formation = Formation();
            formation.SetAltarOwner(Team.Enemy);
            Assert.That(formation.TryGetEffect(BaguaGateKind.Death, Team.Enemy, out _), Is.True, "not before a rotation");
            formation.RotateNow();
            Assert.That(formation.BlessedTeam, Is.EqualTo(Team.Enemy));

            Assert.That(formation.TryGetEffect(BaguaGateKind.Death, Team.Enemy, out _), Is.False, "immune to Tử");
            Assert.That(formation.TryGetEffect(BaguaGateKind.Life, Team.Enemy, out var life), Is.True);
            Assert.That(life.HealFraction, Is.EqualTo(0.12f).Within(1e-4f), "Sinh +50 %");
            formation.TryGetEffect(BaguaGateKind.Haste, Team.Enemy, out var haste);
            Assert.That(haste.MoveSpeedUp, Is.EqualTo(0.45f).Within(1e-4f), "Khai +50 %");
            formation.TryGetEffect(BaguaGateKind.Death, Team.Player, out var death);
            Assert.That(death.DamageFraction, Is.EqualTo(0.1f).Within(1e-4f), "the other side gets the plain effect");
            Assert.That(formation.TryGetEffect(BaguaGateKind.Closed, Team.Player, out _), Is.False);

            formation.SetAltarOwner(null);
            formation.RotateNow();
            Assert.That(formation.BlessedTeam, Is.Null, "nobody held it at the next rotation");
        }

        // ---- Triggers ----

        [Test]
        public void GateTrigger_FiresOncePerEntry_WithAPerUnitCooldown()
        {
            var formation = Formation();
            var gate = formation.Geometry.Gates[1];
            Assert.That(formation.GateTriggerAt(gate.Position), Is.EqualTo(1));
            Assert.That(formation.GateTriggerAt(gate.Position + gate.Across * 1.1f), Is.EqualTo(1), "2.4 across the gap");
            Assert.That(formation.GateTriggerAt(gate.Position + Outward(1) * 0.6f), Is.EqualTo(-1), "1.0 deep");
            Assert.That(formation.GateTriggerAt(Vector2.zero), Is.EqualTo(-1));

            var visits = new BaguaGateVisits();
            var unit = new UnitId(7);
            Assert.That(visits.Enter(unit, 1), Is.EqualTo(1), "entered");
            Assert.That(visits.Enter(unit, 1), Is.EqualTo(-1), "standing inside does not fire again");
            Assert.That(visits.TryUse(unit, BaguaGateKind.Life, 0f, 5f), Is.True);
            visits.Enter(unit, -1);
            Assert.That(visits.Enter(unit, 1), Is.EqualTo(1), "a new entry");
            Assert.That(visits.TryUse(unit, BaguaGateKind.Life, 4f, 5f), Is.False, "Sinh cooldown 5 s per unit");
            Assert.That(visits.TryUse(new UnitId(8), BaguaGateKind.Life, 4f, 5f), Is.True, "other units are not affected");
            Assert.That(visits.TryUse(unit, BaguaGateKind.Life, 5f, 5f), Is.True);
        }
    }
}
