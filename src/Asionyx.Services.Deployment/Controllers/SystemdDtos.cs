namespace Asionyx.Services.Deployment.Controllers
{
    public class SystemdCommandResultDto
    {
        public string Command { get; set; } = string.Empty;
        public string Stdout { get; set; } = string.Empty;
        public string Stderr { get; set; } = string.Empty;
        public int ExitCode { get; set; }
    }
}
