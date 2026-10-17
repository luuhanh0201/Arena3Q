using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>
    /// Battlefield geometry on the logic plane (spec §9): an open field with bounds, forbidden zones (rectangles,
    /// rivers, ...) and the summon zone in front of the player's castle. Forbidden zones block movement; units steer
    /// round them over a small precomputed visibility graph of nodes placed just outside the zones. This is obstacle
    /// avoidance around a handful of static shapes, not grid pathfinding.
    /// </summary>
    public sealed class BattlefieldLayout
    {
        /// <summary>Largest moving unit radius (hero). Path nodes keep this clearance, so they suit every unit.</summary>
        public const float DefaultMaxUnitRadius = 0.45f;

        // Nodes sit slightly further out than the largest unit needs, so a unit standing on one is clear of the zone.
        private const float NodeMargin = 0.1f;

        private readonly IObstacle[] _obstacles;
        private readonly Rect[] _rectZones;
        private readonly Vector2[] _nodes;
        private readonly bool[,] _visible;
        private readonly float _maxUnitRadius;
        private readonly IObstacle[] _extraObstacles;
        private BattlefieldLayout _forFlyers;

        public BattlefieldLayout(Rect bounds, Rect summonZone, IReadOnlyList<Rect> forbiddenZones = null,
            IReadOnlyList<IObstacle> obstacles = null, float maxUnitRadius = DefaultMaxUnitRadius)
        {
            Bounds = bounds;
            SummonZone = summonZone;
            _maxUnitRadius = maxUnitRadius;
            _rectZones = forbiddenZones == null ? Array.Empty<Rect>() : new List<Rect>(forbiddenZones).ToArray();

            var all = new List<IObstacle>();
            foreach (var rect in _rectZones)
                all.Add(new RectObstacle(rect));
            _extraObstacles = obstacles == null ? Array.Empty<IObstacle>() : new List<IObstacle>(obstacles).ToArray();
            all.AddRange(_extraObstacles);
            _obstacles = all.ToArray();

            _nodes = BuildNodes();
            _visible = BuildVisibility(_nodes);
        }

        public Rect Bounds { get; }
        public Rect SummonZone { get; }

        /// <summary>Rectangular forbidden zones only (rivers and other shapes are in <see cref="Obstacles"/>).</summary>
        public IReadOnlyList<Rect> ForbiddenZones => _rectZones;

        /// <summary>Every forbidden zone, whatever its shape.</summary>
        public IReadOnlyList<IObstacle> Obstacles => _obstacles;

        /// <summary>Path nodes around the forbidden zones (exposed for debugging and map tooling).</summary>
        public IReadOnlyList<Vector2> PathNodes => _nodes;

        /// <summary>
        /// The same field for flying units (spec §8): rivers do not block them, rectangular zones and the bounds do.
        /// Built once on first use; the layout itself when there is no river.
        /// </summary>
        public BattlefieldLayout ForFlyers()
        {
            if (_forFlyers != null)
                return _forFlyers;
            var kept = new List<IObstacle>();
            foreach (var obstacle in _extraObstacles)
            {
                if (!(obstacle is RiverObstacle))
                    kept.Add(obstacle);
            }
            _forFlyers = kept.Count == _extraObstacles.Length
                ? this
                : new BattlefieldLayout(Bounds, SummonZone, _rectZones, kept, _maxUnitRadius);
            return _forFlyers;
        }

        public bool IsInsideBounds(Vector2 point) => Bounds.Contains(point);

        public bool IsInForbiddenZone(Vector2 point) => IsBlocked(point, 0f);

        /// <summary>A valid destination for a <see cref="MoveCommand"/>.</summary>
        public bool IsWalkable(Vector2 point) => IsInsideBounds(point) && !IsInForbiddenZone(point);

        /// <summary>A valid drop position for a free summon.</summary>
        public bool IsValidSummonPosition(Vector2 point) => SummonZone.Contains(point) && IsWalkable(point);

        /// <summary>True if a unit of this radius centred here overlaps a forbidden zone.</summary>
        public bool IsBlocked(Vector2 point, float radius)
        {
            foreach (var obstacle in _obstacles)
            {
                if (obstacle.Contains(point, radius))
                    return true;
            }
            return false;
        }

        /// <summary>True if a unit of this radius walking straight from a to b would touch a forbidden zone.</summary>
        public bool IsSegmentBlocked(Vector2 a, Vector2 b, float radius)
        {
            foreach (var obstacle in _obstacles)
            {
                if (obstacle.BlocksSegment(a, b, radius))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Where to head next on the way to <paramref name="destination"/>: the destination itself if the straight line
        /// is clear, otherwise the next node of the shortest route round the forbidden zones (over a river: via a
        /// crossing). Stateless and a true shortest path, so re-evaluating it every step is stable. Returns the
        /// destination if no route exists.
        /// </summary>
        public Vector2 NextWaypoint(Vector2 from, Vector2 destination, float radius)
        {
            if (!IsSegmentBlocked(from, destination, radius))
                return destination;
            return TryFindRoute(from, destination, radius, out var firstHop, out _) ? firstHop : destination;
        }

        /// <summary>Whether a unit of this radius can walk from a to b at all (map validation).</summary>
        public bool HasPath(Vector2 from, Vector2 to, float radius) =>
            !IsSegmentBlocked(from, to, radius) || TryFindRoute(from, to, radius, out _, out _);

        /// <summary>Length of the walking route from a to b, or +infinity if there is none.</summary>
        public float RouteLength(Vector2 from, Vector2 to, float radius)
        {
            if (!IsSegmentBlocked(from, to, radius))
                return Vector2.Distance(from, to);
            return TryFindRoute(from, to, radius, out _, out var length) ? length : float.PositiveInfinity;
        }

        /// <summary>
        /// Safety net for one movement step: a step that would still touch a zone (separation pushes, rounding) keeps
        /// whichever single-axis part stays clear, else is pushed out to the nearest free point.
        /// </summary>
        public Vector2 ResolveStep(Vector2 from, Vector2 to, float radius)
        {
            if (!IsBlocked(to, radius))
                return to;
            var xOnly = new Vector2(to.x, from.y);
            var yOnly = new Vector2(from.x, to.y);
            var xGain = IsBlocked(xOnly, radius) ? -1f : Mathf.Abs(to.x - from.x);
            var yGain = IsBlocked(yOnly, radius) ? -1f : Mathf.Abs(to.y - from.y);
            if (xGain < 0f && yGain < 0f)
                return PushOut(to, radius);
            return xGain >= yGain ? xOnly : yOnly;
        }

        /// <summary>Moves a point out of every forbidden zone (grown by <paramref name="radius"/>).</summary>
        public Vector2 PushOut(Vector2 point, float radius)
        {
            for (var pass = 0; pass < 2; pass++)
            {
                foreach (var obstacle in _obstacles)
                    point = obstacle.PushOut(point, radius);
            }
            return point;
        }

        // ---- Route search ----

        private Vector2[] BuildNodes()
        {
            var raw = new List<Vector2>();
            foreach (var obstacle in _obstacles)
                obstacle.AddWaypoints(raw, _maxUnitRadius + NodeMargin);

            var walkable = new Rect(Bounds.xMin + _maxUnitRadius, Bounds.yMin + _maxUnitRadius,
                Bounds.width - _maxUnitRadius * 2f, Bounds.height - _maxUnitRadius * 2f);
            var nodes = new List<Vector2>(raw.Count);
            foreach (var node in raw)
            {
                // Nodes inside another zone, or off the map, can never be reached.
                if (walkable.Contains(node) && !IsBlocked(node, _maxUnitRadius))
                    nodes.Add(node);
            }
            return nodes.ToArray();
        }

        private bool[,] BuildVisibility(Vector2[] nodes)
        {
            var visible = new bool[nodes.Length, nodes.Length];
            for (var i = 0; i < nodes.Length; i++)
            {
                for (var j = i + 1; j < nodes.Length; j++)
                {
                    var clear = !IsSegmentBlocked(nodes[i], nodes[j], _maxUnitRadius);
                    visible[i, j] = clear;
                    visible[j, i] = clear;
                }
            }
            return visible;
        }

        // Dijkstra over {from, nodes..., to}. Node-to-node visibility is precomputed for the largest unit radius,
        // which is conservative for smaller units; the start and end links are tested with the actual radius.
        private bool TryFindRoute(Vector2 from, Vector2 to, float radius, out Vector2 firstHop, out float length)
        {
            firstHop = to;
            length = float.PositiveInfinity;
            var n = _nodes.Length;
            if (n == 0)
                return false;

            var start = n;
            var goal = n + 1;
            var count = n + 2;
            var cost = new float[count];
            var previous = new int[count];
            var done = new bool[count];
            for (var i = 0; i < count; i++)
            {
                cost[i] = float.PositiveInfinity;
                previous[i] = -1;
            }
            cost[start] = 0f;

            for (var iteration = 0; iteration < count; iteration++)
            {
                var current = -1;
                for (var i = 0; i < count; i++)
                {
                    if (!done[i] && (current < 0 || cost[i] < cost[current]))
                        current = i;
                }
                if (current < 0 || float.IsPositiveInfinity(cost[current]))
                    break;
                if (current == goal)
                    break;
                done[current] = true;
                var currentPoint = current == start ? from : _nodes[current];

                for (var next = 0; next < count; next++)
                {
                    if (done[next] || next == start)
                        continue;
                    var nextPoint = next == goal ? to : _nodes[next];
                    if (!Linked(current, next, currentPoint, nextPoint, start, goal, radius))
                        continue;
                    var candidate = cost[current] + Vector2.Distance(currentPoint, nextPoint);
                    if (candidate < cost[next])
                    {
                        cost[next] = candidate;
                        previous[next] = current;
                    }
                }
            }

            if (previous[goal] < 0)
                return false;
            length = cost[goal];
            var hop = goal;
            while (previous[hop] != start)
                hop = previous[hop];
            firstHop = hop == goal ? to : _nodes[hop];
            return true;
        }

        private bool Linked(int a, int b, Vector2 pa, Vector2 pb, int start, int goal, float radius)
        {
            if (a < _nodes.Length && b < _nodes.Length)
                return _visible[a, b];
            if (a == start && b < _nodes.Length && (pb - pa).sqrMagnitude < 1e-4f)
                return false; // standing on this node already: it is not a step forward
            return !IsSegmentBlocked(pa, pb, radius);
        }
    }
}
