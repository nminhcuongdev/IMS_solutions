using Microsoft.EntityFrameworkCore;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Hosting;

internal static class DatabaseStartupExtensions
{
    public static async Task MigrateAndSeedIfEnabledAsync(this WebApplication app)
    {
        var autoMigrate = app.Configuration.GetValue<bool?>("Database:AutoMigrate") ?? app.Environment.IsDevelopment();
        if (!autoMigrate) return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ImsDbContext>();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        try
        {
            await db.Database.MigrateAsync();
            await SeedData.RunAsync(db);
            log.LogInformation("Database migrate + seed hoàn tất.");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Migrate/seed thất bại. Kiểm tra quyền truy cập SQL của identity đang chạy app.");
        }
    }
}
