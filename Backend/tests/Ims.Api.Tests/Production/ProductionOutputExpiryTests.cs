using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Production;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Production;
using Xunit;

namespace Ims.Api.Tests.Production;

public class ProductionOutputExpiryTests
{
    private static ClaimsPrincipal User => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "admin"), new Claim("role_code", "ADMIN")
    }, "test"));

    private static TestDb Setup()
    {
        var t = TestDb.Seeded();
        var db = t.Db;
        db.ItemGroups.Add(new ItemGroup { ItemGroupCode = "FG", ItemGroupName = "Finished", Category = ItemGroupCategory.FinishedGoods, CreatedAt = DateTime.Now });
        db.Items.Add(new Item { ItemCode = "FG1", ItemName = "Finished", ItemGroupCode = "FG", UomCode = "pcs", PackageQty = 1,
            ExpirationPeriod = 180, IsLotManaged = true, CreatedAt = DateTime.Now });
        db.ProductionOrders.Add(new ProductionOrder { ProductionOrderNo = "MO1", FactoryCode = "FAC-01", OutputItemCode = "FG1",
            StorageLocationCode = "W1_ZA", PlannedQty = 10, UomCode = "pcs", OutputLotNo = "FGLOT",
            PlannedStartDate = DateOnly.FromDateTime(DateTime.Today), PlannedEndDate = DateOnly.FromDateTime(DateTime.Today), CreatedBy = "admin" });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return t;
    }

    [Fact]
    public async Task Output_refuses_an_expiry_date_already_in_the_past_and_writes_nothing()
    {
        using var t = Setup();
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        ResultAssert.Error(await new ProductionService(t.Db).OutputAsync("MO1",
            new OutputWriteDto("out1", 2, ExpiryDate: yesterday), User, default), 409, "INV-001");

        Assert.False(await t.Db.ProductionOutputs.AnyAsync());
        Assert.False(await t.Db.Stocks.AnyAsync(x => x.ItemCode == "FG1"));
    }

    [Fact]
    public async Task Output_without_an_expiry_takes_it_from_the_server_clock()
    {
        using var t = Setup();

        ResultAssert.Created<ApiSuccess<ProductionOutputResultDto>>(await new ProductionService(t.Db).OutputAsync("MO1",
            new OutputWriteDto("out1", 2), User, default));

        var stock = await t.Db.Stocks.SingleAsync(x => x.ItemCode == "FG1");
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today.AddDays(180)), stock.ExpiryDate);
    }
}
