// Services/IStorageService.cs
using System.IO;
using System.Threading.Tasks;

namespace AiImageService.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    }
}