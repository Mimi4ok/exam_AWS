using AiImageService.Data;
using AiImageService.Models;
using AiImageService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AiImageService.Controllers
{
    public class ImageAnalysisController : Controller
    {
        private readonly IStorageService _storageService;
        private readonly ImageClassificationService _mlService;
        private readonly ApplicationDbContext _dbContext;

        public ImageAnalysisController(
            IStorageService storageService,
            ImageClassificationService mlService,
            ApplicationDbContext dbContext)
        {
            _storageService = storageService;
            _mlService = mlService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ImageAnalysisViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(ImageAnalysisViewModel model)
        {
            Stream? imageStream = null;
            string fileName = "image.jpg";
            string contentType = "image/jpeg";

            // 1. Завантаження через файл
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                imageStream = model.ImageFile.OpenReadStream();
                fileName = model.ImageFile.FileName;
                contentType = model.ImageFile.ContentType;
            }
            // 2. Завантаження за URL
            else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(model.ImageUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("ImageUrl", "Не вдалося завантажити зображення за вказаним URL.");
                        return View(model);
                    }

                    // Виправлено: response.Content.ReadAsByteArrayAsync()
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    imageStream = new MemoryStream(bytes);

                    var uri = new Uri(model.ImageUrl);
                    var pathFileName = Path.GetFileName(uri.AbsolutePath);
                    if (!string.IsNullOrEmpty(pathFileName))
                    {
                        fileName = pathFileName;
                    }

                    contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                }
                catch
                {
                    ModelState.AddModelError("ImageUrl", "Некоректний URL або помилка при завантаженні.");
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "Будь ласка, завантажте файл або вкажіть URL.");
                return View(model);
            }

            using (imageStream)
            {
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);

                // 3. Завантаження в AWS S3
                memoryStream.Position = 0;
                string s3Url = await _storageService.UploadFileAsync(memoryStream, fileName, contentType);

                // 4. ML.NET Класифікація (передаємо byte[] через .ToArray())
                byte[] imageBytes = memoryStream.ToArray();
                var prediction = _mlService.ClassifyImage(imageBytes);

                // 5. Збереження в AWS RDS (використовуємо prediction.Label замість PredictedLabel)
                var history = new AnalysisHistory
                {
                    S3Url = s3Url,
                    PredictedLabel = prediction.Label,
                    Confidence = prediction.Confidence,
                    UploadedAt = DateTime.UtcNow,
                    Source = "Web"
                };

                _dbContext.AnalysisHistories.Add(history);
                await _dbContext.SaveChangesAsync();

                // 6. Повернення результатів у View
                model.S3Url = s3Url;
                model.PredictedLabel = prediction.Label;
                model.Confidence = prediction.Confidence;
            }

            return View(model);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var historyList = await _dbContext.AnalysisHistories
                .OrderByDescending(h => h.UploadedAt)
                .ToListAsync();

            return View(historyList);
        }
    }
}