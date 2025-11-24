namespace Asionyx.Services.Deployment.Controllers
{
    public class PackageUploadResultDto
    {
        public string Package { get; set; } = string.Empty;
        public string Manifest { get; set; } = string.Empty;
    }

    public class ExtractionErrorDto
    {
        public string Error { get; set; } = string.Empty;
        public string? Detail { get; set; }
    }
}
