using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Inventory;
using Ims.Api.Features.Items;
using Ims.Api.Features.Receiving;
using Ims.Api.Features.Shipping;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Shipping;
using Xunit;

namespace Ims.Api.Tests.Clock;

public class LocalClockTests
{
    private static ClaimsPrincipal Worker() =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U001") }, "test"));

    private static void OnLocalClock(DateTime stamp)
    {
        var drift = (stamp - DateTime.Now).Duration();
        Assert.True(drift < TimeSpan.FromMinutes(2), $"{stamp:O} is {drift} away from local time {DateTime.Now:O}");
    }

    private static async Task<TestDb> SeedAsync()
    {
        var t = TestDb.Seeded();
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackageQty = 1, IsActive = true, CreatedAt = DateTime.Now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Contoso", IsSupplier = true, IsCustomer = true, IsActive = true, CreatedAt = DateTime.Now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    [Fact]
    public async Task A_receipt_stamps_its_result_stock_and_history_on_the_local_clock()
    {
        using var t = await SeedAsync();

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await new ReceivingService(t.Db).CreateResultAsync(
            new ReceivingResultWriteDto("IT-01", 5, null, null, "W1_ZA", LotNo: "LOT-A"), Worker(), default));

        OnLocalClock((await t.Db.ReceivingResults.SingleAsync()).CreatedAt);
        OnLocalClock((await t.Db.Stocks.SingleAsync()).UpdatedAt);
        OnLocalClock((await t.Db.InventoryHistories.SingleAsync()).CreatedAt);
    }

    [Fact]
    public async Task A_shipment_stamps_its_plan_result_and_stock_on_the_local_clock()
    {
        using var t = await SeedAsync();
        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await new ReceivingService(t.Db).CreateResultAsync(
            new ReceivingResultWriteDto("IT-01", 5, null, null, "W1_ZA", LotNo: "LOT-A"), Worker(), default));
        t.Db.ChangeTracker.Clear();
        t.Db.ShippingPlans.Add(new ShippingPlan
        {
            SoNo = "SO-1", SoBranchNo = "01", PartnerCode = "BP001", ItemCode = "IT-01", Qty = 2,
            Status = ShippingStatus.Pending, CreatedAt = DateTime.Now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(await new ShippingService(t.Db).CreateResultAsync(
            new ShippingResultWriteDto("IT-01", 2, "SO-1", "01", LotNo: "LOT-A"), Worker(), default));

        OnLocalClock((await t.Db.ShippingResults.SingleAsync()).CreatedAt);
        OnLocalClock((await t.Db.Stocks.SingleAsync()).UpdatedAt);
        foreach (var h in await t.Db.InventoryHistories.ToListAsync()) OnLocalClock(h.CreatedAt);
    }

    [Fact]
    public async Task A_master_row_is_stamped_on_the_local_clock()
    {
        using var t = await SeedAsync();

        ResultAssert.Created<ApiSuccess<ItemDto>>(await new ItemService(t.Db).CreateAsync(
            new ItemWriteDto("IT-02", "Second", "IG-01"), default));

        OnLocalClock((await t.Db.Items.SingleAsync(i => i.ItemCode == "IT-02")).CreatedAt);
    }
}
