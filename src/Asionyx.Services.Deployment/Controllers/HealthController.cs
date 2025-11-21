using Microsoft.AspNetCore.Mvc;

namespace Asionyx.Services.Deployment.Controllers
{
    [ApiController]
    [Route("healthz")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "ok" });
    }
}
