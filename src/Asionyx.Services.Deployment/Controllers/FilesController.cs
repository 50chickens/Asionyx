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
        if (System.IO.File.Exists(target))
        {
            var content = System.IO.File.ReadAllText(target);
            return Ok(new { type = "file", path = target, content });
        }
        else if (System.IO.Directory.Exists(target))
        {
            var entries = System.IO.Directory.GetFileSystemEntries(target);
            return Ok(new { type = "directory", path = target, entries });
        }
        else
        {
            return NotFound(new { error = "Path not found", path = target });
        }
    }

    [HttpPost]
    public IActionResult Post([FromBody] FileRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Action) || string.IsNullOrWhiteSpace(req.Path))
            return BadRequest(new { error = "Invalid request. Provide Action and Path." });

        var action = req.Action.ToLowerInvariant();
        try
        {
            if (action == "write")
            {
                System.IO.File.WriteAllText(req.Path, req.Content ?? string.Empty);
                return Ok(new { result = "written", path = req.Path });
            }
            else if (action == "delete")
            {
                if (System.IO.File.Exists(req.Path))
                {
                    System.IO.File.Delete(req.Path);
                    return Ok(new { result = "deleted", path = req.Path });
                }
                return NotFound(new { error = "File not found", path = req.Path });
            }
            else if (action == "list")
            {
                if (System.IO.Directory.Exists(req.Path))
                {
                    var entries = System.IO.Directory.GetFileSystemEntries(req.Path);
                    return Ok(new { result = "listed", path = req.Path, entries });
                }
                return NotFound(new { error = "Directory not found", path = req.Path });
            }

            return BadRequest(new { error = "Unknown action. Supported: write, delete, list" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.ToString() });
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
