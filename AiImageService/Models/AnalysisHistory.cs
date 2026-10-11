using System;
using System.ComponentModel.DataAnnotations;

namespace AiImageService.Models
{
    public class AnalysisHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string S3Url { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string PredictedLabel { get; set; } = string.Empty;

        public float Confidence { get; set; }

        public string Source { get; set; } = "Web"; // "Web" або "TelegramBot"
    }
}