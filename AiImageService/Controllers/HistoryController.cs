using AiImageService.Data;
using AiImageService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AiImageService.Controllers
{
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public HistoryController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(string? source, string? label)
        {
            var query = _dbContext.AnalysisHistories.AsQueryable();

            if (!string.IsNullOrEmpty(source))
            {
                query = query.Where(h => h.Source == source);
            }

            if (!string.IsNullOrEmpty(label))
            {
                query = query.Where(h => h.PredictedLabel.Contains(label));
            }

            var items = await query.OrderByDescending(h => h.UploadedAt).ToListAsync();

            var viewModel = new AnalysisHistoryViewModel
            {
                Items = items,
                SearchSource = source,
                SearchLabel = label
            };

            return View(viewModel);
        }
    }
}