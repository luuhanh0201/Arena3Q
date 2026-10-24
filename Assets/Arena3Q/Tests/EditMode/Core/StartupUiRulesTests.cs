using Arena3Q.Core;
using NUnit.Framework;

namespace Arena3Q.Tests.EditMode
{
    public sealed class StartupUiRulesTests
    {
        [TestCase("player@example.com", true)]
        [TestCase("+84901234567", true)]
        [TestCase("0901234567", true)]
        [TestCase("name", false)]
        [TestCase("a@@example.com", false)]
        [TestCase("a@b.", false)]
        [TestCase("a b@example.com", false)]
        [TestCase("12abc456789", false)]
        public void Login_ValidatesEmailOrPhone(string address, bool valid) => Assert.That(AccountRules.IsEmailOrPhone(address), Is.EqualTo(valid));

        [Test]
        public void Verification_RequiresSixAsciiDigits()
        {
            Assert.That(AccountRules.IsVerificationCode("012345"), Is.True);
            Assert.That(AccountRules.IsVerificationCode("12345"), Is.False);
            Assert.That(AccountRules.IsVerificationCode("12345A"), Is.False);
        }
        [Test]
        public void Network_UsesTelemetryForGoodWeakStallAndTimeout()
        {
            var monitor = new StartupNetworkMonitor(new StartupNetworkSettings());
            monitor.Observe(1, 2 * 1024 * 1024, true, 40);
            Assert.That(monitor.State, Is.EqualTo(StartupNetState.Good));
            monitor.Observe(1, 80 * 1024, true, 300);
            Assert.That(monitor.State, Is.EqualTo(StartupNetState.Weak));
            monitor.Observe(3, 0, true, 300);
            Assert.That(monitor.State, Is.EqualTo(StartupNetState.Stalled));
            Assert.That(monitor.Attempt, Is.Zero, "stall detection must not consume the first retry delay");
            monitor.Observe(.1f, 0, true, 300, 5.1f);
            Assert.That(monitor.State, Is.EqualTo(StartupNetState.ServerTimeout));
        }
        [Test]
        public void Network_RetriesAtTwoFourEight_ThenCanRecoverWithoutResettingProgress()
        {
            var monitor = new StartupNetworkMonitor(new StartupNetworkSettings());
            monitor.Observe(3, 0, true, 0);
            foreach (var delay in new[] { 2f, 4f, 8f })
            {
                monitor.Observe(delay, 0, true, 0);
                Assert.That(monitor.RetryDue, Is.True);
            }
            monitor.Observe(.1f, 0, true, 0);
            Assert.That(monitor.State, Is.EqualTo(StartupNetState.Lost));
            monitor.Retry(); Assert.That(monitor.State, Is.EqualTo(StartupNetState.Retrying));
            monitor.Observe(1, 2 * 1024 * 1024, true, 30);
            Assert.That(monitor.State, Is.EqualTo(StartupNetState.Good));
            Assert.That(monitor.Attempt, Is.Zero);
        }
        [Test]
        public void Network_DisconnectWinsOverOtherStates()
        {
            var monitor = new StartupNetworkMonitor(new StartupNetworkSettings());
            monitor.Observe(1, 0, false, 0, 10);
            Assert.That(monitor.State, Is.EqualTo(StartupNetState.Lost));
        }
        [Test]
        public void DefaultServer_SkipsFullAndMaintenance()
        {
            var servers = new[] { new ServerInfo("s1", "Full", ServerStatus.Full, true), new ServerInfo("s2", "Maint", ServerStatus.Maintenance, true), new ServerInfo("s3", "Open", ServerStatus.Good, false) };
            Assert.That(StaticServerDirectory.PickDefault(servers, "s1").Id, Is.EqualTo("s3"));
        }
    }
}
