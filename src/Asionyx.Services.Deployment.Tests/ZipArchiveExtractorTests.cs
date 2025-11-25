using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using Asionyx.Services.Deployment.Services;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class ZipArchiveExtractorTests
    {
        [Test]
        public void ExtractToDirectory_ExtractsFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var zipPath = Path.Combine(tempDir, "archive.zip");
            var extractDir = Path.Combine(tempDir, "out");
            Directory.CreateDirectory(extractDir);
            try
            {
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("a.txt");
                    using var s = entry.Open();
                    using var w = new StreamWriter(s);
                    w.Write("contentA");
                }

                var extractor = new ZipArchiveExtractor();
                extractor.ExtractToDirectory(zipPath, extractDir);

                var file = Path.Combine(extractDir, "a.txt");
                Assert.That(File.Exists(file), Is.True);
                Assert.That(File.ReadAllText(file), Is.EqualTo("contentA"));
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
