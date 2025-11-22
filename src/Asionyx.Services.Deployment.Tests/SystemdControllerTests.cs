using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class SystemdControllerTests : TestBase
    {
        [Test]
        public void Post_Null_Request_Returns_BadRequest()
        {
            var controller = Get<Asionyx.Services.Deployment.Controllers.SystemdController>();

            var result = controller.Post(null).GetAwaiter().GetResult();
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void Post_No_Systemd_Executable_Returns_500()
        {
            var controller = Get<Asionyx.Services.Deployment.Controllers.SystemdController>();

            var req = new Asionyx.Services.Deployment.Controllers.SystemdRequest { Action = "status", Name = "dummy" };
            var result = controller.Post(req).GetAwaiter().GetResult();
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var obj = (ObjectResult)result;
            Assert.That(obj.StatusCode, Is.EqualTo(500));
        }
    }
}
