using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class PackageControllerTests
    {
        private TestServiceFactory _factory = new TestServiceFactory();

        [Test]
        public void Post_No_File_Returns_BadRequest()
        {
            var provider = _factory.BuildDefaultProvider();
            var controller = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Asionyx.Services.Deployment.Controllers.PackageController>(provider);

            // ensure Request.HasFormContentType is false by leaving ContentType null
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var result = controller.Post();
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
    }
}
