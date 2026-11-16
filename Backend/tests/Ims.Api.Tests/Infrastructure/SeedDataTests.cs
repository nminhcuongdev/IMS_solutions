using Microsoft.EntityFrameworkCore;
using Ims.Infrastructure.Persistence;
using Xunit;

namespace Ims.Api.Tests.Infrastructure;

public class SeedDataTests
{
    [Fact]
    public async Task Seed_runs_on_an_empty_database_without_duplicate_labels()
    {
        using var t = TestDb.Empty();

        await SeedData.RunAsync(t.Db);

        var duplicates = await t.Db.LanguageData
            .GroupBy(x => new { x.LabelKey, x.LanguageCode, x.DeviceScope })
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.LabelKey)
            .ToListAsync();
        Assert.Empty(duplicates);
        Assert.True(await t.Db.AppUsers.AnyAsync(x => x.UserCode == "admin"));
    }
}
