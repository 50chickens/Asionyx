using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asionyx.Services.Deployment.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var isRoot = IsRunningAsRoot();
        var info = new
        {
            status = "ok",
            isRoot = isRoot,
            os = RuntimeInformation.OSDescription
        };
        return Ok(info);
    }

    private static bool IsRunningAsRoot()
    {
        if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        try
        {
            // on Linux/macOS avail via libc
            return geteuid() == 0u;
        }
        catch
        {
            return false;
        }
    }

    [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint geteuid();
}
