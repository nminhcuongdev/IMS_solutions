using Microsoft.EntityFrameworkCore;
using Ims.Api.Hosting;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Picking;
using Ims.Domain.Receiving;
using Ims.Domain.Returns;
using Ims.Domain.Shipping;
using Ims.Infrastructure.Persistence;
using Xunit;

namespace Ims.Api.Tests.Inventory;

public class ConcurrentWriteTests
{
    private static async Task<TestDb> SeedAsync()
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var today = DateOnly.FromDateTime(now);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01", UomCode = "pcs",
            PackingTypeCode = "BOX", PackageQty = 1, IsActive = true, CreatedAt = now,
        });
        var stock = new Stock
        {
            ItemCode = "IT-01", SerialNo = "SN-1", FactoryCode = "FAC-01", LocationCode = "W1_ZA",
            LotNo = "LOT-1", StockQty = 10, UpdatedAt = now,
        };
        stock.Recalculate();
        t.Db.Stocks.Add(stock);
        t.Db.ReceivingPlans.Add(new ReceivingPlan
        {
            PoNo = "PO-1", ItemCode = "IT-01", Qty = 10, ScheduledDate = today, CreatedAt = now,
        });
        t.Db.ShippingPlans.Add(new ShippingPlan
        {
            SoNo = "SO-1", ItemCode = "IT-01", Qty = 10, ScheduledDate = today, CreatedAt = now,
        });
        t.Db.PickingPlans.Add(new PickingPlan
        {
            SoNo = "SO-1", ItemCode = "IT-01", LocationCode = "W1_ZA", Qty = 10, CreatedAt = now,
        });
        t.Db.ReturnPlans.Add(new ReturnPlan
        {
            ReturnNo = "RTN-1", ItemCode = "IT-01", Qty = 10, ScheduledDate = today, CreatedAt = now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static async Task AssertLostUpdateIsRefusedAsync<T>(
        Func<ImsDbContext, Task<T>> load, Action<T> change) where T : class
    {
        using var t = await SeedAsync();
        using var other = t.NewContext();

        var stale = await load(other);
        var fresh = await load(t.Db);
        change(fresh);
        await t.Db.SaveChangesAsync();

        change(stale);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => other.SaveChangesAsync());
    }

    [Fact]
    public Task Two_writers_on_one_stock_row_cannot_both_take_the_same_units() =>
        AssertLostUpdateIsRefusedAsync(db => db.Stocks.FirstAsync(), s => s.StockQty -= 3);

    [Fact]
    public Task Two_receipts_on_one_receiving_line_cannot_overwrite_each_other() =>
        AssertLostUpdateIsRefusedAsync(db => db.ReceivingPlans.FirstAsync(), p => p.ActualQty += 5);

    [Fact]
    public Task Two_shipments_on_one_shipping_line_cannot_overwrite_each_other() =>
        AssertLostUpdateIsRefusedAsync(db => db.ShippingPlans.FirstAsync(), p => p.ActualQty += 5);

    [Fact]
    public Task Two_picks_on_one_picking_line_cannot_overwrite_each_other() =>
        AssertLostUpdateIsRefusedAsync(db => db.PickingPlans.FirstAsync(), p => p.PickedQty += 5);

    [Fact]
    public Task Two_returns_on_one_return_line_cannot_overwrite_each_other() =>
        AssertLostUpdateIsRefusedAsync(db => db.ReturnPlans.FirstAsync(), p => p.ActualQty += 5);

    [Fact]
    public void A_write_that_lost_the_race_is_answered_409_INV005()
    {
        var (status, body) = ExceptionErrors.For(new DbUpdateConcurrencyException("stale"));

        Assert.Equal(409, status);
        Assert.Equal("INV-005", body.Code);
    }

    [Fact]
    public void Any_other_unhandled_error_stays_500_SYS004()
    {
        var (status, body) = ExceptionErrors.For(new InvalidOperationException("boom"));

        Assert.Equal(500, status);
        Assert.Equal("SYS-004", body.Code);
    }
}
