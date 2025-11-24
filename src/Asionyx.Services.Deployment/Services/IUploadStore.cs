using Microsoft.AspNetCore.Http;
namespace Asionyx.Services.Deployment.Services
{
    public interface IUploadStore
    {
        Task<string> StoreAsync(IFormFile file, string destDir);
    }
}
