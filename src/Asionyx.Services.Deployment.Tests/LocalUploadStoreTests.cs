using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Asionyx.Services.Deployment.Services;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class LocalUploadStoreTests
    {
        [Test]
        public async Task StoreAsync_SavesFileAndReturnsPath()
        {
            var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                var content = "hello world";
                var bytes = Encoding.UTF8.GetBytes(content);
                using var ms = new MemoryStream(bytes);
                ms.Position = 0;
                IFormFile file = new FormFile(ms, 0, bytes.Length, "file", "test.txt");

                var store = new LocalUploadStore();
                var path = await store.StoreAsync(file, temp);

                Assert.That(File.Exists(path), Is.True);
                var read = File.ReadAllText(path);
                Assert.That(read, Is.EqualTo(content));
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }
    }
}
