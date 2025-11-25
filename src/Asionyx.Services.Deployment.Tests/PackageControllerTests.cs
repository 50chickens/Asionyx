using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Asionyx.Services.Deployment.Controllers;
using Asionyx.Services.Deployment.Services;
using Asionyx.Services.Deployment.Configuration;

namespace Asionyx.Services.Deployment.Tests
{
    class StubUploadStore : IUploadStore
    {
        private readonly string _dest;
        public StubUploadStore(string dest) { _dest = dest; }
        public async Task<string> StoreAsync(IFormFile file, string destDir)
        {
            var dest = Path.Combine(destDir, Guid.NewGuid().ToString("N") + "-" + (file.FileName ?? "upload.bin"));
            using var fs = System.IO.File.Create(dest);
            await file.CopyToAsync(fs);
            return dest;
        }
    }

    class StubExtractor : IArchiveExtractor
    {
        private readonly bool _createManifest;
        public StubExtractor(bool createManifest) { _createManifest = createManifest; }
        public void ExtractToDirectory(string archivePath, string extractDir)
        {
            if (_createManifest)
            {
                Directory.CreateDirectory(extractDir);
                var manifest = Path.Combine(extractDir, "manifest.json");
                File.WriteAllText(manifest, "{\"ok\":true}");
            }
            else
            {
                // do nothing -> manifest missing
            }
        }
    }

    class ThrowingExtractor : IArchiveExtractor
    {
        public void ExtractToDirectory(string archivePath, string extractDir)
        {
            throw new System.InvalidOperationException("simulated extraction failure");
        }
    }

    class SimpleProvider : IServiceProvider
    {
        private readonly IFileSystem _fs;
        private readonly IUploadStore _store;
        private readonly IArchiveExtractor _extractor;
        private readonly IOptions<DeploymentOptions> _opts;

        public SimpleProvider(IFileSystem fs, IUploadStore store, IArchiveExtractor extractor, IOptions<DeploymentOptions> opts)
        {
            _fs = fs; _store = store; _extractor = extractor; _opts = opts;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IFileSystem)) return _fs;
            if (serviceType == typeof(IUploadStore)) return _store;
            if (serviceType == typeof(IArchiveExtractor)) return _extractor;
            if (serviceType == typeof(Microsoft.Extensions.Options.IOptions<DeploymentOptions>)) return _opts;
            return null;
        }
    }

    [TestFixture]
    public class PackageControllerTests
    {
        [Test]
        public async Task Post_Success_ReturnsPackageUploadResult()
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                var fs = new LocalFileSystem();
                var store = new StubUploadStore(tmp);
                var extractor = new StubExtractor(true);
                var opts = Options.Create(new DeploymentOptions { UploadsDir = tmp });

                var controller = new PackageController();
                var context = new DefaultHttpContext();
                context.RequestServices = new SimpleProvider(fs, store, extractor, opts);

                // create a form file with .nupkg name
                var content = Encoding.UTF8.GetBytes("dummy");
                var ms = new MemoryStream(content);
                IFormFile file = new FormFile(ms, 0, content.Length, "file", "mypkg.nupkg");

                var form = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), new FormFileCollection { file });
                context.Request.Form = form;

                controller.ControllerContext = new ControllerContext { HttpContext = context };

                var result = await controller.Post();
                Assert.That(result, Is.TypeOf<OkObjectResult>());
                var ok = result as OkObjectResult;
                Assert.That(ok!.Value, Is.TypeOf<PackageUploadResultDto>());
                var dto = ok.Value as PackageUploadResultDto;
                Assert.That(dto!.Package, Is.EqualTo("mypkg.nupkg"));
                Assert.That(dto.Manifest, Is.EqualTo("{\"ok\":true}"));
            }
            finally { try { Directory.Delete(tmp, true); } catch { } }
        }

        [Test]
        public async Task Post_InvalidExtension_ReturnsBadRequest()
        {
            var controller = new PackageController();
            var context = new DefaultHttpContext();
            var fs = new LocalFileSystem();
            var store = new StubUploadStore(Path.GetTempPath());
            var extractor = new StubExtractor(true);
            context.RequestServices = new SimpleProvider(fs, store, extractor, Options.Create(new DeploymentOptions()));

            var content = Encoding.UTF8.GetBytes("dummy");
            var ms = new MemoryStream(content);
            IFormFile file = new FormFile(ms, 0, content.Length, "file", "bad.txt");
            var form = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), new FormFileCollection { file });
            context.Request.Form = form;

            controller.ControllerContext = new ControllerContext { HttpContext = context };
            var result = await controller.Post();
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var obj = result as ObjectResult;
            Assert.That(obj!.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task Post_NoFile_ReturnsBadRequest()
        {
            var controller = new PackageController();
            var context = new DefaultHttpContext();
            context.RequestServices = new SimpleProvider(new LocalFileSystem(), new StubUploadStore(Path.GetTempPath()), new StubExtractor(false), Options.Create(new DeploymentOptions()));
            // no files
            var form = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), new FormFileCollection());
            context.Request.Form = form;
            controller.ControllerContext = new ControllerContext { HttpContext = context };
            var result = await controller.Post();
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var obj = result as ObjectResult;
            Assert.That(obj!.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task Post_ExtractionException_Returns_500()
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                var fs = new LocalFileSystem();
                var store = new StubUploadStore(tmp);
                // extractor that throws
                var extractor = new StubExtractor(true);
                var opts = Options.Create(new DeploymentOptions { UploadsDir = tmp });

                var controller = new PackageController();
                var context = new DefaultHttpContext();
                context.RequestServices = new SimpleProvider(fs, store, new ThrowingExtractor(), opts);

                var content = Encoding.UTF8.GetBytes("dummy");
                var ms = new MemoryStream(content);
                IFormFile file = new FormFile(ms, 0, content.Length, "file", "mypkg.nupkg");
                var form = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), new FormFileCollection { file });
                context.Request.Form = form;
                controller.ControllerContext = new ControllerContext { HttpContext = context };

                var result = await controller.Post();
                Assert.That(result, Is.InstanceOf<ObjectResult>());
                var obj = result as ObjectResult;
                Assert.That(obj!.StatusCode, Is.EqualTo(500));
            }
            finally { try { Directory.Delete(tmp, true); } catch { } }
        }

        [Test]
        public async Task Post_ManifestMissing_Returns_NotFound()
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                var fs = new LocalFileSystem();
                var store = new StubUploadStore(tmp);
                var extractor = new StubExtractor(false); // does not create manifest
                var opts = Options.Create(new DeploymentOptions { UploadsDir = tmp });

                var controller = new PackageController();
                var context = new DefaultHttpContext();
                context.RequestServices = new SimpleProvider(fs, store, extractor, opts);

                var content = Encoding.UTF8.GetBytes("dummy");
                var ms = new MemoryStream(content);
                IFormFile file = new FormFile(ms, 0, content.Length, "file", "mypkg.nupkg");
                var form = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), new FormFileCollection { file });
                context.Request.Form = form;
                controller.ControllerContext = new ControllerContext { HttpContext = context };

                var result = await controller.Post();
                Assert.That(result, Is.InstanceOf<ObjectResult>());
                var obj = result as ObjectResult;
                Assert.That(obj!.StatusCode, Is.EqualTo(404));
            }
            finally { try { Directory.Delete(tmp, true); } catch { } }
        }
    }
}
