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
    public IActionResult Post()
    {
        try
        {
            if (!Request.HasFormContentType || Request.Form?.Files == null || Request.Form.Files.Count == 0)
                return BadRequest(new { error = "No file uploaded" });

            var file = Request.Form.Files[0];
            var fileName = Path.GetFileName(file.FileName ?? "upload.nupkg");
            if (!fileName.EndsWith(".nupkg", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Expected a .nupkg file" });

            var stagingRoot = "/var/asionyx_uploads";
            try { if (!Directory.Exists(stagingRoot)) Directory.CreateDirectory(stagingRoot); } catch { }

            var destPath = Path.Combine(stagingRoot, Guid.NewGuid().ToString("N") + "-" + fileName);
            using (var fs = System.IO.File.Create(destPath))
            {
                file.CopyTo(fs);
            }

            // Unzip into a subdirectory
            var extractDir = destPath + ".d";
            try { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true); } catch { }
            Directory.CreateDirectory(extractDir);
            try
            {
                ZipFile.ExtractToDirectory(destPath, extractDir);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to extract package", detail = ex.Message });
            }

            // Look for manifest.json anywhere inside extracted content
            var manifest = Directory.EnumerateFiles(extractDir, "manifest.json", SearchOption.AllDirectories).FirstOrDefault();
            if (manifest == null)
                return NotFound(new { error = "manifest.json not found in package" });

            var manifestContent = System.IO.File.ReadAllText(manifest);
            return Ok(new { package = fileName, manifest = manifestContent });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.ToString() });
        }
    }
}
