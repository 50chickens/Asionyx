using System.Diagnostics;
using Asionyx.Library.Shared.Diagnostics;
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
    private readonly IAppDiagnostics? _diag;

    public SystemdController(IAppDiagnostics? diag = null)
    {
        _diag = diag;
    }
    // Prefer the in-container installed self-contained executable in /usr/local/bin
    private static readonly string InContainerExec = "/usr/local/bin/Asionyx.Services.Deployment.SystemD";

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SystemdRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Action) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Missing action or name");

        var cmd = $"{req.Action} {req.Name}";
        try
        {
            var exec = InContainerExec;
            if (!System.IO.File.Exists(exec))
            {
                var err = "SystemD executable not found at /usr/local/bin/Asionyx.Services.Deployment.SystemD";
                try { _diag?.WriteAsync("systemd_exec_missing", new { Timestamp = DateTime.UtcNow, Error = err, PathTried = exec }).GetAwaiter().GetResult(); } catch { }
                return StatusCode(500, new { error = err });
            }

            var psi = new ProcessStartInfo(exec, cmd)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                var err = "Failed to start systemd CLI";
                try { _diag?.WriteAsync("systemd_start_failed", new { Timestamp = DateTime.UtcNow, Error = err, Command = cmd }).GetAwaiter().GetResult(); } catch { }
                return StatusCode(500, new { error = err });
            }

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdoutTask, stderrTask);
            proc.WaitForExit(5000);

            var outStr = stdoutTask.Result.Trim();
            var errStr = stderrTask.Result.Trim();
            if (proc.ExitCode != 0)
            {
                try { _diag?.WriteAsync("systemd_cli_exit_nonzero", new { Timestamp = DateTime.UtcNow, Command = cmd, ExitCode = proc.ExitCode, Stdout = outStr, Stderr = errStr }).GetAwaiter().GetResult(); } catch { }
            }
            return Ok(new { command = cmd, stdout = outStr, stderr = errStr, exitCode = proc.ExitCode });
        }
        catch (Exception ex)
        {
            try { _diag?.WriteAsync("systemd_cli_exception", new { Timestamp = DateTime.UtcNow, Exception = ex.ToString(), Command = cmd }).GetAwaiter().GetResult(); } catch { }
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
