using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Asionyx.Services.Deployment.Controllers;

public class SystemdRequest
{
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

[ApiController]
[Route("[controller]")]
public class SystemdController : ControllerBase
{
    // Prefer the in-container installed CLI path; fall back to repo-relative CLI for local dev
    private static readonly string InContainerCli = "/app/systemd/Asionyx.Services.Deployment.SystemD";

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SystemdRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Action) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Missing action or name");

        var cmd = $"{req.Action} {req.Name}";
        try
        {
            var cli = InContainerCli;
            if (!System.IO.File.Exists(cli))
            {
                // fallback for local development
                cli = Path.Combine(Directory.GetCurrentDirectory(), "..", "Asionyx.Services.Deployment.SystemD", "Asionyx.Services.Deployment.SystemD");
            }

            if (!System.IO.File.Exists(cli))
                return StatusCode(500, new { error = "SystemD CLI not found in-container or at fallback path" });

            var psi = new ProcessStartInfo(cli, cmd)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return StatusCode(500, new { error = "Failed to start systemd CLI" });

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdoutTask, stderrTask);
            proc.WaitForExit(5000);

            return Ok(new { command = cmd, stdout = stdoutTask.Result.Trim(), stderr = stderrTask.Result.Trim(), exitCode = proc.ExitCode });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
