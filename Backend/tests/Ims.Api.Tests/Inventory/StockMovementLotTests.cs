using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Inventory;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Inventory;

public class StockMovementLotTests
{
    private static async Task<TestDb> SeedAsync(params (string Serial, string? Lot, decimal Qty)[] rows)
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01", UomCode = "pcs",
            PackingTypeCode = "BOX", PackageQty = 1, IsActive = true, CreatedAt = now,
        });
        foreach (var (serial, lot, qty) in rows)
        {
            var s = new Stock
            {
                ItemCode = "IT-01", SerialNo = serial, FactoryCode = "FAC-01", LocationCode = "W1_ZA",
                LotNo = lot, StockQty = qty, UpdatedAt = now,
            };
            s.Recalculate();
            t.Db.Stocks.Add(s);
        }
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static async Task<ServiceResult?> TakeAsync(TestDb t, string? lot, decimal qty)
    {
        var res = await StockMovement.TakeAsync(t.Db, "IT-01", lot, null, qty,
            InventoryAction.Shipping, null, "U001", ErrorCatalog.Lot002(), default);
        if (res is null) await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return res;
    }

    private static async Task<decimal> QtyAsync(TestDb t, string serial) =>
        (await t.Db.Stocks.SingleAsync(s => s.SerialNo == serial)).StockQty;

    [Fact]
    public async Task A_lot_on_the_label_can_be_taken_from_stock_the_books_hold_without_a_lot()
    {
        using var t = await SeedAsync(("SN-NOLOT", null, 10));

        Assert.Null(await TakeAsync(t, "LOT-A", 4));

        Assert.Equal(6m, await QtyAsync(t, "SN-NOLOT"));
    }

    [Fact]
    public async Task An_empty_lot_counts_as_no_lot()
    {
        using var t = await SeedAsync(("SN-EMPTY", "", 10));

        Assert.Null(await TakeAsync(t, "LOT-A", 4));

        Assert.Equal(6m, await QtyAsync(t, "SN-EMPTY"));
    }

    [Fact]
    public async Task The_exact_lot_is_used_before_stock_without_a_lot()
    {
        using var t = await SeedAsync(("SN-A-NOLOT", null, 10), ("SN-Z-EXACT", "LOT-A", 5));

        Assert.Null(await TakeAsync(t, "LOT-A", 7));

        Assert.Equal(0m, await QtyAsync(t, "SN-Z-EXACT"));
        Assert.Equal(8m, await QtyAsync(t, "SN-A-NOLOT"));
    }

    [Fact]
    public async Task Another_lot_is_still_not_a_match()
    {
        using var t = await SeedAsync(("SN-B", "LOT-B", 10));

        ResultAssert.Error((await TakeAsync(t, "LOT-A", 4))!, 404, "LOT-002");

        Assert.Equal(10m, await QtyAsync(t, "SN-B"));
    }

    [Fact]
    public async Task Managed_item_does_not_take_stock_without_a_lot()
    {
        using var t = await SeedAsync(("SN-NOLOT", null, 10));
        var item = await t.Db.Items.SingleAsync();
        item.IsLotManaged = true;
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Error((await TakeAsync(t, "LOT-A", 4))!, 409, "SCN-006");
        Assert.Equal(10m, await QtyAsync(t, "SN-NOLOT"));
    }

    [Fact]
    public async Task Allocation_records_the_quantity_taken_from_each_serial()
    {
        using var t = await SeedAsync(("SN-A", "LOT-A", 3), ("SN-B", "LOT-A", 5));
        var allocations = new List<StockAllocation>();

        var result = await StockMovement.TakeAsync(t.Db, "IT-01", "LOT-A", null, 6,
            InventoryAction.Shipping, null, "U001", ErrorCatalog.Lot002(), default,
            allocations: allocations);

        Assert.Null(result);
        Assert.Equal(6m, allocations.Sum(a => a.Qty));
        Assert.Equal(new[] { 3m, 3m }, allocations.Select(a => a.Qty));
        Assert.Equal(new[] { "SN-A", "SN-B" }, allocations.Select(a => a.SerialNo));
        Assert.All(t.Db.InventoryHistories.Local, h => Assert.NotNull(h.SerialNo));
    }

    [Fact]
    public async Task Consumption_can_choose_its_shortage_code()
    {
        using var t = await SeedAsync(("SN-A", "LOT-A", 2));

        var result = await StockMovement.TakeAsync(t.Db, "IT-01", "LOT-A", null, 3,
            InventoryAction.Shipping, null, "U001", ErrorCatalog.Lot002(), default,
            shortageError: ErrorCatalog.Inv009());

        ResultAssert.Error(result!, 409, "INV-009");
    }

    [Fact]
    public async Task Optional_expiry_refusal_leaves_stock_untouched()
    {
        using var t = await SeedAsync(("SN-A", "LOT-A", 4));
        var stock = await t.Db.Stocks.SingleAsync();
        stock.ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var result = await StockMovement.TakeAsync(t.Db, "IT-01", "LOT-A", null, 3,
            InventoryAction.Shipping, null, "U001", ErrorCatalog.Lot002(), default,
            refuseExpired: true);

        ResultAssert.Error(result!, 409, "INV-001");
        Assert.Equal(4m, await QtyAsync(t, "SN-A"));
    }
}
