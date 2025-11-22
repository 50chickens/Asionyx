using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class StatusControllerTests : TestBase
    {
        [Test]
        public void Get_Returns_Ok_With_Status()
        {
            var controller = Get<Asionyx.Services.Deployment.Controllers.StatusController>();
            var result = controller.Get();
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }
    }
}
