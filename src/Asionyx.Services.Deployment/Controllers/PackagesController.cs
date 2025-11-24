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
    private readonly Asionyx.Services.Deployment.Services.IProcessRunner _runner;

    public PackagesController(Asionyx.Services.Deployment.Services.IProcessRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Return installed packages via dpkg -l
        var (exit, output, error) = await _runner.RunAsync("dpkg", "-l");
        if (exit != 0)
            return StatusCode(500, new ErrorDto { Error = error, Detail = output });

        return Ok(new PackagesListDto { Packages = output });
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] PackageRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Action) || req.Packages == null || req.Packages.Length == 0)
            return BadRequest(new ErrorDto { Error = "Invalid request. Provide Action and Packages." });

        var action = req.Action.ToLowerInvariant();
        if (action == "install")
        {
            // Ensure apt lists are up to date first
            var upd = await _runner.RunAsync("apt-get", "update -y");
            // Install requested packages
            var pkgArgs = "install -y " + string.Join(' ', req.Packages);
            var (exit, output, error) = await _runner.RunAsync("apt-get", pkgArgs);
            if (exit != 0)
                return StatusCode(500, new ErrorDto { Error = error, Detail = output });

            return Ok(new PackageActionResultDto { Result = "installed", Output = output });
        }
        else if (action == "remove" || action == "uninstall")
        {
            var pkgArgs = "remove -y " + string.Join(' ', req.Packages);
            var (exit, output, error) = await _runner.RunAsync("apt-get", pkgArgs);
            if (exit != 0)
                return StatusCode(500, new ErrorDto { Error = error, Detail = output });

            return Ok(new PackageActionResultDto { Result = "removed", Output = output });
        }

        return BadRequest(new ErrorDto { Error = "Unknown action. Supported: install, remove" });
    }
}
