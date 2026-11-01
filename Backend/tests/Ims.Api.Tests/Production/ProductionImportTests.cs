using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Ims.Api.Contracts;
using Ims.Api.Features.Import;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Production;

public class ProductionImportTests
{
    private const string Header = "Production Order No,Order Date,Factory Code,Product Code,Product Planned,Product Unit,Production Lot No,Finished Goods Lot No,Storage Location Code,Start Date,End Date,Line No,Process Code,Material Code,Material Planned,Material Unit,Production Scheduled Date\n";
    private const string Row = "MO1,2026-10-26,FAC-01,FG1,10,pcs,PL1,LOT1,W1_ZA,2026-10-26,2026-10-27,1,P1,MAT,5,pcs,\n";

    private static FormFile File(string csv)
    {
        var bytes = Encoding.UTF8.GetBytes(csv);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "orders.csv");
    }

    [Fact]
    public async Task Import_is_all_or_nothing_and_counts_orders_and_lines()
    {
        using var t = TestDb.Seeded();
        t.Db.ItemGroups.Add(new ItemGroup { ItemGroupCode = "FG", ItemGroupName = "Finished", Category = ItemGroupCategory.FinishedGoods, CreatedAt = DateTime.Now });
        t.Db.Items.Add(new Item { ItemCode = "FG1", ItemName = "Finished", ItemGroupCode = "FG", UomCode = "pcs", IsLotManaged = true, CreatedAt = DateTime.Now });
        t.Db.Items.Add(new Item { ItemCode = "MAT", ItemName = "Material", ItemGroupCode = "IG-01", UomCode = "pcs", CreatedAt = DateTime.Now });
        t.Db.Processes.Add(new Ims.Domain.Masters.Process { ProcessCode = "P1", ProcessName = "Process", CreatedAt = DateTime.Now });
        t.Db.SaveChanges();
        var service = new ImportService(t.Db);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "admin") }, "test"));
        var failed = ResultAssert.Ok<ApiSuccess<ImportResultDto>>(await service.ImportProductionOrderAsync(File(Header + Row + Row.Replace("P1", "MISSING").Replace(",1,", ",2,")), user, default));
        Assert.Equal(0, failed.Data!.ImportedCount);
        Assert.NotEmpty(failed.Data.Errors);
        Assert.Empty(t.Db.ProductionOrders);
        var ok = ResultAssert.Ok<ApiSuccess<ImportResultDto>>(await service.ImportProductionOrderAsync(File(Header + Row), user, default));
        Assert.Equal(1, ok.Data!.ImportedCount);
        Assert.Equal(1, ok.Data.ImportedLineCount);
        Assert.Single(t.Db.ProductionOrderLines);
    }
}
