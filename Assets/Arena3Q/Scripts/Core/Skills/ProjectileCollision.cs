using UnityEngine;

namespace Arena3Q.Core
{
    public static class ProjectileCollision
    {
        /// <summary>First segment entry into a circle, including a start already inside. Negative means no collision.</summary>
        public static float Entry(Vector2 start, Vector2 end, Vector2 center, float radius)
        {
            var offset = start - center; var c = offset.sqrMagnitude - radius * radius;
            if (c <= 0) return 0;
            var direction = end - start; var a = direction.sqrMagnitude;
            if (a < .0000001f) return -1;
            var b = Vector2.Dot(offset, direction); var discriminant = b * b - a * c;
            if (discriminant < 0) return -1;
            var t = (-b - Mathf.Sqrt(discriminant)) / a;
            return t >= 0 && t <= 1 ? t : -1;
        }
    }
}
