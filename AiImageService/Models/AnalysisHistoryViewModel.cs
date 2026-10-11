using System.Collections.Generic;

namespace AiImageService.Models
{
    public class AnalysisHistoryViewModel
    {
        public List<AnalysisHistory> Items { get; set; } = new();
        public string? SearchSource { get; set; }
        public string? SearchLabel { get; set; }
    }
}