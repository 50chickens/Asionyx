using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class PackagesControllerTests : TestBase
    {
        [Test]
        public void Post_Invalid_Request_Returns_BadRequest()
        {
            var controller = Get<Asionyx.Services.Deployment.Controllers.PackagesController>();

            var result = controller.Post(null);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
    }
}
