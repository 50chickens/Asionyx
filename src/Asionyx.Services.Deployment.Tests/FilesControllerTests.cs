using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Asionyx.Services.Deployment.Controllers;
using Asionyx.Services.Deployment.Services;

namespace Asionyx.Services.Deployment.Tests
{
    public class InMemoryFileSystem : IFileSystem
    {
        public readonly System.Collections.Generic.Dictionary<string, string> Files = new();
        public readonly System.Collections.Generic.Dictionary<string, string[]> Dirs = new();

        public bool FileExists(string path) => Files.ContainsKey(path);
        public string ReadAllText(string path) => Files[path];
        public void WriteAllText(string path, string content) => Files[path] = content;
        public void DeleteFile(string path) => Files.Remove(path);
        public bool DirectoryExists(string path) => Dirs.ContainsKey(path);
        public string[] GetFileSystemEntries(string path) => Dirs[path];
        public void CreateDirectory(string path) => Dirs[path] = Array.Empty<string>();
        public void DeleteDirectory(string path, bool recursive) => Dirs.Remove(path);
    }

    public class SimpleServiceProvider : IServiceProvider
    {
        private readonly IFileSystem _fs;
        public SimpleServiceProvider(IFileSystem fs) => _fs = fs;
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IFileSystem)) return _fs;
            return null;
        }
    }

    [TestFixture]
    public class FilesControllerTests
    {
        [Test]
        public void List_File_ReturnsFileResult()
        {
            var fs = new InMemoryFileSystem();
            fs.WriteAllText("/", "hello");

            var controller = new FilesController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = new SimpleServiceProvider(fs) } };

            var result = controller.List(null) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var dto = result!.Value as FileResultDto;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Type, Is.EqualTo("file"));
            Assert.That(dto.Path, Is.EqualTo("/"));
            Assert.That(dto.Content, Is.EqualTo("hello"));
        }

        [Test]
        public void List_Directory_ReturnsDirectoryResult()
        {
            var fs = new InMemoryFileSystem();
            fs.Dirs["/mydir"] = new[] { "a.txt", "b.txt" };

            var controller = new FilesController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = new SimpleServiceProvider(fs) } };

            var result = controller.List("/mydir") as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var dto = result!.Value as FileResultDto;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Type, Is.EqualTo("directory"));
            Assert.That(dto.Path, Is.EqualTo("/mydir"));
            Assert.That(dto.Entries, Is.EqualTo(new[] { "a.txt", "b.txt" }));
        }

        [Test]
        public void List_NotFound_ReturnsNotFound()
        {
            var fs = new InMemoryFileSystem();
            var controller = new FilesController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = new SimpleServiceProvider(fs) } };

            var result = controller.List("/nope") as ObjectResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public void Post_Write_WritesFile()
        {
            var fs = new InMemoryFileSystem();
            var controller = new FilesController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = new SimpleServiceProvider(fs) } };

            var req = new FilesController.FileRequest("write", "/a", "data");
            var result = controller.Post(req) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var dto = result!.Value as ActionResultDto;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Result, Is.EqualTo("written"));
            Assert.That(fs.FileExists("/a"), Is.True);
            Assert.That(fs.ReadAllText("/a"), Is.EqualTo("data"));
        }

        [Test]
        public void Post_Delete_FileExists_Deletes()
        {
            var fs = new InMemoryFileSystem();
            fs.WriteAllText("/f", "x");
            var controller = new FilesController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = new SimpleServiceProvider(fs) } };

            var req = new FilesController.FileRequest("delete", "/f", null);
            var result = controller.Post(req) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var dto = result!.Value as ActionResultDto;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Result, Is.EqualTo("deleted"));
            Assert.That(fs.FileExists("/f"), Is.False);
        }

        [Test]
        public void Post_Delete_NotFound_ReturnsNotFound()
        {
            var fs = new InMemoryFileSystem();
            var controller = new FilesController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = new SimpleServiceProvider(fs) } };

            var req = new FilesController.FileRequest("delete", "/missing", null);
            var result = controller.Post(req) as ObjectResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public void Post_InvalidRequest_ReturnsBadRequest()
        {
            var controller = new FilesController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = new SimpleServiceProvider(new InMemoryFileSystem()) } };

            var result = controller.Post(null) as ObjectResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.StatusCode, Is.EqualTo(400));
        }
    }
}
