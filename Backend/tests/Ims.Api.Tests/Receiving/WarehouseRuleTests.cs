using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Inventory;
using Ims.Api.Features.Receiving;
using Ims.Api.Features.Traceability;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Xunit;

namespace Ims.Api.Tests.Receiving;

public class WarehouseRuleTests
{
    private static ClaimsPrincipal Worker() =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U001") }, "test"));

    private static async Task<TestDb> SeedAsync(bool lotManaged = true)
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 1, 1);
        t.Db.Items.Add(new Item
        {
            ItemCode = "CL-RM1", ItemName = "Raw", ItemGroupCode = "IG-01", UomCode = "pcs",
            PackageQty = 1, IsLotManaged = lotManaged, IsActive = true, CreatedAt = now,
        });
        t.Db.Factories.Add(new Factory { FactoryCode = "FAC-02", FactoryName = "Second", IsActive = true, CreatedAt = now });
        t.Db.Zones.Add(new Zone { ZoneCode = "ZONE-B", ZoneName = "Zone B", FactoryCode = "FAC-02", IsActive = true, CreatedAt = now });
        t.Db.Locations.Add(new Location
        {
            LocationCode = "X06", LocationType = LocationType.Large, LargeCode = "X06",
            LocationName = "Rack 6", ZoneCode = "ZONE-B", IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner { PartnerCode = "BP001", PartnerName = "Supplier", IsSupplier = true, IsActive = true, CreatedAt = now });
        t.Db.Reasons.Add(new Reason { ReasonCode = "RS-QC", ReasonName = "QC", ReasonCategoryValue = ReasonCategory.Defect, IsActive = true, CreatedAt = now });
        t.Db.Reasons.Add(new Reason { ReasonCode = "RS-SHORT", ReasonName = "Short", ReasonCategoryValue = ReasonCategory.Shortage, IsActive = true, CreatedAt = now });
        t.Db.ReceivingPlans.Add(new ReceivingPlan
        {
            PoNo = "CL-PO-1", PoBranchNo = "01", PartnerCode = "BP001", ItemCode = "CL-RM1", Qty = 100,
            FactoryCode = "FAC-01", ScheduledDate = DateOnly.FromDateTime(DateTime.Now),
            Status = ReceivingStatus.NotArrived, CreatedAt = now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static ReceivingResultWriteDto Receive(decimal qty, string? po = "CL-PO-1", string location = "W1_ZA",
        string? lot = "CL-LB", DateOnly? expiry = null, string? requestId = null) =>
        new("CL-RM1", qty, po, po is null ? null : "01", location, lot, expiry, RequestId: requestId);

    [Fact]
    public async Task Partial_receipts_without_a_reason_complete_the_line_and_leave_no_reason()
    {
        using var t = await SeedAsync();
        var service = new ReceivingService(t.Db);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await service.CreateResultAsync(Receive(60), Worker(), default));
        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(await service.CreateResultAsync(Receive(40), Worker(), default));

        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal(ReceivingStatus.Arrived, plan.Status);
        Assert.Null(plan.ReasonCode);
    }

    [Fact]
    public async Task A_shortage_after_a_partial_receipt_closes_the_line_with_the_reason()
    {
        using var t = await SeedAsync();
        await new ReceivingService(t.Db).CreateResultAsync(Receive(60), Worker(), default);
        t.Db.ChangeTracker.Clear();

        ResultAssert.Ok<ApiSuccess<ReceivingPlanDto>>(
            await new ReceivingService(t.Db).RegisterShortageAsync("CL-PO-1", new ReceivingShortageDto(40, "RS-SHORT"), default));

        var plan = await t.Db.ReceivingPlans.SingleAsync();
        Assert.Equal(ReceivingStatus.Arrived, plan.Status);
        Assert.Equal("RS-SHORT", plan.ReasonCode);
    }

    [Fact]
    public async Task Receiving_into_a_location_of_another_factory_than_the_plan_is_LOC_004()
    {
        using var t = await SeedAsync();

        var r = await new ReceivingService(t.Db).CreateResultAsync(Receive(10, location: "X06"), Worker(), default);

        ResultAssert.Error(r, 409, "LOC-004");
        Assert.Empty(await t.Db.Stocks.ToListAsync());
    }

    [Fact]
    public async Task Unplanned_receiving_takes_the_factory_of_the_location()
    {
        using var t = await SeedAsync();

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(
            await new ReceivingService(t.Db).CreateResultAsync(Receive(10, po: null, location: "X06"), Worker(), default));

        Assert.Equal("FAC-02", (await t.Db.Stocks.SingleAsync()).FactoryCode);
    }

    [Theory]
    [InlineData("-")]
    [InlineData("NA")]
    [InlineData("n/a")]
    [InlineData("0")]
    public async Task A_placeholder_lot_is_refused_for_a_lot_managed_item(string lot)
    {
        using var t = await SeedAsync();

        ResultAssert.BadRequest(await new ReceivingService(t.Db).CreateResultAsync(Receive(10, lot: lot), Worker(), default), "VAL-005");
    }

    [Fact]
    public async Task An_item_without_lot_management_needs_no_lot_and_stores_none()
    {
        using var t = await SeedAsync(lotManaged: false);

        ResultAssert.Created<ApiSuccess<ReceivingResultDto>>(
            await new ReceivingService(t.Db).CreateResultAsync(Receive(10, lot: "NA"), Worker(), default));

        Assert.Null((await t.Db.Stocks.SingleAsync()).LotNo);
        Assert.Null((await t.Db.ReceivingResults.SingleAsync()).LotNo);
    }

    [Fact]
    public async Task An_expired_lot_is_refused_at_receiving()
    {
        using var t = await SeedAsync();

        var r = await new ReceivingService(t.Db).CreateResultAsync(
            Receive(10, expiry: DateOnly.FromDateTime(DateTime.Now).AddDays(-1)), Worker(), default);

        ResultAssert.Error(r, 409, "INV-001");
        Assert.Empty(await t.Db.Stocks.ToListAsync());
    }

    [Fact]
    public async Task Disposal_and_its_restore_keep_the_trace_balance_in_line()
    {
        using var t = await SeedAsync();
        await new ReceivingService(t.Db).CreateResultAsync(Receive(40, po: null), Worker(), default);
        var serial = (await t.Db.Stocks.SingleAsync()).SerialNo;
        t.Db.ChangeTracker.Clear();

        await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("CL-RM1", serial, "RS-QC", 8), Worker(), default);
        var afterDisposal = await BalanceAsync(t);
        Assert.Equal(-8m, afterDisposal.AdjustmentQty);
        Assert.Equal(32m, afterDisposal.ExpectedQty);
        Assert.True(afterDisposal.Ok);

        var disposalNo = (await t.Db.InventoryHistories.SingleAsync(h => h.ActionType == InventoryAction.Disposal)).No;
        t.Db.ChangeTracker.Clear();
        await new InventoryService(t.Db).RestoreDisposalAsync(new RestoreDisposalDto([disposalNo]), Worker(), default);
        var afterRestore = await BalanceAsync(t);
        Assert.Equal(0m, afterRestore.AdjustmentQty);
        Assert.Equal(40m, afterRestore.StockQty);
        Assert.True(afterRestore.Ok);
    }

    [Fact]
    public async Task A_repeated_disposal_request_is_answered_from_the_first_and_moves_stock_once()
    {
        using var t = await SeedAsync();
        await new ReceivingService(t.Db).CreateResultAsync(Receive(38, po: null), Worker(), default);
        var serial = (await t.Db.Stocks.SingleAsync()).SerialNo;
        t.Db.ChangeTracker.Clear();

        var first = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("CL-RM1", serial, "RS-QC", 3, "REQ-X"), Worker(), default);
        t.Db.ChangeTracker.Clear();
        var second = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("CL-RM1", serial, "RS-QC", 3, "REQ-X"), Worker(), default);

        Assert.Equal(200, first.StatusCode);
        Assert.Equal(200, second.StatusCode);
        Assert.Equal(35m, (await t.Db.Stocks.SingleAsync()).StockQty);
        Assert.Single(await t.Db.InventoryHistories.Where(h => h.ActionType == InventoryAction.Disposal).ToListAsync());
    }

    private static async Task<TraceBalanceDto> BalanceAsync(TestDb t) =>
        ResultAssert.Ok<ApiSuccess<TraceBalanceDto>>(
            await new TraceabilityService(t.Db).BalanceAsync("CL-RM1", "CL-LB", default)).Data!;
}
