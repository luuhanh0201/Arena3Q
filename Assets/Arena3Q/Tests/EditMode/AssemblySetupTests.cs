using NUnit.Framework;

namespace Arena3Q.Tests.EditMode
{
    // Smoke test: confirms the EditMode test assembly compiles and the runner discovers it.
    public class AssemblySetupTests
    {
        [Test]
        public void TestAssembly_IsDiscoveredByRunner()
        {
            Assert.Pass();
        }
    }
}
