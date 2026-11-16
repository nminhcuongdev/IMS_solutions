using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Receiving;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Receiving;

public class ReceivingStatusChangeTests
{
    private static async Task SeedAsync(TestDb t, string? defaultLocation = "W1_ZA")
    {
        t.Db.Items.Add(new Item
        {
            IsLotManaged = true,
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01", UomCode = "pcs",
            PackingTypeCode = "BOX", PackageQty = 0, DefaultLocationCode = defaultLocation,
            IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Supplier", IsSupplier = true, IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
    }

    private static ReceivingPlan Plan(string poNo, decimal qty = 10, decimal actual = 0,
        int status = ReceivingStatus.NotArrived, string? location = null) => new()
    {
        PoNo = poNo, PoBranchNo = "01", PartnerCode = "BP001", ItemCode = "IT-01",
        Qty = qty, ActualQty = actual, FactoryCode = "FAC-01", Status = status,
        LotNo = "LOT-A", LocationCode = location, CreatedAt = DateTime.UtcNow, CreatedBy = "admin",
    };

    private static ReceivingService Service(TestDb t) => new(t.Db);

    private static async Task<ReceivingStatusChangeResult> ChangeAsync(TestDb t, string action, params string[] poNos)
    {
        t.Db.ChangeTracker.Clear();
        var res = await Service(t).ChangeStatusAsync(new ReceivingStatusChangeDto(poNos, action), default);
        t.Db.ChangeTracker.Clear();
        return ResultAssert.Ok<ApiSuccess<ReceivingStatusChangeResult>>(res).Data!;
    }

    [Fact]
    public async Task Done_receives_the_whole_line_at_the_item_default_location()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReceivingPlans.Add(Plan("PO-A", qty: 10));
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, "done", "PO-A");

        Assert.Equal(1, data.Updated);
        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal(ReceivingStatus.Arrived, plan.Status);
        Assert.Equal(10m, plan.ActualQty);
        var result = await t.Db.ReceivingResults.SingleAsync();
        Assert.Equal(("PO-A", "01", 10m, "W1_ZA", "LOT-A"),
            (result.PoNo, result.PoBranchNo, result.Qty, result.LocationCode, result.LotNo));
        var stock = await t.Db.Stocks.SingleAsync();
        Assert.Equal((10m, "W1_ZA"), (stock.StockQty, stock.LocationCode));
        var history = await t.Db.InventoryHistories.SingleAsync();
        Assert.Equal((InventoryAction.Receiving, 10m), (history.ActionType, history.Qty));
    }

    [Fact]
    public async Task Done_requires_a_lot_for_a_managed_item()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        var item = await t.Db.Items.SingleAsync();
        item.IsLotManaged = true;
        var plan = Plan("PO-A");
        plan.LotNo = null;
        t.Db.ReceivingPlans.Add(plan);
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var result = await Service(t).ChangeStatusAsync(
            new ReceivingStatusChangeDto(new[] { "PO-A" }, "done"), default);

