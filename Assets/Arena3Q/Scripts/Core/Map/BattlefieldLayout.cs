using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>
    /// Battlefield geometry on the logic plane (spec §9): an open field with bounds, optional
    /// forbidden zones and the summon zone in front of the player's castle. No pathfinding.
    /// </summary>
    public sealed class BattlefieldLayout
    {
        private readonly Rect[] _forbiddenZones;

        public BattlefieldLayout(Rect bounds, Rect summonZone, IReadOnlyList<Rect> forbiddenZones = null)
        {
            Bounds = bounds;
            SummonZone = summonZone;
            _forbiddenZones = forbiddenZones == null ? Array.Empty<Rect>() : new List<Rect>(forbiddenZones).ToArray();
        }

        public Rect Bounds { get; }
        public Rect SummonZone { get; }
        public IReadOnlyList<Rect> ForbiddenZones => _forbiddenZones;

        public bool IsInsideBounds(Vector2 point) => Bounds.Contains(point);

        public bool IsInForbiddenZone(Vector2 point)
        {
            foreach (var zone in _forbiddenZones)
            {
                if (zone.Contains(point))
                    return true;
            }
            return false;
        }

        /// <summary>A valid destination for a <see cref="MoveCommand"/>.</summary>
        public bool IsWalkable(Vector2 point) => IsInsideBounds(point) && !IsInForbiddenZone(point);

        /// <summary>A valid drop position for a free summon.</summary>
        public bool IsValidSummonPosition(Vector2 point) => SummonZone.Contains(point) && IsWalkable(point);

        // Corners are aimed at slightly outside the grown zone so a unit standing on one has a clear view onwards.
        private const float CornerClearance = 0.05f;

        /// <summary>
        /// Where to head next on the way to <paramref name="destination"/> (spec §9: forbidden zones block movement; no
        /// general pathfinding). If a zone blocks the straight line, this finds the shortest way round that zone over
        /// its corners (corners outside the map are skipped) and returns the first point of it; otherwise the
        /// destination itself. Stateless and exact for one rectangle, so re-evaluating it every step never makes the
        /// unit change its mind about which way to go round.
        /// </summary>
        public Vector2 NextWaypoint(Vector2 from, Vector2 destination, float radius)
        {
            foreach (var raw in _forbiddenZones)
            {
                var zone = Expand(raw, radius);
                if (SegmentCrossesInterior(from, destination, zone))
                    return FirstHopAround(zone, from, destination, radius);
            }
            return destination;
        }

        // Shortest path from -> destination on the visibility graph {from, zone corners, destination} (Dijkstra on at
        // most 6 nodes). Returns the node after 'from', or the destination if no way round exists.
        private Vector2 FirstHopAround(Rect zone, Vector2 from, Vector2 destination, float radius)
        {
            var walkable = new Rect(Bounds.xMin + radius, Bounds.yMin + radius, Bounds.width - radius * 2f,
                Bounds.height - radius * 2f);
            var nodes = new List<Vector2>(6) { from };
            foreach (var corner in Corners(Expand(zone, CornerClearance)))
            {
                if (walkable.Contains(corner) && (corner - from).sqrMagnitude > 1e-4f)
                    nodes.Add(corner);
            }
            nodes.Add(destination);
            var target = nodes.Count - 1;

            var count = nodes.Count;
            var cost = new float[count];
            var previous = new int[count];
            var done = new bool[count];
            for (var i = 0; i < count; i++)
            {
                cost[i] = float.PositiveInfinity;
                previous[i] = -1;
            }
            cost[0] = 0f;
            for (var iteration = 0; iteration < count; iteration++)
            {
                var current = -1;
                for (var i = 0; i < count; i++)
                {
                    if (!done[i] && (current < 0 || cost[i] < cost[current]))
                        current = i;
                }
                if (current < 0 || float.IsPositiveInfinity(cost[current]) || current == target)
                    break;
                done[current] = true;
                for (var next = 0; next < count; next++)
                {
                    if (done[next] || SegmentCrossesInterior(nodes[current], nodes[next], zone))
                        continue;
                    var candidate = cost[current] + Vector2.Distance(nodes[current], nodes[next]);
                    if (candidate < cost[next])
                    {
                        cost[next] = candidate;
                        previous[next] = current;
                    }
                }
            }

            if (previous[target] < 0)
                return destination;
            var hop = target;
            while (previous[hop] != 0)
                hop = previous[hop];
            return nodes[hop];
        }

        /// <summary>
        /// Safety net for one movement step: a step that would still enter a zone (separation pushes, rounding) keeps
        /// whichever single-axis part stays outside, else is pushed out to the nearest edge.
        /// </summary>
        public Vector2 ResolveStep(Vector2 from, Vector2 to, float radius)
        {
            if (!TryFindZone(to, radius, out _))
                return to;
            var xOnly = new Vector2(to.x, from.y);
            var yOnly = new Vector2(from.x, to.y);
            var xGain = TryFindZone(xOnly, radius, out _) ? -1f : Mathf.Abs(to.x - from.x);
            var yGain = TryFindZone(yOnly, radius, out _) ? -1f : Mathf.Abs(to.y - from.y);
            if (xGain < 0f && yGain < 0f)
                return PushOut(to, radius);
            return xGain >= yGain ? xOnly : yOnly;
        }

        private static Vector2[] Corners(Rect rect) => new[]
        {
            new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin),
            new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax)
        };

        // Liang–Barsky clip against the open rectangle (a segment running along an edge does not count).
        private static bool SegmentCrossesInterior(Vector2 a, Vector2 b, Rect rect)
        {
            const float inset = 1e-3f;
            var xMin = rect.xMin + inset;
            var xMax = rect.xMax - inset;
            var yMin = rect.yMin + inset;
            var yMax = rect.yMax - inset;
            var d = b - a;
            var t0 = 0f;
            var t1 = 1f;
            return Clip(-d.x, a.x - xMin, ref t0, ref t1) && Clip(d.x, xMax - a.x, ref t0, ref t1)
                   && Clip(-d.y, a.y - yMin, ref t0, ref t1) && Clip(d.y, yMax - a.y, ref t0, ref t1)
                   && t0 < t1;
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Approximately(p, 0f))
                return q > 0f;
            var t = q / p;
            if (p < 0f)
            {
                if (t > t1) return false;
                if (t > t0) t0 = t;
            }
            else
            {
                if (t < t0) return false;
                if (t < t1) t1 = t;
            }
            return true;
        }

        private bool TryFindZone(Vector2 point, float radius, out Rect grownZone)
        {
            foreach (var raw in _forbiddenZones)
            {
                grownZone = Expand(raw, radius);
                if (grownZone.Contains(point))
                    return true;
            }
            grownZone = default;
            return false;
        }

        /// <summary>Moves a point out of any forbidden zone (grown by <paramref name="radius"/>) to the nearest edge.</summary>
        public Vector2 PushOut(Vector2 point, float radius)
        {
            foreach (var raw in _forbiddenZones)
            {
                var zone = Expand(raw, radius);
                if (!zone.Contains(point))
                    continue;
                switch (NearestFace(zone, point))
                {
                    case Face.Left: point.x = zone.xMin - 1e-3f; break;
                    case Face.Right: point.x = zone.xMax + 1e-3f; break;
                    case Face.Bottom: point.y = zone.yMin - 1e-3f; break;
                    default: point.y = zone.yMax + 1e-3f; break;
                }
            }
            return point;
        }

        private enum Face
        {
            Left,
            Right,
            Bottom,
            Top
        }

        private static Rect Expand(Rect rect, float amount) =>
            new Rect(rect.xMin - amount, rect.yMin - amount, rect.width + amount * 2f, rect.height + amount * 2f);

        // Face of the rectangle closest to the point (signed distance outward, so an outside point picks the face it
        // is beyond).
        private static Face NearestFace(Rect zone, Vector2 point)
        {
            var left = point.x - zone.xMin;
            var right = zone.xMax - point.x;
            var bottom = point.y - zone.yMin;
            var top = zone.yMax - point.y;
            var best = Face.Left;
            var min = left;
            if (right < min) { min = right; best = Face.Right; }
            if (bottom < min) { min = bottom; best = Face.Bottom; }
            if (top < min) best = Face.Top;
            return best;
        }
    }
}
