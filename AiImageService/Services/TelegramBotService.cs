using AiImageService.Data;
using AiImageService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AiImageService.Services
{
    public class TelegramBotService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ImageClassificationService _mlService;

        public TelegramBotService(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ImageClassificationService _mlService)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            this._mlService = _mlService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var token = _configuration["TelegramBot:Token"];
            if (string.IsNullOrEmpty(token) || token == "YOUR_TELEGRAM_BOT_TOKEN")
                return;

            var botClient = new TelegramBotClient(token);

            // Очищаємо всі старі накопичені повідомлення (Drop Pending Updates)
            try
            {
                await botClient.DeleteWebhookAsync(dropPendingUpdates: true, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error dropping pending updates: {ex.Message}");
            }

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };

            botClient.StartReceiving(
                UpdateHandler,
                ErrorHandler,
                receiverOptions,
                cancellationToken: stoppingToken
            );

            await Task.Delay(-1, stoppingToken);
        }

        private async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Message is not { } message) return;

            using var scope = _scopeFactory.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            byte[]? imageBytes = null;
            string fileName = $"tg_{Guid.NewGuid()}.jpg";

            // 1. Обробка завантаженого зображення
            if (message.Photo != null && message.Photo.Length > 0)
            {
                var photo = message.Photo[^1];
                var fileInfo = await bot.GetFileAsync(photo.FileId, ct);

                using var stream = new MemoryStream();
                await bot.DownloadFileAsync(fileInfo.FilePath!, stream, ct);
                imageBytes = stream.ToArray();
            }
            // 2. Обробка посилання на зображення
            else if (!string.IsNullOrEmpty(message.Text) && Uri.IsWellFormedUriString(message.Text, UriKind.Absolute))
            {
                using var httpClient = new HttpClient();
                try
                {
                    imageBytes = await httpClient.GetByteArrayAsync(message.Text, ct);
                }
                catch
                {
                    await bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Не вдалося завантажити зображення за посиланням.",
                        cancellationToken: ct);
                    return;
                }
            }

            if (imageBytes != null)
            {
                await bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Аналізую зображення...",
                    cancellationToken: ct);

                using var uploadStream = new MemoryStream(imageBytes);
                var s3Url = await storageService.UploadFileAsync(uploadStream, fileName, "image/jpeg");

                var (label, confidence) = _mlService.ClassifyImage(imageBytes);

                var history = new AnalysisHistory
                {
                    FileName = fileName,
                    S3Url = s3Url,
                    PredictedLabel = label,
                    Confidence = confidence,
                    Source = "TelegramBot",
                    UploadedAt = DateTime.UtcNow
                };

                dbContext.AnalysisHistories.Add(history);
                await dbContext.SaveChangesAsync(ct);

                string response = $"*Результат аналізу:*\n" +
                                 $"Класифікація: *{label}*\n" +
                                 $"Точність (Confidence): *{confidence * 100:F2}%*\n" +
                                 $"S3 URL: [Переглянути]({s3Url})";

                await bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: response,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);
            }
        }

        private Task ErrorHandler(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Telegram Bot Error: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}