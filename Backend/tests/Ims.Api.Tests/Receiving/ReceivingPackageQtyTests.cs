using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Ht;
using Ims.Api.Features.Receiving;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Receiving;

public class ReceivingPackageQtyTests
{
    private static ClaimsPrincipal Worker() =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U001") }, "test"));

    private static async Task<TestDb> SeedAsync(decimal planQty, decimal packageQty, decimal actual = 0)
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            IsLotManaged = true,
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackingTypeCode = "BOX", PackageQty = packageQty,
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
            PoNo = "PO-001", PoBranchNo = "01", PartnerCode = "BP001", ItemCode = "IT-01",
            Qty = planQty, ActualQty = actual, FactoryCode = "FAC-01",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = actual > 0 ? ReceivingStatus.Partial : ReceivingStatus.NotArrived,
            LotNo = "LOT-A", LocationCode = "W1_ZA", CreatedAt = now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static ReceivingResultWriteDto Scan(decimal qty, string? reason = null) =>
        new("IT-01", qty, "PO-001", "01", "W1_ZA", LotNo: "LOT-A",
            ExpiryDate: null, ReasonCode: reason, InvoiceNo: null);

    [Fact]
    public async Task A_full_case_of_package_qty_units_needs_no_reason()
    {
        using var t = await SeedAsync(planQty: 200, packageQty: 10);

        var res = await new ReceivingService(t.Db).CreateResultAsync(Scan(10), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
        Assert.Equal(10m, (await t.Db.ReceivingPlans.FirstAsync()).ActualQty);
    }

    [Fact]
    public async Task A_partial_scan_needs_no_reason()
    {
        using var t = await SeedAsync(planQty: 30, packageQty: 10);

        var res = await new ReceivingService(t.Db).CreateResultAsync(Scan(5), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal(ReceivingStatus.Partial, plan.Status);
        Assert.Null(plan.ReasonCode);
    }

    [Fact]
    public async Task The_last_case_is_only_expected_to_hold_what_is_left_of_the_line()
    {
        using var t = await SeedAsync(planQty: 30, packageQty: 10, actual: 25);

        var res = await new ReceivingService(t.Db).CreateResultAsync(Scan(5), Worker(), default);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(res);
        var plan = await t.Db.ReceivingPlans.FirstAsync();
        Assert.Equal(30m, plan.ActualQty);
        Assert.Equal(ReceivingStatus.Arrived, plan.Status);
    }

    [Fact]
    public async Task A_loose_item_is_received_against_the_rest_of_the_line()
    {
        using var t = await SeedAsync(planQty: 50, packageQty: 1);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(
            await new ReceivingService(t.Db).CreateResultAsync(Scan(1), Worker(), default));
        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(
            await new ReceivingService(t.Db).CreateResultAsync(Scan(49), Worker(), default));
        Assert.Equal(ReceivingStatus.Arrived, (await t.Db.ReceivingPlans.FirstAsync()).Status);
    }
}
