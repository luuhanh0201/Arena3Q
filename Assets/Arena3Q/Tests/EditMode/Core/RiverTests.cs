using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public class RiverTests
    {
        private const float HeroRadius = 0.45f;
        private static readonly Rect Bounds = new Rect(-24f, -13f, 48f, 26f);

        // Same shape as the shipped stages (PrototypeContentBuilder.StandardRiverPath / StandardBridges).
        private static readonly Vector2[] Path =
        {
            new Vector2(1f, 14f), new Vector2(-1f, 9.5f), new Vector2(1.5f, 5f), new Vector2(0.5f, 0f),
            new Vector2(-1.5f, -5f), new Vector2(1f, -9.5f), new Vector2(-0.5f, -14f)
        };
        private static readonly Vector2 NorthBridge = new Vector2(0.67f, 6.5f);
        private static readonly Vector2 SouthBridge = new Vector2(-0.67f, -6.5f);

        private static RiverObstacle River(bool withBridges = true) =>
            new RiverObstacle(Path, 2.5f, withBridges ? new[] { (NorthBridge, 4f), (SouthBridge, 4f) } : new (Vector2, float)[0]);

        private static BattlefieldLayout Layout(bool withBridges = true) =>
            new BattlefieldLayout(Bounds, new Rect(-18f, -9f, 7f, 18f), null, new IObstacle[] { River(withBridges) });

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
        public void Water_BlocksTheCentreLine_ButNotTheBridgesOrTheBanks()
        {
            var river = River();

            Assert.That(river.Contains(new Vector2(0.5f, 0f), 0f), Is.True, "river centre");
            Assert.That(river.Contains(new Vector2(0.5f + 1.2f, 0f), 0f), Is.True, "inside the 2.5 width");
            Assert.That(river.Contains(new Vector2(4f, 0f), HeroRadius), Is.False, "dry bank");
            Assert.That(river.Contains(NorthBridge, HeroRadius), Is.False, "on the north bridge");
            Assert.That(river.Contains(SouthBridge, HeroRadius), Is.False, "on the south bridge");
            Assert.That(river.WetPieces, Has.Count.EqualTo(3), "two bridges cut the river into three stretches");
        }

        [Test]
        public void Bridge_PassageIsTheCrossingWidth()
        {
            // Straight north-south river, 2.5 wide, one 4-wide bridge at y = 0.
            var river = new RiverObstacle(new[] { new Vector2(0f, -14f), new Vector2(0f, 14f) }, 2.5f,
                new[] { (Vector2.zero, 4f) });

            // A hero's centre may walk across anywhere with |y| < 2 - 0.45 (its body stays on the 4-wide bridge).
            Assert.That(river.BlocksSegment(new Vector2(-3f, 0f), new Vector2(3f, 0f), HeroRadius), Is.False, "bridge centre");
            Assert.That(river.BlocksSegment(new Vector2(-3f, 1.5f), new Vector2(3f, 1.5f), HeroRadius), Is.False, "near the edge");
            Assert.That(river.BlocksSegment(new Vector2(-3f, 2.6f), new Vector2(3f, 2.6f), HeroRadius), Is.True, "off the bridge");
            Assert.That(river.Crossings[0].Center, Is.EqualTo(Vector2.zero));
            Assert.That(river.Crossings[0].Width, Is.EqualTo(4f));
        }

        [Test]
        public void CrossingTheRiver_GoesOverABridge_AndNeverWades()
        {
            var layout = Layout();
            var from = new Vector2(-8f, 0f);
            var to = new Vector2(8f, 0f);

            var path = Walk(layout, from, to, HeroRadius, 3000);

            Assert.That(path.Any(p => layout.IsBlocked(p, HeroRadius - 0.01f)), Is.False, "never steps into the water");
            Assert.That(Vector2.Distance(path.Last(), to), Is.LessThan(0.02f), "arrives on the far bank");
            var nearestBridge = path.Min(p => Mathf.Min(Vector2.Distance(p, NorthBridge), Vector2.Distance(p, SouthBridge)));
            Assert.That(nearestBridge, Is.LessThan(2f), "went over a bridge (within its 4-wide passage)");
        }

        [Test]
        public void CrossingTheRiver_TakesTheNearerBridge()
        {
            var layout = Layout();

            var north = Walk(layout, new Vector2(-8f, 8f), new Vector2(8f, 8f), HeroRadius, 3000);
            var south = Walk(layout, new Vector2(-8f, -8f), new Vector2(8f, -8f), HeroRadius, 3000);

            Assert.That(north.Min(p => Vector2.Distance(p, NorthBridge)), Is.LessThan(2f));
            Assert.That(south.Min(p => Vector2.Distance(p, SouthBridge)), Is.LessThan(2f));
        }

        [Test]
        public void RiverWithoutBridges_CutsTheMap()
        {
            var layout = Layout(withBridges: false);

            Assert.That(layout.HasPath(new Vector2(-8f, 0f), new Vector2(8f, 0f), HeroRadius), Is.False);
            Assert.That(layout.HasPath(new Vector2(-8f, 0f), new Vector2(-8f, 9f), HeroRadius), Is.True, "same bank is fine");
        }

        [Test]
        public void RouteLength_IsLongerThanTheStraightLine_WhenTheRiverIsInTheWay()
        {
            var layout = Layout();
            var from = new Vector2(-8f, 0f);
            var to = new Vector2(8f, 0f);

            var length = layout.RouteLength(from, to, HeroRadius);

            Assert.That(length, Is.GreaterThan(16f));
            Assert.That(length, Is.LessThan(30f));
        }

        [Test]
        public void PushOut_MovesAPointOutOfTheWater()
        {
            var river = River();

            var pushed = river.PushOut(new Vector2(0.8f, 0f), HeroRadius);

            Assert.That(river.Contains(pushed, HeroRadius), Is.False);
            Assert.That(Vector2.Distance(pushed, new Vector2(0.8f, 0f)), Is.LessThan(2.5f), "nearest bank, not far away");
        }

        [Test]
        public void IsInForbiddenZone_CoversRiversAndRectangles()
        {
            var layout = new BattlefieldLayout(Bounds, new Rect(-18f, -9f, 7f, 18f), new[] { new Rect(10f, 10f, 2f, 2f) },
                new IObstacle[] { River() });

            Assert.That(layout.IsInForbiddenZone(new Vector2(0.5f, 0f)), Is.True, "river");
            Assert.That(layout.IsInForbiddenZone(new Vector2(11f, 11f)), Is.True, "rectangle");
            Assert.That(layout.IsWalkable(NorthBridge), Is.True, "bridge");
        }
    }
}
