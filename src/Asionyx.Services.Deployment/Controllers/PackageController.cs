using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asionyx.Services.Deployment.Controllers;

[ApiController]
[Route("package")]
[Authorize]
public class PackageController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(50_000_000)] // allow up to ~50MB uploads
    public async Task<IActionResult> Post()
    {
        try
        {
            if (!Request.HasFormContentType || Request.Form?.Files == null || Request.Form.Files.Count == 0)
                return BadRequest(new ErrorDto { Error = "No file uploaded" });

            var file = Request.Form.Files[0];
            var fileName = Path.GetFileName(file.FileName ?? "upload.nupkg");
            var fs = HttpContext.RequestServices.GetService(typeof(Asionyx.Services.Deployment.Services.IFileSystem)) as Asionyx.Services.Deployment.Services.IFileSystem ?? new Asionyx.Services.Deployment.Services.LocalFileSystem();
            var extractor = HttpContext.RequestServices.GetService(typeof(Asionyx.Services.Deployment.Services.IArchiveExtractor)) as Asionyx.Services.Deployment.Services.IArchiveExtractor ?? new Asionyx.Services.Deployment.Services.ZipArchiveExtractor();
            var store = HttpContext.RequestServices.GetService(typeof(Asionyx.Services.Deployment.Services.IUploadStore)) as Asionyx.Services.Deployment.Services.IUploadStore ?? new Asionyx.Services.Deployment.Services.LocalUploadStore(fs);
            if (!fileName.EndsWith(".nupkg", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new ErrorDto { Error = "Expected a .nupkg file" });

            var opts = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Options.IOptions<Asionyx.Services.Deployment.Configuration.DeploymentOptions>)) as Microsoft.Extensions.Options.IOptions<Asionyx.Services.Deployment.Configuration.DeploymentOptions>;
            var defaultOpts = new Asionyx.Services.Deployment.Configuration.DeploymentOptions();
            var stagingRoot = opts?.Value?.UploadsDir ?? Environment.GetEnvironmentVariable("ASIONYX_UPLOADS_DIR") ?? defaultOpts.UploadsDir;
            try { if (!fs.DirectoryExists(stagingRoot)) fs.CreateDirectory(stagingRoot); } catch { }

            var destPath = await store.StoreAsync(file, stagingRoot);

            // Unzip into a subdirectory
            var extractDir = destPath + ".d";
            try { if (fs.DirectoryExists(extractDir)) fs.DeleteDirectory(extractDir, true); } catch { }
            fs.CreateDirectory(extractDir);
            try
            {
                extractor.ExtractToDirectory(destPath, extractDir);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ExtractionErrorDto { Error = "Failed to extract package", Detail = ex.Message });
            }

            // Look for manifest.json anywhere inside extracted content
            var manifest = Directory.EnumerateFiles(extractDir, "manifest.json", SearchOption.AllDirectories).FirstOrDefault();
            if (manifest == null)
                return NotFound(new ErrorDto { Error = "manifest.json not found in package" });

            var manifestContent = System.IO.File.ReadAllText(manifest);
            return Ok(new PackageUploadResultDto { Package = fileName, Manifest = manifestContent });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorDto { Error = "Internal error", Detail = ex.Message });
        }
    }
}
