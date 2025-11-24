using System.Threading;
using System.Threading.Tasks;
using Asionyx.Services.Deployment.Services;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class ProcessRunnerTests
    {
        [Test]
        public async Task RunAsync_Dotnet_Version_Returns_Exit0()
        {
            var runner = new DefaultProcessRunner();
            var (exit, stdout, stderr) = await runner.RunAsync("dotnet", "--version", timeoutMs: 10000);
            Assert.That(exit, Is.EqualTo(0));
            Assert.That(stdout, Is.Not.Null.And.Not.Empty);
            Assert.That(stderr, Is.Null.Or.Empty);
        }

        [Test]
        public async Task RunAsync_Respects_Timeout()
        {
            var runner = new DefaultProcessRunner();
            // Use a very small timeout to force timeout behaviour
            var (exit, stdout, stderr) = await runner.RunAsync("dotnet", "--version", timeoutMs: 1);
            Assert.That(exit, Is.EqualTo(-1));
            Assert.That(stderr, Is.Not.Null.And.Not.Empty);
        }
    }
}
