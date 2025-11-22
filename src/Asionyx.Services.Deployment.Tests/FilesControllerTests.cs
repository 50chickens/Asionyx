using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class FilesControllerTests : TestBase
    {
        [Test]
        public void List_Returns_NotFound_For_Missing_Path()
        {
            var controller = Get<Asionyx.Services.Deployment.Controllers.FilesController>();
            var result = controller.List("/this/path/should/not/exist");
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public void Post_Write_And_Delete_File()
        {
            var controller = Get<Asionyx.Services.Deployment.Controllers.FilesController>();

            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
            try
            {
                var writeReq = new Asionyx.Services.Deployment.Controllers.FilesController.FileRequest("write", tempFile, "hello");
                var writeResult = controller.Post(writeReq);
                Assert.That(writeResult, Is.InstanceOf<OkObjectResult>());

                Assert.That(File.Exists(tempFile), Is.True);

                var delReq = new Asionyx.Services.Deployment.Controllers.FilesController.FileRequest("delete", tempFile, null);
                var delResult = controller.Post(delReq);
                Assert.That(delResult, Is.InstanceOf<OkObjectResult>());
                Assert.That(File.Exists(tempFile), Is.False);
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }
    }
}
