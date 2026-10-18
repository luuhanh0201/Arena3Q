using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>
    /// A forbidden zone shape (spec §9): blocks movement and command destinations. All queries take the unit radius,
    /// i.e. they work on the shape grown by that radius (Minkowski sum), so a unit's centre can be tested as a point.
    /// </summary>
    public interface IObstacle
    {
        /// <summary>Display name ("Sông", ...), for debugging and map tooling.</summary>
        string Name { get; }

        /// <summary>True if a unit of this radius centred at <paramref name="point"/> would overlap the obstacle.</summary>
        bool Contains(Vector2 point, float radius);

        /// <summary>True if a unit of this radius moving straight from a to b would overlap the obstacle.</summary>
        bool BlocksSegment(Vector2 a, Vector2 b, float radius);

        /// <summary>Points around the obstacle at <paramref name="clearance"/> from it, used as path nodes.</summary>
        void AddWaypoints(List<Vector2> nodes, float clearance);

        /// <summary>Moves a point that overlaps the obstacle to the nearest point that does not.</summary>
        Vector2 PushOut(Vector2 point, float radius);
    }

    /// <summary>Axis-aligned rectangular forbidden zone.</summary>
    public sealed class RectObstacle : IObstacle
    {
        public RectObstacle(Rect rect, string name = "Vùng cấm")
        {
            Rect = rect;
            Name = name;
        }

        public Rect Rect { get; }
        public string Name { get; }

        public bool Contains(Vector2 point, float radius) => Grow(Rect, radius).Contains(point);

        public bool BlocksSegment(Vector2 a, Vector2 b, float radius) =>
            Geometry2D.SegmentCrossesRectInterior(a, b, Grow(Rect, radius));

        public void AddWaypoints(List<Vector2> nodes, float clearance)
        {
            var r = Grow(Rect, clearance);
            nodes.Add(new Vector2(r.xMin, r.yMin));
            nodes.Add(new Vector2(r.xMax, r.yMin));
            nodes.Add(new Vector2(r.xMax, r.yMax));
            nodes.Add(new Vector2(r.xMin, r.yMax));
        }

        public Vector2 PushOut(Vector2 point, float radius)
        {
            var zone = Grow(Rect, radius);
            if (!zone.Contains(point))
                return point;
            var left = point.x - zone.xMin;
            var right = zone.xMax - point.x;
            var bottom = point.y - zone.yMin;
            var top = zone.yMax - point.y;
            var min = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top));
            if (min == left) point.x = zone.xMin - 1e-3f;
            else if (min == right) point.x = zone.xMax + 1e-3f;
            else if (min == bottom) point.y = zone.yMin - 1e-3f;
            else point.y = zone.yMax + 1e-3f;
            return point;
        }

        private static Rect Grow(Rect rect, float amount) =>
            new Rect(rect.xMin - amount, rect.yMin - amount, rect.width + amount * 2f, rect.height + amount * 2f);
    }

    /// <summary>Round forbidden zone (the pillar clusters of the Bát Quái ring, docs/BanDo/BatQuaiTran.md §1).</summary>
    public sealed class CircleObstacle : IObstacle
    {
        // Path nodes form a regular polygon round the circle; its edges stay clear of the grown circle.
        private const int WaypointCount = 8;

        public CircleObstacle(Vector2 center, float radius, string name = "Cột")
        {
            if (radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            Center = center;
            Radius = radius;
            Name = name;
        }

        public Vector2 Center { get; }
        public float Radius { get; }
        public string Name { get; }

        public bool Contains(Vector2 point, float radius) => (point - Center).sqrMagnitude < Sqr(Radius + radius);

        public bool BlocksSegment(Vector2 a, Vector2 b, float radius) =>
            Geometry2D.PointSegmentDistance(Center, a, b) < Radius + radius;

        public void AddWaypoints(List<Vector2> nodes, float clearance)
        {
            var distance = (Radius + clearance) / Mathf.Cos(Mathf.PI / WaypointCount);
            for (var i = 0; i < WaypointCount; i++)
            {
                var angle = (i + 0.5f) * 2f * Mathf.PI / WaypointCount;
                nodes.Add(Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance);
            }
        }

        public Vector2 PushOut(Vector2 point, float radius)
        {
            var offset = point - Center;
            var limit = Radius + radius;
            if (offset.sqrMagnitude >= limit * limit)
                return point;
            var direction = offset.sqrMagnitude > 1e-10f ? offset.normalized : Vector2.right;
            return Center + direction * (limit + 1e-3f);
        }

        private static float Sqr(float value) => value * value;
    }

    /// <summary>
    /// A straight wall with a thickness that can be switched on and off (the slab of a closed Bát Quái gate,
    /// docs/BanDo/BatQuaiTran.md §3). While off it blocks nothing. Whoever switches it must call
    /// <see cref="BattlefieldLayout.RebuildRoutes"/> so routes follow the change.
    /// </summary>
    public sealed class WallObstacle : IObstacle
    {
        public WallObstacle(Vector2 a, Vector2 b, float thickness, string name = "Phiến đá", bool active = true)
        {
            if (thickness <= 0f)
                throw new ArgumentOutOfRangeException(nameof(thickness));
            A = a;
            B = b;
            HalfThickness = thickness * 0.5f;
            Name = name;
            IsActive = active;
        }

        public Vector2 A { get; }
        public Vector2 B { get; }
        public float HalfThickness { get; }
        public string Name { get; }
        public bool IsActive { get; set; }

        public bool Contains(Vector2 point, float radius) =>
            IsActive && Geometry2D.PointSegmentDistance(point, A, B) < HalfThickness + radius;

        public bool BlocksSegment(Vector2 a, Vector2 b, float radius) =>
            IsActive && Geometry2D.SegmentSegmentDistance(a, b, A, B) < HalfThickness + radius;

        public void AddWaypoints(List<Vector2> nodes, float clearance)
        {
            // Corners of the grown wall; kept while the wall is off too (they are ordinary free points then).
            var along = (B - A).normalized;
            var side = new Vector2(-along.y, along.x);
            var d = HalfThickness + clearance;
            nodes.Add(A - along * d + side * d);
            nodes.Add(A - along * d - side * d);
            nodes.Add(B + along * d + side * d);
            nodes.Add(B + along * d - side * d);
        }

        /// <summary>Pushes a point to the nearer side of the wall (units caught in a closing gate step out).</summary>
        public Vector2 PushOut(Vector2 point, float radius)
        {
            if (!IsActive)
                return point;
            var limit = HalfThickness + radius;
            var closest = Geometry2D.ClosestPointOnSegment(point, A, B);
            var offset = point - closest;
            var distance = offset.magnitude;
            if (distance >= limit)
                return point;
            Vector2 away;
            if (distance > 1e-5f)
                away = offset / distance;
            else
            {
                var along = (B - A).normalized;
                away = new Vector2(-along.y, along.x);
            }
            return closest + away * (limit + 1e-3f);
        }
    }

    /// <summary>A place where a river can be crossed (bridge or ford).</summary>
    public readonly struct RiverCrossing
    {
        public readonly Vector2 Center;
        /// <summary>Unit vector along the river at the crossing.</summary>
        public readonly Vector2 Along;
        /// <summary>Clear walking width of the crossing, measured along the river.</summary>
        public readonly float Width;

        public RiverCrossing(Vector2 center, Vector2 along, float width)
        {
            Center = center;
            Along = along;
            Width = width;
        }
    }

    /// <summary>
    /// A river (spec §9, provisional): a polyline centre line with a width. Crossings cut gaps in the water so units
    /// can pass; the gap is <c>crossing width + river width</c> long so the walkable passage is the crossing width.
    /// </summary>
    public sealed class RiverObstacle : IObstacle
    {
        private readonly List<Vector2[]> _wetPieces = new List<Vector2[]>();
        private readonly List<RiverCrossing> _crossings = new List<RiverCrossing>();

        public RiverObstacle(IReadOnlyList<Vector2> path, float width, IReadOnlyList<(Vector2 position, float width)> crossings,
            string name = "Sông")
        {
            if (path == null || path.Count < 2)
                throw new ArgumentException("A river needs at least two centre-line points.", nameof(path));
            if (width <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width));

            Name = name;
            Path = new List<Vector2>(path).ToArray();
            Width = width;
            HalfWidth = width * 0.5f;
            BuildWetPieces(crossings ?? Array.Empty<(Vector2, float)>());
        }

        public string Name { get; }
        public IReadOnlyList<Vector2> Path { get; }
        public float Width { get; }
        public float HalfWidth { get; }
        public IReadOnlyList<RiverCrossing> Crossings => _crossings;

        /// <summary>Stretches of water that block movement (the centre line minus the crossing gaps).</summary>
        public IReadOnlyList<Vector2[]> WetPieces => _wetPieces;

        public bool Contains(Vector2 point, float radius) => DistanceToWater(point) < HalfWidth + radius;

        public bool BlocksSegment(Vector2 a, Vector2 b, float radius)
        {
            var limit = HalfWidth + radius;
            foreach (var piece in _wetPieces)
            {
                for (var i = 0; i < piece.Length - 1; i++)
                {
                    if (Geometry2D.SegmentSegmentDistance(a, b, piece[i], piece[i + 1]) < limit)
                        return true;
                }
            }
            return false;
        }

        public void AddWaypoints(List<Vector2> nodes, float clearance)
        {
            var d = HalfWidth + clearance;
            foreach (var piece in _wetPieces)
            {
                var last = piece.Length - 1;
                for (var i = 0; i <= last; i++)
                {
                    // Offset along the (mitred) normal so straight runs keep exactly distance d from the centre line.
                    var nPrev = i > 0 ? Normal(piece[i - 1], piece[i]) : Normal(piece[0], piece[1]);
                    var nNext = i < last ? Normal(piece[i], piece[i + 1]) : nPrev;
                    var miter = (nPrev + nNext).normalized;
                    if (miter.sqrMagnitude < 1e-6f)
                        miter = nPrev;
                    var length = Mathf.Min(d / Mathf.Max(0.2f, Vector2.Dot(miter, nPrev)), d * 3f);
                    nodes.Add(piece[i] + miter * length);
                    nodes.Add(piece[i] - miter * length);
                }

                // Square caps beyond both ends, so units can walk round the end of a stretch of water.
                AddCap(nodes, piece[0], (piece[0] - piece[1]).normalized, d);
                AddCap(nodes, piece[last], (piece[last] - piece[last - 1]).normalized, d);
            }
        }

        public Vector2 PushOut(Vector2 point, float radius)
        {
            var limit = HalfWidth + radius;
            for (var iteration = 0; iteration < 3; iteration++)
            {
                var closest = ClosestWaterPoint(point, out var distance, out var segmentNormal);
                if (distance >= limit)
                    return point;
                var away = distance > 1e-5f ? (point - closest) / distance : segmentNormal;
                point = closest + away * (limit + 1e-3f);
            }
            return point;
        }

        private static void AddCap(List<Vector2> nodes, Vector2 end, Vector2 outward, float d)
        {
            var side = new Vector2(-outward.y, outward.x);
            nodes.Add(end + outward * d + side * d);
            nodes.Add(end + outward * d - side * d);
        }

        private static Vector2 Normal(Vector2 a, Vector2 b)
        {
            var t = (b - a).normalized;
            return new Vector2(-t.y, t.x);
        }

        private float DistanceToWater(Vector2 point)
        {
            ClosestWaterPoint(point, out var distance, out _);
            return distance;
        }

        private Vector2 ClosestWaterPoint(Vector2 point, out float distance, out Vector2 segmentNormal)
        {
            var best = point;
            distance = float.PositiveInfinity;
            segmentNormal = Vector2.up;
            foreach (var piece in _wetPieces)
            {
                for (var i = 0; i < piece.Length - 1; i++)
                {
                    var c = Geometry2D.ClosestPointOnSegment(point, piece[i], piece[i + 1]);
                    var dist = Vector2.Distance(point, c);
                    if (dist < distance)
                    {
                        distance = dist;
                        best = c;
                        segmentNormal = Normal(piece[i], piece[i + 1]);
                    }
                }
            }
            return best;
        }

        // Splits the centre line into wet stretches by removing a gap around each crossing.
        private void BuildWetPieces(IReadOnlyList<(Vector2 position, float width)> crossings)
        {
            var cumulative = new float[Path.Count];
            for (var i = 1; i < Path.Count; i++)
                cumulative[i] = cumulative[i - 1] + Vector2.Distance(Path[i - 1], Path[i]);
            var total = cumulative[Path.Count - 1];

            var gaps = new List<(float start, float end)>();
            foreach (var (position, crossingWidth) in crossings)
            {
                var s = ProjectToArcLength(position, cumulative, out var center, out var along);
                var half = (crossingWidth + Width) * 0.5f;
                gaps.Add((s - half, s + half));
                _crossings.Add(new RiverCrossing(center, along, crossingWidth));
            }
            gaps.Sort((a, b) => a.start.CompareTo(b.start));

            var cursor = 0f;
            foreach (var (start, end) in gaps)
            {
                if (start > cursor)
                    AddPiece(cursor, start, cumulative);
                cursor = Mathf.Max(cursor, end);
            }
            if (cursor < total)
                AddPiece(cursor, total, cumulative);
        }

        private void AddPiece(float from, float to, float[] cumulative)
        {
            if (to - from < 1e-3f)
                return;
            var points = new List<Vector2> { PointAt(from, cumulative) };
            for (var i = 0; i < Path.Count; i++)
            {
                if (cumulative[i] > from && cumulative[i] < to)
                    points.Add(Path[i]);
            }
            points.Add(PointAt(to, cumulative));
            _wetPieces.Add(points.ToArray());
        }

        private Vector2 PointAt(float s, float[] cumulative)
        {
            s = Mathf.Clamp(s, 0f, cumulative[cumulative.Length - 1]);
            for (var i = 1; i < Path.Count; i++)
            {
                if (s <= cumulative[i])
                {
                    var length = cumulative[i] - cumulative[i - 1];
                    var t = length > 1e-6f ? (s - cumulative[i - 1]) / length : 0f;
                    return Vector2.Lerp(Path[i - 1], Path[i], t);
                }
            }
            return Path[Path.Count - 1];
        }

        private float ProjectToArcLength(Vector2 point, float[] cumulative, out Vector2 center, out Vector2 along)
        {
            var bestS = 0f;
            var bestDistance = float.PositiveInfinity;
            center = Path[0];
            along = (Path[1] - Path[0]).normalized;
            for (var i = 0; i < Path.Count - 1; i++)
            {
                var c = Geometry2D.ClosestPointOnSegment(point, Path[i], Path[i + 1]);
                var dist = Vector2.Distance(point, c);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestS = cumulative[i] + Vector2.Distance(Path[i], c);
                    center = c;
                    along = (Path[i + 1] - Path[i]).normalized;
                }
            }
            return bestS;
        }
    }

    public static class Geometry2D
    {
        public static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            if (lengthSq < 1e-12f)
                return a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
            return a + ab * t;
        }

        public static float PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b) =>
            Vector2.Distance(p, ClosestPointOnSegment(p, a, b));

        public static float SegmentSegmentDistance(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            if (SegmentsIntersect(a, b, c, d))
                return 0f;
            return Mathf.Min(Mathf.Min(PointSegmentDistance(a, c, d), PointSegmentDistance(b, c, d)),
                Mathf.Min(PointSegmentDistance(c, a, b), PointSegmentDistance(d, a, b)));
        }

        public static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var d1 = Cross(d - c, a - c);
            var d2 = Cross(d - c, b - c);
            var d3 = Cross(b - a, c - a);
            var d4 = Cross(b - a, d - a);
            return ((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) && ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f));
        }

        /// <summary>Liang–Barsky clip against the open rectangle (a segment running along an edge does not count).</summary>
        public static bool SegmentCrossesRectInterior(Vector2 a, Vector2 b, Rect rect)
        {
            const float inset = 1e-3f;
            var xMin = rect.xMin + inset;
            var xMax = rect.xMax - inset;
            var yMin = rect.yMin + inset;
            var yMax = rect.yMax - inset;
            var delta = b - a;
            var t0 = 0f;
            var t1 = 1f;
            return Clip(-delta.x, a.x - xMin, ref t0, ref t1) && Clip(delta.x, xMax - a.x, ref t0, ref t1)
                   && Clip(-delta.y, a.y - yMin, ref t0, ref t1) && Clip(delta.y, yMax - a.y, ref t0, ref t1)
                   && t0 < t1;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

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
    }
}
