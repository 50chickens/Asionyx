using Microsoft.AspNetCore.Http;

namespace Asionyx.Services.Deployment.Services
{
    public class LocalUploadStore : IUploadStore
    {
        private readonly IFileSystem _fs;

        public LocalUploadStore() : this(new LocalFileSystem()) { }
        public LocalUploadStore(IFileSystem fs) { _fs = fs; }

        public async Task<string> StoreAsync(IFormFile file, string destDir)
        {
            if (!_fs.DirectoryExists(destDir)) _fs.CreateDirectory(destDir);
            var fileName = Path.GetFileName(file.FileName ?? "upload.bin");
            var destPath = Path.Combine(destDir, Guid.NewGuid().ToString("N") + "-" + fileName);
            using (var fs = System.IO.File.Create(destPath))
            {
                await file.CopyToAsync(fs);
            }
            return destPath;
        }
    }
}
