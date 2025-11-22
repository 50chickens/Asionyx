using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class InfoControllerTests : TestBase
    {
        [Test]
        public void Get_Returns_Ok()
        {
            var controller = Get<Asionyx.Services.Deployment.Controllers.InfoController>();

            var result = controller.Get();
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }
    }
}
