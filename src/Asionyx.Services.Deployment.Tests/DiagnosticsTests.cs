using System;
using System.IO;
using System.Threading.Tasks;
using Asionyx.Library.Shared.Diagnostics;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class DiagnosticsTests
    {
        [Test]
        public async Task FileDiagnostics_WriteAndRead_Works()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "asionyx_tests", Guid.NewGuid().ToString("N"));
            try
            {
                var diag = new FileDiagnostics(tempDir);

                var obj = new { Time = DateTime.UtcNow, Message = "hello" };

                await diag.WriteAsync("test1", obj);

                var read = await diag.ReadAsync<dynamic>("test1");

                Assert.That(read, Is.Not.Null);
                Assert.That(read.Message.ToString(), Is.EqualTo("hello"));
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }
}
