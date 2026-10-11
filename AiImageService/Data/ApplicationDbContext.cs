using AiImageService.Models;
using Microsoft.EntityFrameworkCore;

namespace AiImageService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<AnalysisHistory> AnalysisHistories { get; set; } = null!;
    }
}