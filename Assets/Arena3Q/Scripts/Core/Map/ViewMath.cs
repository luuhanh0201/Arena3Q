using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>Camera limits for the battlefield (spec §9): the view never leaves the map bounds.</summary>
    public static class CameraMath
    {
        /// <summary>Largest orthographic size whose view still fits inside the bounds.</summary>
        public static float MaxOrthographicSize(Rect bounds, float aspect) =>
            Mathf.Max(0.01f, Mathf.Min(bounds.height * 0.5f, bounds.width * 0.5f / Mathf.Max(0.01f, aspect)));

        /// <summary>Clamps a camera centre so the view stays inside the bounds; centres an axis the view is wider than.</summary>
        public static Vector2 ClampCenter(Rect bounds, Vector2 center, float orthographicSize, float aspect)
        {
            var halfHeight = orthographicSize;
            var halfWidth = orthographicSize * aspect;
            return new Vector2(ClampAxis(center.x, bounds.xMin, bounds.xMax, halfWidth),
                ClampAxis(center.y, bounds.yMin, bounds.yMax, halfHeight));
        }

        private static float ClampAxis(float value, float min, float max, float halfExtent)
        {
            if (max - min <= halfExtent * 2f)
                return (min + max) * 0.5f;
            return Mathf.Clamp(value, min + halfExtent, max - halfExtent);
        }
    }

    /// <summary>Off-screen hero icons (spec §9): where on the screen edge to show a unit that is out of view.</summary>
    public static class EdgeIndicatorMath
    {
        public static bool IsOnScreen(Vector2 viewport) =>
            viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;

        /// <summary>
        /// Projects a viewport point (0..1, may be outside) onto the screen border along the ray from the screen centre,
        /// inset by <paramref name="margin"/> (viewport units).
        /// </summary>
        public static Vector2 ClampToEdge(Vector2 viewport, float margin) =>
            ClampToArea(viewport, new Rect(margin, margin, 1f - margin * 2f, 1f - margin * 2f));

        /// <summary>
        /// Like <see cref="ClampToEdge"/> but inside an arbitrary viewport area, e.g. the screen minus the card bar,
        /// so icons never sit under other UI. The ray starts at the screen centre, which must lie inside the area.
        /// </summary>
        public static Vector2 ClampToArea(Vector2 viewport, Rect area)
        {
            var center = new Vector2(0.5f, 0.5f);
            var dir = viewport - center;
            if (dir.sqrMagnitude < 1e-8f)
                return center;
            var scale = 1f;
            if (dir.x > 0f) scale = Mathf.Min(scale, (area.xMax - center.x) / dir.x);
            if (dir.x < 0f) scale = Mathf.Min(scale, (area.xMin - center.x) / dir.x);
            if (dir.y > 0f) scale = Mathf.Min(scale, (area.yMax - center.y) / dir.y);
            if (dir.y < 0f) scale = Mathf.Min(scale, (area.yMin - center.y) / dir.y);
            return center + dir * Mathf.Max(0f, scale);
        }
    }

    /// <summary>Formation slots around a hero (spec §7): concentric rings, 6 soldiers on the first ring.</summary>
    public static class FormationMath
    {
        public static Vector2 SlotOffset(int index, float spacing)
        {
            var ring = 0;
            var ringCapacity = 6;
            var start = 0;
            while (index >= start + ringCapacity)
            {
                start += ringCapacity;
                ring++;
                ringCapacity = 6 * (ring + 1);
            }
            var slotInRing = index - start;
            var angle = (slotInRing / (float)ringCapacity) * Mathf.PI * 2f + ring * 0.3f;
            var radius = spacing * (ring + 1);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }

    /// <summary>Tap vs drag classification (spec §5): a tap is short and barely moves.</summary>
    public static class GestureRules
    {
        public static bool IsTap(Vector2 startScreen, Vector2 endScreen, float durationSeconds, float maxMovePixels,
            float maxDurationSeconds) =>
            (endScreen - startScreen).sqrMagnitude <= maxMovePixels * maxMovePixels && durationSeconds <= maxDurationSeconds;

        public static bool HasMovedBeyond(Vector2 startScreen, Vector2 currentScreen, float maxMovePixels) =>
            (currentScreen - startScreen).sqrMagnitude > maxMovePixels * maxMovePixels;
    }

    /// <summary>
    /// Tilted orthographic battle camera (docs/KeHoach/ChuyenSang3D.md §12): the ground is the logic plane (XY, z = 0),
    /// "up" is -Z, and the camera is turned about X by the tilt, so the ground shows squashed by cos(tilt) on screen.
    /// The "screen plane" is the old 2.5D view space: (x, y · cos(tilt)).
    /// </summary>
    public static class TiltedViewMath
    {
        /// <summary>Rotation of the camera (and of upright billboards) for a tilt in degrees.</summary>
        public static Quaternion CameraRotation(float tiltDegrees) => Quaternion.Euler(-tiltDegrees, 0f, 0f);

        /// <summary>Ground point under a screen-plane point.</summary>
        public static Vector2 PlaneToGround(Vector2 plane, float groundProjection) =>
            new Vector2(plane.x, plane.y / Mathf.Max(0.01f, groundProjection));

        public static Vector2 GroundToPlane(Vector2 ground, float groundProjection) =>
            new Vector2(ground.x, ground.y * groundProjection);

        /// <summary>Where a ray meets the ground (z = 0); false when it runs parallel to the ground.</summary>
        public static bool RayToGround(Vector3 origin, Vector3 direction, out Vector2 ground)
        {
            ground = default;
            if (Mathf.Abs(direction.z) < 1e-6f)
                return false;
            var t = -origin.z / direction.z;
            var hit = origin + direction * t;
            ground = new Vector2(hit.x, hit.y);
            return true;
        }

        /// <summary>Camera position that looks at a ground point from <paramref name="distance"/> away.</summary>
        public static Vector3 CameraPosition(Vector2 ground, float tiltDegrees, float distance) =>
            new Vector3(ground.x, ground.y, 0f) - CameraRotation(tiltDegrees) * Vector3.forward * distance;
    }
}
