using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public class ProjectileCollisionTests
    {
        [Test] public void FastProjectileCannotTunnelThroughTarget()
        {
            Assert.That(ProjectileCollision.Entry(Vector2.zero, Vector2.right * 10, Vector2.right * 5, 1), Is.EqualTo(.4f).Within(.00001f));
            Assert.That(ProjectileCollision.Entry(Vector2.zero, Vector2.right * 10, new Vector2(5, 2), 1), Is.LessThan(0));
        }
        [Test] public void SurfaceEntryOrdersTargetsRatherThanTheirCentres()
        {
            var small = ProjectileCollision.Entry(Vector2.zero, Vector2.right * 10, Vector2.right * 4, .1f);
            var large = ProjectileCollision.Entry(Vector2.zero, Vector2.right * 10, Vector2.right * 5, 2);
            Assert.That(large, Is.LessThan(small));
            Assert.That(ProjectileCollision.Entry(Vector2.zero, Vector2.zero, Vector2.zero, 1), Is.Zero);
        }
    }
}
