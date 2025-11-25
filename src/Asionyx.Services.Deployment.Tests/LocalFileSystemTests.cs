using System;
using System.IO;
using NUnit.Framework;
using Asionyx.Services.Deployment.Services;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class LocalFileSystemTests
    {
        [Test]
        public void FileAndDirectoryOperations_WorkAsExpected()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                var fs = new LocalFileSystem();
                Assert.That(fs.DirectoryExists(root), Is.False);
                fs.CreateDirectory(root);
                Assert.That(fs.DirectoryExists(root), Is.True);

                var file = Path.Combine(root, "a.txt");
                fs.WriteAllText(file, "x");
                Assert.That(fs.FileExists(file), Is.True);
                Assert.That(fs.ReadAllText(file), Is.EqualTo("x"));

                var entries = fs.GetFileSystemEntries(root);
                Assert.That(entries, Has.Member(file));

                fs.DeleteFile(file);
                Assert.That(fs.FileExists(file), Is.False);

                fs.DeleteDirectory(root, false);
                Assert.That(fs.DirectoryExists(root), Is.False);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }
    }
}
