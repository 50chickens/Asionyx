using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asionyx.Services.Deployment.Controllers;

[ApiController]
[Route("filesystem/files")]
[Authorize]
public class FilesController : ControllerBase
{
    public record FileRequest(string Action, string Path, string? Content);

    [HttpGet]
    public IActionResult List([FromQuery] string? path)
    {
        var target = string.IsNullOrWhiteSpace(path) ? "/" : path;
        var fs = HttpContext?.RequestServices.GetService(typeof(Asionyx.Services.Deployment.Services.IFileSystem)) as Asionyx.Services.Deployment.Services.IFileSystem ?? new Asionyx.Services.Deployment.Services.LocalFileSystem();
        if (fs.FileExists(target))
        {
            var content = fs.ReadAllText(target);
            var dto = new FileResultDto { Type = "file", Path = target, Content = content };
            return new OkObjectResult(dto);
        }
        else if (fs.DirectoryExists(target))
        {
            var entries = fs.GetFileSystemEntries(target);
            var dto = new FileResultDto { Type = "directory", Path = target, Entries = entries };
            return new OkObjectResult(dto);
        }
        else
        {
            return NotFound(new ErrorDto { Error = "Path not found", Path = target });
        }
    }

    [HttpPost]
    public IActionResult Post([FromBody] FileRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Action) || string.IsNullOrWhiteSpace(req.Path))
            return BadRequest(new ErrorDto { Error = "Invalid request. Provide Action and Path." });

        var action = req.Action.ToLowerInvariant();
            try
            {
                var fs = HttpContext?.RequestServices.GetService(typeof(Asionyx.Services.Deployment.Services.IFileSystem)) as Asionyx.Services.Deployment.Services.IFileSystem ?? new Asionyx.Services.Deployment.Services.LocalFileSystem();
                if (action == "write")
                {
                    fs.WriteAllText(req.Path, req.Content ?? string.Empty);
                    return new OkObjectResult(new ActionResultDto { Result = "written", Path = req.Path });
                }
                else if (action == "delete")
                {
                    if (fs.FileExists(req.Path))
                    {
                        fs.DeleteFile(req.Path);
                        return new OkObjectResult(new ActionResultDto { Result = "deleted", Path = req.Path });
                    }
                    return NotFound(new ErrorDto { Error = "File not found", Path = req.Path });
                }
                else if (action == "list")
                {
                    if (fs.DirectoryExists(req.Path))
                    {
                        var entries = fs.GetFileSystemEntries(req.Path);
                        return new OkObjectResult(new ActionResultDto { Result = "listed", Path = req.Path, Entries = entries });
                    }
                    return NotFound(new ErrorDto { Error = "Directory not found", Path = req.Path });
                }

                return BadRequest(new ErrorDto { Error = "Unknown action. Supported: write, delete, list" });
            }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorDto { Error = "Internal error", Detail = ex.Message });
        }
    }

    // keep this helper in case we want to shell out in future
    private static (int ExitCode, string StdOut, string StdErr) RunProcess(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var p = Process.Start(psi);
            if (p == null) return (-1, string.Empty, "failed to start process");
            var outt = p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode, outt, err);
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.ToString());
        }
    }
}
