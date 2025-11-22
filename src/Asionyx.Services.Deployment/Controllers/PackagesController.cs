using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asionyx.Services.Deployment.Controllers;

[ApiController]
[Route("[controller]")]
    [Authorize]
public class PackagesController : ControllerBase
{
    public record PackageRequest(string Action, string[] Packages);

    [HttpGet]
    public IActionResult Get()
    {
        // Return installed packages via dpkg -l
        var (exit, output, error) = RunProcess("dpkg", "-l");
        if (exit != 0)
            return StatusCode(500, new { error = error, output = output });

        return Ok(new { packages = output });
    }

    [HttpPost]
    public IActionResult Post([FromBody] PackageRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Action) || req.Packages == null || req.Packages.Length == 0)
            return BadRequest(new { error = "Invalid request. Provide Action and Packages." });

        var action = req.Action.ToLowerInvariant();
        if (action == "install")
        {
            // Ensure apt lists are up to date first
            var upd = RunProcess("apt-get", "update -y");
            // Install requested packages
            var pkgArgs = "install -y " + string.Join(' ', req.Packages);
            var (exit, output, error) = RunProcess("apt-get", pkgArgs);
            if (exit != 0)
                return StatusCode(500, new { error = error, output = output });

            return Ok(new { result = "installed", output = output });
        }
        else if (action == "remove" || action == "uninstall")
        {
            var pkgArgs = "remove -y " + string.Join(' ', req.Packages);
            var (exit, output, error) = RunProcess("apt-get", pkgArgs);
            if (exit != 0)
                return StatusCode(500, new { error = error, output = output });

            return Ok(new { result = "removed", output = output });
        }

        return BadRequest(new { error = "Unknown action. Supported: install, remove" });
    }

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
