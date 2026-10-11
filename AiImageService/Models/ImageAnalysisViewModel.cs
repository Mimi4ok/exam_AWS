using Microsoft.AspNetCore.Http;

namespace AiImageService.Models
{
    public class ImageAnalysisViewModel
    {
        // Властивість ImageFile для сумісності з вашим контролером
        public IFormFile? ImageFile { get; set; }

        // Псевдонім File (для представлення Index.cshtml)
        public IFormFile? File
        {
            get => ImageFile;
            set => ImageFile = value;
        }

        // URL-посилання на зображення (якщо завантажують за посиланням)
        public string? ImageUrl { get; set; }

        // Результати аналізу
        public string? PredictedLabel { get; set; }
        public float Confidence { get; set; }

        // Посилання на збережений файл в AWS S3
        public string? S3Url { get; set; }
    }
}