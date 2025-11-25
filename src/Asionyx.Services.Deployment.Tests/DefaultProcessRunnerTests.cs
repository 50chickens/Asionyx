using System.Threading.Tasks;
using NUnit.Framework;
using Asionyx.Services.Deployment.Services;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class DefaultProcessRunnerTests
    {
        [Test]
        public async Task RunAsync_DotnetVersion_ReturnsExit0()
        {
            var runner = new DefaultProcessRunner();
            var (exit, stdout, stderr) = await runner.RunAsync("dotnet", "--version");
            Assert.That(exit, Is.EqualTo(0));
            Assert.That(stdout, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task RunAsync_InvalidExecutable_Returns_NegativeExit()
        {
            var runner = new DefaultProcessRunner();
            var (exit, stdout, stderr) = await runner.RunAsync("no-such-exe-12345", "");
            Assert.That(exit, Is.EqualTo(-1));
            Assert.That(stderr, Is.Not.Null.And.Not.Empty);
        }
    }
}
