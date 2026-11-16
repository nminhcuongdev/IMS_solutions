using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Shipping;
using Ims.Api.Features.Picking;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Picking;
using Ims.Domain.Shipping;
using Xunit;

namespace Ims.Api.Tests.Shipping;

public class ShippingShortageStockTests
{
    private static ClaimsPrincipal Worker() =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U001") }, "test"));

    private static async Task<TestDb> SeedAsync(decimal planQty = 10, decimal onShelf = 100)
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackingTypeCode = "BOX", PackageQty = 10, IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "ABC", IsCustomer = true, IsActive = true, CreatedAt = now,
        });
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-SHORT", ReasonName = "Short", ReasonCategoryValue = ReasonCategory.Shortage,
            IsActive = true, CreatedAt = now,
        });
        var stock = new Stock
        {
            ItemCode = "IT-01", SerialNo = "SN-SEED", FactoryCode = "FAC-01", LocationCode = "W1_ZA",
            LotNo = "LOT-1", StockQty = onShelf, UpdatedAt = now,
        };
        stock.Recalculate();
        t.Db.Stocks.Add(stock);
        t.Db.ShippingPlans.Add(new ShippingPlan
        {
            SoNo = "SO-001", SoBranchNo = "01", PartnerCode = "BP001", ItemCode = "IT-01",
            Qty = planQty, ActualQty = 0, FactoryCode = "FAC-01",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = ShippingStatus.Pending, CreatedAt = now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static Task<ServiceResult> ShipAsync(TestDb t, decimal qty) =>
        new ShippingService(t.Db).CreateResultAsync(
            new ShippingResultWriteDto("IT-01", qty, "SO-001", "01", LotNo: "LOT-1", ReasonCode: "RS-SHORT"),
            Worker(), default);

    private static async Task<decimal> OnShelfAsync(TestDb t)
    {
        t.Db.ChangeTracker.Clear();
        return (await t.Db.Stocks.ToListAsync()).Sum(s => s.StockQty);
    }

    [Fact]
    public async Task Declaring_a_shortage_takes_what_did_ship_off_the_shelf()
    {
        using var t = await SeedAsync();

        var res = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default);

        Assert.Equal(7m, ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(res).Data!.ActualQty);
        Assert.Equal(93m, await OnShelfAsync(t));
        var history = Assert.Single(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(InventoryAction.Shipping, history.ActionType);
        Assert.Equal(-7m, history.Qty);
    }

    [Fact]
    public async Task A_shortage_after_a_partial_shipment_takes_only_the_difference()
    {
        using var t = await SeedAsync();
        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(await ShipAsync(t, 4));

        var res = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default);

        Assert.Equal(7m, ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(res).Data!.ActualQty);
        Assert.Equal(93m, await OnShelfAsync(t));
    }

    [Fact]
    public async Task A_shortage_cannot_declare_less_than_has_already_shipped()
    {
        using var t = await SeedAsync();
        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(await ShipAsync(t, 8));

        var res = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default);

        ResultAssert.BadRequest(res, "QTY-001");
        Assert.Equal(92m, await OnShelfAsync(t));
        Assert.Equal(8m, (await t.Db.ShippingPlans.FirstAsync()).ActualQty);
    }

    [Fact]
    public async Task A_shortage_the_shelf_cannot_cover_is_refused_and_changes_nothing()
    {
        using var t = await SeedAsync(planQty: 200, onShelf: 100);

        var res = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(0, "RS-SHORT"), default);

        ResultAssert.Error(res, 409, "INV-005");
        Assert.Equal(100m, await OnShelfAsync(t));
        var plan = await t.Db.ShippingPlans.FirstAsync();
        Assert.Equal(0m, plan.ActualQty);
        Assert.Null(plan.ReasonCode);
    }

    private static async Task PickedAsync(TestDb t, decimal picked, string location = "W1_ZA", string? lot = "LOT-1", bool withResult = true)
    {
        t.Db.PickingPlans.Add(new PickingPlan
        {
            SoNo = "SO-001", SoBranchNo = "01", ItemCode = "IT-01", LocationCode = "W1_ZA",
            Qty = 10, PickedQty = picked, Status = picked >= 10 ? PickingStatus.Picked : PickingStatus.NotPicked,
            CreatedAt = DateTime.UtcNow,
        });
        if (withResult)
            t.Db.PickingResults.Add(new PickingResult
            {
                SoNo = "SO-001", SoBranchNo = "01", ItemCode = "IT-01", Qty = picked,
                LocationCode = location, LotNo = lot, CreatedAt = DateTime.UtcNow,
            });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task A_shortage_on_a_picked_line_puts_what_was_picked_but_not_shipped_back_on_the_shelf()
    {
        using var t = await SeedAsync();
        await PickedAsync(t, 10);

        var res = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default);

        Assert.Equal(7m, ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(res).Data!.ActualQty);
        Assert.Equal(103m, await OnShelfAsync(t));
        Assert.Equal(103m, (await t.Db.Stocks.SingleAsync()).StockQty);
        var history = Assert.Single(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(InventoryAction.Picking, history.ActionType);
        Assert.Equal(3m, history.Qty);
        Assert.Equal("W1_ZA", history.LocationCode);
        Assert.Equal("LOT-1", history.LotNo);
        var pick = await t.Db.PickingPlans.SingleAsync();
        Assert.Equal(7m, pick.PickedQty);
        Assert.Equal(PickingStatus.NotPicked, pick.Status);
    }

    [Fact]
    public async Task Picked_goods_are_returned_even_when_the_shipped_part_was_already_confirmed()
    {
        using var t = await SeedAsync();
        await PickedAsync(t, 10);
        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(await ShipAsync(t, 7));

        var res = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default);

        Assert.Equal(7m, ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(res).Data!.ActualQty);
        Assert.Equal(103m, await OnShelfAsync(t));
        Assert.Equal(7m, (await t.Db.PickingPlans.SingleAsync()).PickedQty);
    }

    [Fact]
    public async Task Picked_goods_with_no_matching_shelf_row_get_a_row_of_their_own()
    {
        using var t = await SeedAsync();
        await PickedAsync(t, 10, lot: "LOT-OTHER");

        ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(
            await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default));

        var back = await t.Db.Stocks.SingleAsync(s => s.LotNo == "LOT-OTHER");
        Assert.Equal(3m, back.StockQty);
        Assert.Equal(3m, back.AvailableQty);
        Assert.Equal("W1_ZA", back.LocationCode);
        Assert.Equal("FAC-01", back.FactoryCode);
        Assert.Equal(100m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-SEED")).StockQty);
    }

    [Fact]
    public async Task A_shortage_matching_what_was_picked_returns_nothing()
    {
        using var t = await SeedAsync();
        await PickedAsync(t, 7);

        ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(
            await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default));

        Assert.Equal(100m, await OnShelfAsync(t));
        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(7m, (await t.Db.PickingPlans.SingleAsync()).PickedQty);
    }

    [Fact]
    public async Task A_second_shortage_cannot_return_the_same_picked_goods_twice()
    {
        using var t = await SeedAsync();
        await PickedAsync(t, 10);
        ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(
            await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default));
        t.Db.ChangeTracker.Clear();

        ResultAssert.NotFound(
            await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default));

        Assert.Equal(103m, await OnShelfAsync(t));
    }

    [Fact]
    public async Task A_multi_lot_shortage_restores_the_last_allocated_serial()
    {
        using var t = await SeedAsync();
        var first = await t.Db.Stocks.SingleAsync();
        first.StockQty = 3;
        first.Recalculate();
        var second = new Stock
        {
            ItemCode = "IT-01", SerialNo = "SN-Z-SECOND", FactoryCode = "FAC-01",
            LocationCode = "W1_ZA", LotNo = "LOT-2", StockQty = 5, UpdatedAt = DateTime.Now,
        };
        second.Recalculate();
        t.Db.Stocks.Add(second);
        t.Db.PickingPlans.Add(new PickingPlan
        {
            SoNo = "SO-001", SoBranchNo = "01", ItemCode = "IT-01", LocationCode = "W1_ZA",
            Qty = 10, CreatedAt = DateTime.Now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<PickingResultDto>>(await new PickingService(t.Db).CreateResultAsync(
            new PickingResultWriteDto("IT-01", 8, "SO-001", "01"), Worker(), default));
        ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(await new ShippingService(t.Db).RegisterShortageAsync(
            "SO-001", new ShippingShortageDto(5, "RS-SHORT"), default));

        t.Db.ChangeTracker.Clear();
        var children = await t.Db.PickingResultLots.OrderBy(l => l.SerialNo).ToListAsync();
        Assert.Equal(new[] { 3m, 2m }, children.Select(c => c.Qty));
        Assert.Equal(3m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-Z-SECOND")).StockQty);
        Assert.Equal("LOT-2", (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-Z-SECOND")).LotNo);
    }

    [Fact]
    public async Task Returning_an_entire_picking_allocation_deletes_its_child_row()
    {
        using var t = await SeedAsync();
        t.Db.PickingPlans.Add(new PickingPlan
        {
            SoNo = "SO-001", SoBranchNo = "01", ItemCode = "IT-01", LocationCode = "W1_ZA",
            Qty = 10, CreatedAt = DateTime.Now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        ResultAssert.Created<ApiSuccess<PickingResultDto>>(await new PickingService(t.Db).CreateResultAsync(
            new PickingResultWriteDto("IT-01", 8, "SO-001", "01"), Worker(), default));

        ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(await new ShippingService(t.Db).RegisterShortageAsync(
            "SO-001", new ShippingShortageDto(10, "RS-SHORT"), default));

        t.Db.ChangeTracker.Clear();
        Assert.Empty(await t.Db.PickingResultLots.ToListAsync());
        Assert.Equal(100m, (await t.Db.Stocks.SingleAsync()).StockQty);
    }
}
