namespace Asionyx.Services.Deployment.Controllers
{
    public class PackagesListDto
    {
        public string Packages { get; set; } = string.Empty;
    }

    public class PackageActionResultDto
    {
        public string Result { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
    }
}
