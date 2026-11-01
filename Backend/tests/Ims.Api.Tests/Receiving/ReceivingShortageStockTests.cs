using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Receiving;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Receiving;

public class ReceivingShortageStockTests
{
    private static ClaimsPrincipal Worker() =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U001") }, "test"));

    private static async Task<TestDb> SeedAsync(string? planLocation = "W1_ZA")
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackingTypeCode = "BOX", PackageQty = 5,
            IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Contoso", IsSupplier = true, IsActive = true, CreatedAt = now,
        });
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-SHORT", ReasonName = "Short", ReasonCategoryValue = ReasonCategory.Shortage,
            IsActive = true, CreatedAt = now,
        });
        t.Db.ReceivingPlans.Add(new ReceivingPlan
        {
            PoNo = "PO-A", PoBranchNo = "01", PartnerCode = "BP001", ItemCode = "IT-01",
            Qty = 15, ActualQty = 0, FactoryCode = "FAC-01",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = ReceivingStatus.NotArrived,
            LotNo = "LOT-A", LocationCode = planLocation, CreatedAt = now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static ReceivingService Service(TestDb t) => new(t.Db);

    private static Task<ServiceResult> ReceiveAsync(TestDb t, decimal qty) =>
        Service(t).CreateResultAsync(
            new ReceivingResultWriteDto("IT-01", qty, "PO-A", "01", "W1_ZA", LotNo: "LOT-A",
                ExpiryDate: null, ReasonCode: "RS-SHORT", InvoiceNo: null),
            Worker(), default);

    [Fact]
    public async Task Declaring_a_shortage_puts_what_did_arrive_on_the_shelf()
    {
        using var t = await SeedAsync();

        var res = await Service(t).RegisterShortageAsync("PO-A", new ReceivingShortageDto(4, "RS-SHORT"), default);

        Assert.Equal(11m, ResultAssert.Ok<ApiSuccess<ReceivingPlanDto>>(res).Data!.ActualQty);
        var stock = Assert.Single(await t.Db.Stocks.ToListAsync());
        Assert.Equal(11m, stock.StockQty);
        Assert.Equal("W1_ZA", stock.LocationCode);
        Assert.Equal("LOT-A", stock.LotNo);
        var history = Assert.Single(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(InventoryAction.Receiving, history.ActionType);
        Assert.Equal(11m, history.Qty);
        Assert.Equal(11m, (await t.Db.ReceivingResults.ToListAsync()).Sum(r => r.Qty));
    }

    [Fact]
    public async Task A_shortage_after_scanning_only_receives_what_the_handheld_has_not()
    {
        using var t = await SeedAsync();
        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await ReceiveAsync(t, 5));

        var res = await Service(t).RegisterShortageAsync("PO-A", new ReceivingShortageDto(4, "RS-SHORT"), default);

        Assert.Equal(11m, ResultAssert.Ok<ApiSuccess<ReceivingPlanDto>>(res).Data!.ActualQty);
        Assert.Equal(11m, (await t.Db.Stocks.ToListAsync()).Sum(s => s.StockQty));
        Assert.Equal(11m, (await t.Db.InventoryHistories.ToListAsync()).Sum(h => h.Qty ?? 0));
        Assert.Equal(11m, (await t.Db.ReceivingResults.ToListAsync()).Sum(r => r.Qty));
    }

    [Fact]
    public async Task A_shortage_cannot_declare_less_than_has_already_been_received()
    {
        using var t = await SeedAsync();
        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await ReceiveAsync(t, 12));

        var res = await Service(t).RegisterShortageAsync("PO-A", new ReceivingShortageDto(4, "RS-SHORT"), default);

        ResultAssert.BadRequest(res, "QTY-001");
        t.Db.ChangeTracker.Clear();
        Assert.Equal(12m, (await t.Db.ReceivingPlans.FirstAsync()).ActualQty);
        Assert.Equal(12m, (await t.Db.Stocks.ToListAsync()).Sum(s => s.StockQty));
    }

    [Fact]
    public async Task A_shortage_with_nowhere_to_put_the_goods_is_LOC001_and_changes_nothing()
    {
        using var t = await SeedAsync(planLocation: null);

        var res = await Service(t).RegisterShortageAsync("PO-A", new ReceivingShortageDto(4, "RS-SHORT"), default);

        ResultAssert.BadRequest(res, "LOC-001");
        t.Db.ChangeTracker.Clear();
        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal(0m, plan.ActualQty);
        Assert.Null(plan.ReasonCode);
        Assert.Empty(await t.Db.Stocks.ToListAsync());
    }
}
