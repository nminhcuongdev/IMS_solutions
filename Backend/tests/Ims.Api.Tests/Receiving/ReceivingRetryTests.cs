using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Receiving;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Receiving;

public class ReceivingRetryTests
{
    private static ClaimsPrincipal Worker() =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U001") }, "test"));

    private static async Task<TestDb> SeedAsync()
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackingTypeCode = "BOX", PackageQty = 5, IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "Contoso", IsSupplier = true, IsActive = true, CreatedAt = now,
        });
        t.Db.ReceivingPlans.Add(new ReceivingPlan
        {
            PoNo = "PO-A", PoBranchNo = "01", PartnerCode = "BP001", ItemCode = "IT-01",
            Qty = 15, ActualQty = 0, FactoryCode = "FAC-01",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = ReceivingStatus.NotArrived,
            LotNo = "LOT-A", CreatedAt = now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static async Task<ServiceResult> ReceiveAsync(TestDb t, string? requestId)
    {
        var res = await new ReceivingService(t.Db).CreateResultAsync(
            new ReceivingResultWriteDto("IT-01", 5, "PO-A", "01", "W1_ZA", LotNo: "LOT-A", RequestId: requestId),
            Worker(), default);
        t.Db.ChangeTracker.Clear();
        return res;
    }

    [Fact]
    public async Task A_retried_package_is_recorded_once_and_answered_with_the_first_result()
    {
        using var t = await SeedAsync();

        var first = ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await ReceiveAsync(t, "req-1"));
        var retry = ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await ReceiveAsync(t, "req-1"));

        Assert.Equal(first.Data!.No, retry.Data!.No);
        Assert.Single(await t.Db.ReceivingResults.ToListAsync());
        Assert.Single(await t.Db.Stocks.ToListAsync());
        Assert.Single(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(5m, (await t.Db.ReceivingPlans.SingleAsync()).ActualQty);
    }

    [Fact]
    public async Task Two_different_requests_are_two_packages()
    {
        using var t = await SeedAsync();

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await ReceiveAsync(t, "req-1"));
        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await ReceiveAsync(t, "req-2"));

        Assert.Equal(2, await t.Db.ReceivingResults.CountAsync());
        Assert.Equal(10m, (await t.Db.ReceivingPlans.SingleAsync()).ActualQty);
    }

    [Fact]
    public async Task Without_a_request_id_every_call_is_a_new_package()
    {
        using var t = await SeedAsync();

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await ReceiveAsync(t, null));
        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await ReceiveAsync(t, null));

        Assert.Equal(2, await t.Db.ReceivingResults.CountAsync());
    }

    [Fact]
    public async Task A_request_id_longer_than_the_column_is_VAL005()
    {
        using var t = await SeedAsync();

        ResultAssert.BadRequest(await ReceiveAsync(t, new string('x', 41)), "VAL-005");

        Assert.Empty(await t.Db.ReceivingResults.ToListAsync());
    }
}
