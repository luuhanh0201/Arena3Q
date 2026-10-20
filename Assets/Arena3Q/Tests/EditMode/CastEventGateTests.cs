using Arena3Q.Core;
using NUnit.Framework;

namespace Arena3Q.Tests.EditMode
{
    public class CastEventGateTests
    {
        [Test] public void BlendedClipCannotRepeatTheSameEvent_ButComboHitsAreDistinct()
        {
            var gate = new CastEventGate();
            var cast = gate.Begin();
            Assert.That(gate.TryAccept(cast, "TripleFeather:0"), Is.True);
            Assert.That(gate.TryAccept(cast, "TripleFeather:0"), Is.False);
            Assert.That(gate.TryAccept(cast, "TripleFeather:1"), Is.True);
            Assert.That(gate.TryAccept(cast, "TripleFeather:2"), Is.True);
        }
        [Test] public void CancelledOrReplacedCastCannotSpawnLateEffects()
        {
            var gate = new CastEventGate();
            var oldCast = gate.Begin();
            gate.Cancel();
            Assert.That(gate.TryAccept(oldCast, "WindSlash"), Is.False);
            var next = gate.Begin();
            Assert.That(gate.TryAccept(oldCast, "WindSlash"), Is.False);
            Assert.That(gate.TryAccept(next, "WindSlash"), Is.True);
        }
    }
}
