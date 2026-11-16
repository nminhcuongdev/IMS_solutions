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
            log.LogInformation("Database migrate and seed completed.");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Migration/seed failed. Check the SQL permissions of the identity that runs the app.");
        }
    }
}
