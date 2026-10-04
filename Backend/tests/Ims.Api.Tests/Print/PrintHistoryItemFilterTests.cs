using Ims.Api.Contracts;
using Ims.Api.Features.History;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Print;

public class PrintHistoryItemFilterTests
{
    private static async Task SeedAsync(TestDb t)
    {
        t.Db.Items.Add(new Item
        {
            ItemCode = "PT-RBN-05", ItemName = "Ribbon wax 110mm", ItemGroupCode = "IG-01",
            UomCode = "pcs", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        foreach (var (doc, item, spec) in new[]
                 {
                     ("PO-1", "PT-RBN-05", "IND4"),
                     ("PO-2", "LB-TH-02", "IND4"),
                     ("PO-3", "LB-TH-03", "RBN-PRINTER"),
                 })
        {
            t.Db.PrintHistories.Add(new Domain.History.PrintHistory
            {
                Category = "Receiving", DocNo = doc, ItemCode = item, Qty = 1, UomCode = "pcs",
                PackageCount = 1, Spec = spec, Status = 19, CreatedBy = "U001",
                CreatedAt = new DateTime(2026, 9, 14, 8, 0, 0),
            });
        }
        await t.Db.SaveChangesAsync();
    }

    private static async Task<string[]> DocsAsync(TestDb t, string item)
    {
        var res = await new HistoryService(t.Db, null!, new Ims.Api.Features.Print.PrintTargetResolver(t.Db)).ListPrintAsync(CancellationToken.None, item: item);
        return ResultAssert.Ok<ApiListSuccess<PrintHistoryDto>>(res).Data
            .Select(r => r.DocNo!).OrderBy(d => d).ToArray();
    }

    [Fact]
    public async Task Item_matches_part_of_the_item_code_and_nothing_else()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);

        Assert.Equal(new[] { "PO-1" }, await DocsAsync(t, "rbn"));
    }

    [Fact]
    public async Task Item_matches_part_of_the_item_name_from_the_master()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);

        Assert.Equal(new[] { "PO-1" }, await DocsAsync(t, "wax"));
    }

    [Fact]
    public async Task A_blank_item_filters_nothing()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);

        Assert.Equal(new[] { "PO-1", "PO-2", "PO-3" }, await DocsAsync(t, "  "));
    }
}
