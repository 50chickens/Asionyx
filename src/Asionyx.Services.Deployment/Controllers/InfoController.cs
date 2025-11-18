using Microsoft.AspNetCore.Mvc;
using Asionyx.Library.Core;

namespace Asionyx.Services.Deployment.Controllers;

[ApiController]
[Route("[controller]")]
public class InfoController : ControllerBase
{
    private readonly ISystemConfigurator _configurator;
    private readonly IConfiguration _configuration;

    public InfoController(ISystemConfigurator configurator, IConfiguration configuration)
    {
        _configurator = configurator;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var info = new {
            Service = "Asionyx.Services.Deployment",
            Configurator = _configurator.GetInfo(),
            Env = _configuration["ASIONYX_ENV"] ?? "unknown"
        };
        return Ok(info);
    }
}
