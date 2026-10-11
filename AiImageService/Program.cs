using AiImageService.Data;
using AiImageService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Додавання DbContext для AWS RDS
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)
    ));
// Реєстрація сервісів
builder.Services.AddScoped<IStorageService, S3StorageService>();
builder.Services.AddSingleton<ImageClassificationService>();

// Реєстрація Telegram Bot як BackgroundService
builder.Services.AddHostedService<TelegramBotService>();

builder.Services.AddControllersWithViews();

// Реєструємо сервіс перекладу міток
builder.Services.AddSingleton<LabelTranslationService>();

// Реєструємо сервіс ML.NET класифікації
builder.Services.AddSingleton<ImageClassificationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ImageAnalysis}/{action=Index}/{id?}");

app.Run();