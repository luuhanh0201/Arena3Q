using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>The fixed diagonal battle camera (user 2026-10-28): footprint, limits and the turn's direction.</summary>
    public class DiagonalViewMathTests
    {
        private static readonly Quaternion Diagonal = TiltedViewMath.CameraRotation(40f, 45f);
        private const float Aspect = 2340f / 1080f;

        [Test]
        public void PositiveYaw_ShowsTheMapsXAxisTowardsTheTopRight()
        {
            var right = Diagonal * Vector3.right;
            var up = Diagonal * Vector3.up;
            // Screen components of the ground's +x: along the camera's right and up axes.
            Assert.That(Vector3.Dot(Vector3.right, right), Is.GreaterThan(0f));
            Assert.That(Vector3.Dot(Vector3.right, up), Is.GreaterThan(0f));
            // And +y towards the top left.
            Assert.That(Vector3.Dot(Vector3.up, right), Is.LessThan(0f));
            Assert.That(Vector3.Dot(Vector3.up, up), Is.GreaterThan(0f));
        }

        [Test]
        public void WithoutYaw_MatchesTheOldTiltOnlyCamera()
        {
            Assert.That(Quaternion.Angle(TiltedViewMath.CameraRotation(40f, 0f), TiltedViewMath.CameraRotation(40f)), Is.LessThan(1e-3f));
        }

        [Test]
        public void GroundOffset_OfTheViewCentre_IsZero_AndCornersLieOnTheGround()
        {
            Assert.That(TiltedViewMath.GroundOffset(Diagonal, 10f, Aspect, Vector2.zero).magnitude, Is.LessThan(1e-4f));
            // A corner's ray from the camera plane really meets z = 0 at the returned offset.
            var offset = TiltedViewMath.GroundOffset(Diagonal, 10f, Aspect, new Vector2(1f, 1f));
            var origin = TiltedViewMath.CameraPosition(Vector2.zero, Diagonal, 60f) + Diagonal * new Vector3(10f * Aspect, 10f, 0f);
            Assert.That(TiltedViewMath.RayToGround(origin, Diagonal * Vector3.forward, out var hit), Is.True);
            Assert.That(Vector2.Distance(hit, offset), Is.LessThan(1e-3f));
        }

        [Test]
        public void MaxSizeAndClampedFocus_KeepTheWholeFootprintInsideTheGround()
        {
            var ground = new Rect(-64f, -46f, 128f, 92f); // the 80×44 map plus 24 of outer ground
            var max = TiltedViewMath.MaxOrthographicSize(ground, Diagonal, Aspect);
            var box = TiltedViewMath.FootprintBox(Diagonal, max, Aspect);
            Assert.That(box.width <= ground.width + 1e-3f && box.height <= ground.height + 1e-3f, Is.True);
            Assert.That(Mathf.Max(box.width / ground.width, box.height / ground.height), Is.EqualTo(1f).Within(1e-3f), "fills one axis");

            foreach (var wanted in new[] { new Vector2(500f, 0f), new Vector2(-500f, -500f), new Vector2(0f, 300f) })
            {
                var size = max * 0.7f;
                var focus = TiltedViewMath.ClampFocus(ground, wanted, Diagonal, size, Aspect);
                var footprint = TiltedViewMath.FootprintBox(Diagonal, size, Aspect);
                Assert.That(focus.x + footprint.xMin, Is.GreaterThanOrEqualTo(ground.xMin - 1e-3f));
                Assert.That(focus.x + footprint.xMax, Is.LessThanOrEqualTo(ground.xMax + 1e-3f));
                Assert.That(focus.y + footprint.yMin, Is.GreaterThanOrEqualTo(ground.yMin - 1e-3f));
                Assert.That(focus.y + footprint.yMax, Is.LessThanOrEqualTo(ground.yMax + 1e-3f));
            }
        }
    }
}
