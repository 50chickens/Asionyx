using System.Diagnostics;
using Asionyx.Library.Shared.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asionyx.Services.Deployment.Controllers;

public class SystemdRequest
{
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

[ApiController]
[Route("[controller]")]
[Authorize]
public class SystemdController : ControllerBase
{
    private readonly IAppDiagnostics? _diag;
    private readonly string _execPath;

    public SystemdController(IAppDiagnostics? diag = null, Microsoft.Extensions.Options.IOptions<Asionyx.Services.Deployment.Configuration.DeploymentOptions>? options = null)
    {
        _diag = diag;
        // Prefer configured path, then environment override, then the default from DeploymentOptions
        var defaultOpts = new Asionyx.Services.Deployment.Configuration.DeploymentOptions();
        _execPath = options?.Value?.SystemdExecPath ?? Environment.GetEnvironmentVariable("ASIONYX_SYSTEMD_EXEC") ?? defaultOpts.SystemdExecPath;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SystemdRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Action) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new ErrorDto { Error = "Missing action or name" });

        var cmd = $"{req.Action} {req.Name}";
        try
        {
            var exec = _execPath;
            if (!System.IO.File.Exists(exec))
            {
                var err = $"SystemD executable not found at {exec}";
                try { if (_diag != null) await _diag.WriteAsync("systemd_exec_missing", new { Timestamp = DateTime.UtcNow, Error = err, PathTried = exec }); } catch { }
                return StatusCode(500, new ErrorDto { Error = err });
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
                try { if (_diag != null) await _diag.WriteAsync("systemd_start_failed", new { Timestamp = DateTime.UtcNow, Error = err, Command = cmd }); } catch { }
                return StatusCode(500, new ErrorDto { Error = err });
            }

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdoutTask, stderrTask);
            proc.WaitForExit(5000);

            var outStr = stdoutTask.Result.Trim();
            var errStr = stderrTask.Result.Trim();
            if (proc.ExitCode != 0)
            {
                try { if (_diag != null) await _diag.WriteAsync("systemd_cli_exit_nonzero", new { Timestamp = DateTime.UtcNow, Command = cmd, ExitCode = proc.ExitCode, Stdout = outStr, Stderr = errStr }); } catch { }
            }
            return Ok(new SystemdCommandResultDto { Command = cmd, Stdout = outStr, Stderr = errStr, ExitCode = proc.ExitCode });
        }
        catch (Exception ex)
        {
            try { if (_diag != null) await _diag.WriteAsync("systemd_cli_exception", new { Timestamp = DateTime.UtcNow, Exception = ex.ToString(), Command = cmd }); } catch { }
            return StatusCode(500, new ErrorDto { Error = ex.Message });
        }
    }
}
