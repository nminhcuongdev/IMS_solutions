using Ims.Api.Contracts;
using Ims.Api.Features.Items;
using Ims.Api.Features.Partners;
using Ims.Api.Features.Reasons;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Picking;
using Ims.Domain.Receiving;
using Ims.Domain.Returns;
using Ims.Domain.Shipping;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class MasterDeleteReferenceTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 9, 0, 0);

    private static TestDb Warehouse()
    {
        var t = TestDb.Seeded();
        t.Db.Items.Add(new Item { ItemCode = "IT-1", ItemName = "Item", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, CreatedAt = Now });
        t.Db.Partners.Add(new Partner { PartnerCode = "BP-1", PartnerName = "Partner", IsSupplier = true, IsCustomer = true, CreatedAt = Now });
        t.Db.Reasons.Add(new Reason { ReasonCode = "RS-1", ReasonName = "Reason", ReasonCategoryValue = ReasonCategory.Shortage, CreatedAt = Now });
        t.Db.SaveChanges();
        return t;
    }

    private static async Task<bool> Deleted(TestDb t, Func<Task<ServiceResult>> delete)
    {
        var r = await delete();
        if (r.StatusCode == 409) { ResultAssert.InUse(r); return false; }
        ResultAssert.NoContent(r);
        return true;
    }

    private static Stock Stock(decimal qty, bool held = false, string? reason = null) => new()
    {
        ItemCode = "IT-1", SerialNo = "S-1", FactoryCode = "FAC-01", StockQty = qty,
        AvailableQty = held ? 0 : qty, IsOnHold = held, ReasonCode = reason, UpdatedAt = Now,
    };

    [Fact]
    public async Task An_item_with_stock_on_the_shelf_cannot_be_deleted()
    {
        using var t = Warehouse();
        t.Db.Stocks.Add(Stock(3));
        await t.Db.SaveChangesAsync();

        Assert.False(await Deleted(t, () => new ItemService(t.Db).DeleteAsync("IT-1", default)));
        Assert.False(t.Db.Items.Single(i => i.ItemCode == "IT-1").IsDeleted);
    }

    [Fact]
    public async Task An_item_whose_stock_has_run_to_zero_can_be_deleted()
    {
        using var t = Warehouse();
        t.Db.Stocks.Add(Stock(0));
        await t.Db.SaveChangesAsync();

        Assert.True(await Deleted(t, () => new ItemService(t.Db).DeleteAsync("IT-1", default)));
    }

    [Theory]
    [InlineData("rcv", ReceivingStatus.NotArrived)]
    [InlineData("rcv", ReceivingStatus.Partial)]
    [InlineData("shp", ShippingStatus.Pending)]
    [InlineData("shp", ShippingStatus.Partial)]
    [InlineData("rtn", ReturnStatus.Pending)]
    [InlineData("rtn", ReturnStatus.Partial)]
    [InlineData("pck", PickingStatus.NotPicked)]
    public async Task An_item_on_an_open_plan_cannot_be_deleted(string domain, int status)
    {
        using var t = Warehouse();
        AddPlan(t, domain, status);
        await t.Db.SaveChangesAsync();

        Assert.False(await Deleted(t, () => new ItemService(t.Db).DeleteAsync("IT-1", default)));
    }

    [Theory]
    [InlineData("rcv", ReceivingStatus.Arrived)]
    [InlineData("rcv", ReceivingStatus.Cancelled)]
    [InlineData("shp", ShippingStatus.Shipped)]
    [InlineData("rtn", ReturnStatus.Cancelled)]
    [InlineData("pck", PickingStatus.Picked)]
    public async Task Finished_or_cancelled_plans_do_not_block_any_delete(string domain, int status)
    {
        using var t = Warehouse();
        AddPlan(t, domain, status, partner: "BP-1", reason: "RS-1");
        await t.Db.SaveChangesAsync();

        Assert.True(await Deleted(t, () => new ItemService(t.Db).DeleteAsync("IT-1", default)));
        Assert.True(await Deleted(t, () => new PartnerService(t.Db).DeleteAsync("BP-1", default)));
        Assert.True(await Deleted(t, () => new ReasonService(t.Db).DeleteAsync("RS-1", default)));
    }

    [Theory]
    [InlineData("rcv")]
    [InlineData("shp")]
    [InlineData("rtn")]
    public async Task A_partner_on_an_open_plan_cannot_be_deleted(string domain)
    {
        using var t = Warehouse();
        AddPlan(t, domain, 1, partner: "BP-1");
        await t.Db.SaveChangesAsync();

        Assert.False(await Deleted(t, () => new PartnerService(t.Db).DeleteAsync("BP-1", default)));
    }

    [Theory]
    [InlineData("rcv")]
    [InlineData("shp")]
    [InlineData("rtn")]
    public async Task A_reason_on_an_open_plan_cannot_be_deleted(string domain)
    {
        using var t = Warehouse();
        AddPlan(t, domain, 1, reason: "RS-1");
        await t.Db.SaveChangesAsync();

        Assert.False(await Deleted(t, () => new ReasonService(t.Db).DeleteAsync("RS-1", default)));
    }

    [Fact]
    public async Task A_reason_holding_stock_cannot_be_deleted_until_released()
    {
        using var t = Warehouse();
        t.Db.Stocks.Add(Stock(5, held: true, reason: "RS-1"));
        await t.Db.SaveChangesAsync();

        Assert.False(await Deleted(t, () => new ReasonService(t.Db).DeleteAsync("RS-1", default)));

        var s = t.Db.Stocks.Single();
        s.IsOnHold = false;
        await t.Db.SaveChangesAsync();
        Assert.True(await Deleted(t, () => new ReasonService(t.Db).DeleteAsync("RS-1", default)));
    }

    private static void AddPlan(TestDb t, string domain, int status, string? partner = null, string? reason = null)
    {
        switch (domain)
        {
            case "rcv":
                t.Db.ReceivingPlans.Add(new ReceivingPlan { PoNo = "PO-1", ItemCode = "IT-1", Qty = 5, Status = status, PartnerCode = partner, ReasonCode = reason, CreatedAt = Now });
                break;
            case "shp":
                t.Db.ShippingPlans.Add(new ShippingPlan { SoNo = "SO-1", ItemCode = "IT-1", Qty = 5, Status = status, PartnerCode = partner, ReasonCode = reason, CreatedAt = Now });
                break;
            case "rtn":
                t.Db.ReturnPlans.Add(new ReturnPlan { ReturnNo = "RT-1", ItemCode = "IT-1", Qty = 5, Status = status, PartnerCode = partner, ReasonCode = reason, CreatedAt = Now });
                break;
            default:
                t.Db.PickingPlans.Add(new PickingPlan { SoNo = "SO-1", ItemCode = "IT-1", Qty = 5, Status = status, CreatedAt = Now });
                break;
        }
    }
}
