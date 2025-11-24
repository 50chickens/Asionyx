using System.IO.Compression;

namespace Asionyx.Services.Deployment.Services
{
    public class ZipArchiveExtractor : IArchiveExtractor
    {
        public void ExtractToDirectory(string archivePath, string extractDir)
        {
            ZipFile.ExtractToDirectory(archivePath, extractDir);
        }
    }
}
