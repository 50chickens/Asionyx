using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Asionyx.Services.Deployment.Controllers;
using Asionyx.Services.Deployment.Services;

namespace Asionyx.Services.Deployment.Tests
{
    class StubRunner : IProcessRunner
    {
        private readonly int _exit;
        private readonly string _out;
        private readonly string _err;
        public StubRunner(int exit, string @out = "", string err = "") { _exit = exit; _out = @out; _err = err; }
        public Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(string fileName, string args, int timeoutMs = 60000, System.Threading.CancellationToken ct = default)
        {
            return Task.FromResult((_exit, _out, _err));
        }
    }

    [TestFixture]
    public class PackagesControllerUnitTests
    {
        [Test]
        public async Task Get_Returns_Ok_With_Packages_When_Exit0()
        {
            var runner = new StubRunner(0, "pkg1\npkg2");
            var controller = new PackagesController(runner);
            var res = await controller.Get();
            Assert.That(res, Is.TypeOf<OkObjectResult>());
            var ok = res as OkObjectResult;
            Assert.That(ok!.Value, Is.TypeOf<PackagesListDto>());
            var dto = ok.Value as PackagesListDto;
            Assert.That(dto!.Packages, Is.EqualTo("pkg1\npkg2"));
        }

        [Test]
        public async Task Get_Returns_500_When_NonZeroExit()
        {
            var runner = new StubRunner(1, "", "error");
            var controller = new PackagesController(runner);
            var res = await controller.Get();
            Assert.That(res, Is.TypeOf<ObjectResult>());
            var obj = res as ObjectResult;
            Assert.That(obj!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task Post_Install_Returns_Ok_On_Success()
        {
            var runner = new StubRunner(0, "installed output");
            var controller = new PackagesController(runner);
            var req = new PackagesController.PackageRequest("install", new[] { "pkgA" });
            var res = await controller.Post(req);
            Assert.That(res, Is.TypeOf<OkObjectResult>());
            var ok = res as OkObjectResult;
            Assert.That(ok!.Value, Is.TypeOf<PackageActionResultDto>());
            var dto = ok.Value as PackageActionResultDto;
            Assert.That(dto!.Result, Is.EqualTo("installed"));
        }

        [Test]
        public async Task Post_Remove_Returns_Ok_On_Success()
        {
            var runner = new StubRunner(0, "removed output");
            var controller = new PackagesController(runner);
            var req = new PackagesController.PackageRequest("remove", new[] { "pkgA" });
            var res = await controller.Post(req);
            Assert.That(res, Is.TypeOf<OkObjectResult>());
            var ok = res as OkObjectResult;
            Assert.That(ok!.Value, Is.TypeOf<PackageActionResultDto>());
            var dto = ok.Value as PackageActionResultDto;
            Assert.That(dto!.Result, Is.EqualTo("removed"));
        }

        [Test]
        public async Task Post_Install_Returns_500_On_Failure()
        {
            var runner = new StubRunner(1, "", "err");
            var controller = new PackagesController(runner);
            var req = new PackagesController.PackageRequest("install", new[] { "pkgA" });
            var res = await controller.Post(req);
            Assert.That(res, Is.TypeOf<ObjectResult>());
            var obj = res as ObjectResult;
            Assert.That(obj!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task Post_InvalidAction_Returns_BadRequest()
        {
            var runner = new StubRunner(0, "ok");
            var controller = new PackagesController(runner);
            var req = new PackagesController.PackageRequest("unknown", new[] { "pkgA" });
            var res = await controller.Post(req);
            Assert.That(res, Is.InstanceOf<ObjectResult>());
            var obj = res as ObjectResult;
            Assert.That(obj!.StatusCode, Is.EqualTo(400));
        }
    }
}
