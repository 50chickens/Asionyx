namespace Asionyx.Services.Deployment.Services
{
    public interface IArchiveExtractor
    {
        void ExtractToDirectory(string archivePath, string extractDir);
    }
}
