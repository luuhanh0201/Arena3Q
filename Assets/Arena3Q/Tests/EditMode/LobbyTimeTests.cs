using Arena3Q.Core;
using NUnit.Framework;

namespace Arena3Q.Tests.EditMode
{
    public sealed class LobbyTimeTests
    {
        [TestCase(0, 4)] [TestCase(4, 4)] [TestCase(5, 0)] [TestCase(10, 0)]
        [TestCase(11, 1)] [TestCase(15, 2)] [TestCase(18, 3)] [TestCase(21, 4)] [TestCase(24, 4)]
        public void UsesConfiguredBoundaryAndWrapsMidnight(int hour, int expected) =>
            Assert.That(LobbyTimeRules.IndexAtHour(hour, new[] { 5, 11, 15, 18, 21 }), Is.EqualTo(expected));

        [Test]
        public void ReturningPlayerOpensLobbyButNewPlayerFirstPlaysTutorial()
        {
            var planner = new FrontendFlowPlanner(new[] { FrontendStep.ServerSelect, FrontendStep.TutorialBattle, FrontendStep.Lobby, FrontendStep.Battle });
            Assert.That(planner.Next(FrontendStep.ServerSelect, new FrontendState(true, true, true, true, true, true)), Is.EqualTo(FrontendStep.Lobby));
            Assert.That(planner.Next(FrontendStep.ServerSelect, new FrontendState(true, true, true, true, true, false)), Is.EqualTo(FrontendStep.TutorialBattle));
        }
    }
}