        ResultAssert.BadRequest(result, "VAL-001");
        Assert.Empty(await t.Db.ReceivingResults.ToListAsync());
    }

    [Fact]
    public async Task Done_on_a_partial_line_receives_only_the_remainder()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReceivingPlans.Add(Plan("PO-A", qty: 10, actual: 4, status: ReceivingStatus.Partial));
        await t.Db.SaveChangesAsync();

        await ChangeAsync(t, "done", "PO-A");

        Assert.Equal(10m, (await t.Db.ReceivingPlans.SingleAsync()).ActualQty);
        Assert.Equal(6m, (await t.Db.ReceivingResults.SingleAsync()).Qty);
        Assert.Equal(6m, (await t.Db.InventoryHistories.SingleAsync()).Qty);
    }

    [Fact]
    public async Task Done_falls_back_to_the_plan_location_when_the_item_has_none()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t, defaultLocation: null);
        t.Db.ReceivingPlans.Add(Plan("PO-A", location: "W1"));
        await t.Db.SaveChangesAsync();

        await ChangeAsync(t, "done", "PO-A");

        Assert.Equal("W1", (await t.Db.Stocks.SingleAsync()).LocationCode);
    }

    [Fact]
    public async Task Done_skips_a_line_with_nowhere_to_put_the_goods()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t, defaultLocation: null);
        t.Db.ReceivingPlans.Add(Plan("PO-A"));
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, "done", "PO-A");

        Assert.Equal(0, data.Updated);
        Assert.Equal(new[] { "PO-A" }, data.Skipped);
        Assert.Equal(ReceivingStatus.NotArrived, (await t.Db.ReceivingPlans.SingleAsync()).Status);
        Assert.False(await t.Db.Stocks.AnyAsync());
    }

    [Fact]
    public async Task NotDone_resets_actual_to_zero_and_reopens_a_cancelled_line()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReceivingPlans.Add(Plan("PO-A", status: ReceivingStatus.Cancelled));
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, "notDone", "PO-A");

        Assert.Equal(1, data.Updated);
        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal((ReceivingStatus.NotArrived, 0m), (plan.Status, plan.ActualQty));
    }

    [Fact]
    public async Task NotDone_on_an_arrived_line_takes_back_the_stock_it_posted()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReceivingPlans.Add(Plan("PO-A", qty: 10, actual: 4, status: ReceivingStatus.Partial));
        await t.Db.SaveChangesAsync();
        await ChangeAsync(t, "done", "PO-A");

        var data = await ChangeAsync(t, "notDone", "PO-A");

        Assert.Equal(1, data.Updated);
        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal((ReceivingStatus.NotArrived, 0m, (string?)null), (plan.Status, plan.ActualQty, plan.ReasonCode));
        Assert.Empty(await t.Db.ReceivingResults.ToListAsync());
        var stock = await t.Db.Stocks.SingleAsync();
        Assert.Equal((0m, 0m), (stock.StockQty, stock.AvailableQty));
        var history = await t.Db.InventoryHistories.OrderBy(h => h.No).ToListAsync();
        Assert.Equal(new (string, decimal?)[] { (InventoryAction.Receiving, 6m), (InventoryAction.Receiving, -6m) },
            history.Select(h => (h.ActionType, h.Qty)).ToArray());
    }

    [Fact]
    public async Task NotDone_leaves_a_line_alone_when_its_goods_have_already_moved_on()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReceivingPlans.Add(Plan("PO-A", qty: 10));
        await t.Db.SaveChangesAsync();
        await ChangeAsync(t, "done", "PO-A");
        var stock = await t.Db.Stocks.SingleAsync();
        stock.StockQty = 3;
        stock.Recalculate();
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, "notDone", "PO-A");

        Assert.Equal(0, data.Updated);
        Assert.Equal(new[] { "PO-A" }, data.Skipped);
        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal((ReceivingStatus.Arrived, 10m), (plan.Status, plan.ActualQty));
        Assert.Single(await t.Db.ReceivingResults.ToListAsync());
        Assert.Equal(3m, (await t.Db.Stocks.SingleAsync()).StockQty);
        Assert.Single(await t.Db.InventoryHistories.ToListAsync());
    }

    [Fact]
    public async Task NotDone_leaves_a_line_alone_when_its_goods_are_reserved()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReceivingPlans.Add(Plan("PO-A", qty: 10));
        await t.Db.SaveChangesAsync();
        await ChangeAsync(t, "done", "PO-A");
        var stock = await t.Db.Stocks.SingleAsync();
        stock.ReservedQty = 1;
        stock.Recalculate();
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, "notDone", "PO-A");

        Assert.Equal(new[] { "PO-A" }, data.Skipped);
        Assert.Equal(10m, (await t.Db.Stocks.SingleAsync()).StockQty);
    }

    [Fact]
    public async Task NotDone_resets_a_line_whose_quantity_has_no_recorded_receipt()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReceivingPlans.Add(Plan("PO-A", actual: 4, status: ReceivingStatus.Partial));
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, "notDone", "PO-A");

        Assert.Equal(1, data.Updated);
        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal((ReceivingStatus.NotArrived, 0m), (plan.Status, plan.ActualQty));
        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
    }

    [Fact]
    public async Task Cancelled_closes_a_partial_line_and_keeps_what_arrived()
    {
        using var t = TestDb.Seeded();
        await SeedAsync(t);
        t.Db.ReceivingPlans.Add(Plan("PO-A", actual: 4, status: ReceivingStatus.Partial));
        await t.Db.SaveChangesAsync();

        var data = await ChangeAsync(t, "cancelled", "PO-A");

        Assert.Equal(1, data.Updated);
        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal((ReceivingStatus.Cancelled, 4m), (plan.Status, plan.ActualQty));
    }
}
