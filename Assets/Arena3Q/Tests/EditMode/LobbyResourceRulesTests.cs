using Arena3Q.Core;
using NUnit.Framework;

namespace Arena3Q.Tests.EditMode
{
    public sealed class LobbyResourceRulesTests
    {
        [TestCase(0L, "0")]
        [TestCase(10000L, "10.000")]
        [TestCase(12680L, "12.680")]
        [TestCase(99999L, "99.999")]
        [TestCase(100000L, "10 Vạn")]
        [TestCase(1234567L, "123,4 Vạn")]
        [TestCase(5320000L, "532 Vạn")]
        [TestCase(5329999L, "532,9 Vạn")]
        [TestCase(long.MaxValue, "922337203685477,5 Vạn")]
        public void WalletUsesVietnameseSeparatorsAndNeverRoundsUp(long amount, string expected) =>
            Assert.That(LobbyResourceRules.Compact(amount, 100000, 1, "Vạn"), Is.EqualTo(expected));

        [Test]
        public void ExactVanAndAlternativeDataSettingsAreSupported()
        {
            Assert.That(LobbyResourceRules.Compact(10000, 10000, 1, "Vạn"), Is.EqualTo("1 Vạn"));
            Assert.That(LobbyResourceRules.Compact(1234567, 100000, 0, "vạn"), Is.EqualTo("123 vạn"));
            Assert.That(LobbyResourceRules.Compact(1234567, 0, 1, "Vạn"), Is.EqualTo("1.234.567"));
            Assert.That(LobbyResourceRules.Exact(long.MaxValue), Is.EqualTo("9.223.372.036.854.775.807"));
        }

        [TestCase(0, 100, false, 0f)]
        [TestCase(50, 100, false, .5f)]
        [TestCase(100, 100, false, 1f)]
        [TestCase(500, 100, false, 1f)]
        [TestCase(0, 0, true, 1f)]
        public void ExperienceBarHandlesEmptyHalfFullAndMaximumLevel(int experience, int required, bool maxLevel, float expected) =>
            Assert.That(LobbyResourceRules.ExperienceFraction(experience, required, maxLevel), Is.EqualTo(expected));
    }
}
