using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Returns;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Returns;
using Xunit;

namespace Ims.Api.Tests.Returns;

public class ReturnStockTests
{
    private static ClaimsPrincipal Worker() =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "U001") }, "test"));

    private static async Task<TestDb> SeedAsync(decimal planQty = 10, string? defaultLocation = "W1_ZA")
    {
        var t = TestDb.Seeded();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Printer", ItemGroupCode = "IG-01", UomCode = "pcs",
            PackingTypeCode = "BOX", PackageQty = 10, DefaultLocationCode = defaultLocation,
            IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "DEF", IsCustomer = true, IsActive = true, CreatedAt = now,
        });
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-SHORT", ReasonName = "Short", ReasonCategoryValue = ReasonCategory.Shortage,
            IsActive = true, CreatedAt = now,
        });
        t.Db.ReturnPlans.Add(new ReturnPlan
        {
            ReturnNo = "RTN-001", ReturnBranchNo = "01", PartnerCode = "BP001", ItemCode = "IT-01",
            Qty = planQty, ActualQty = 0, FactoryCode = "FAC-01",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = ReturnStatus.Pending,
            LotNo = "LOT-A", CreatedAt = now,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static Task<ServiceResult> ConfirmAsync(TestDb t, decimal qty, string? returnNo = "RTN-001") =>
        new ReturnsService(t.Db).CreateResultAsync(
            new ReturnResultWriteDto("IT-01", qty, returnNo, returnNo is null ? null : "01", "W1_ZA", "LOT-A"),
            Worker(), default);

    private static async Task<List<Stock>> ShelfAsync(TestDb t)
    {
        t.Db.ChangeTracker.Clear();
        return await t.Db.Stocks.ToListAsync();
    }

    [Fact]
    public async Task Confirming_a_return_puts_the_goods_back_on_the_shelf()
    {
        using var t = await SeedAsync();

        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(await ConfirmAsync(t, 5));

        var stock = Assert.Single(await ShelfAsync(t));
        Assert.Equal(5m, stock.StockQty);
        Assert.Equal(5m, stock.AvailableQty);
        Assert.Equal("W1_ZA", stock.LocationCode);
        Assert.Equal("LOT-A", stock.LotNo);
        Assert.Equal("FAC-01", stock.FactoryCode);
        var history = Assert.Single(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(InventoryAction.Returns, history.ActionType);
        Assert.Equal(5m, history.Qty);
    }

    [Fact]
    public async Task A_second_confirmation_of_the_same_line_adds_only_the_difference()
    {
        using var t = await SeedAsync();

        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(await ConfirmAsync(t, 3));
        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(await ConfirmAsync(t, 5));

        Assert.Equal(5m, (await ShelfAsync(t)).Sum(s => s.StockQty));
        Assert.Equal(new decimal?[] { 3m, 2m },
            (await t.Db.InventoryHistories.OrderBy(h => h.No).ToListAsync()).Select(h => h.Qty).ToArray());
        Assert.Equal(5m, (await t.Db.ReturnPlans.FirstAsync()).ActualQty);
    }

    [Fact]
    public async Task Confirming_less_than_was_already_put_back_is_QTY001()
    {
        using var t = await SeedAsync();
        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(await ConfirmAsync(t, 5));

        ResultAssert.BadRequest(await ConfirmAsync(t, 3), "QTY-001");

        Assert.Equal(5m, (await ShelfAsync(t)).Sum(s => s.StockQty));
        Assert.Equal(5m, (await t.Db.ReturnPlans.FirstAsync()).ActualQty);
    }

    [Fact]
    public async Task A_return_without_a_plan_still_puts_the_goods_on_the_shelf()
    {
        using var t = await SeedAsync();

        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(await ConfirmAsync(t, 4, returnNo: null));

        Assert.Equal(4m, Assert.Single(await ShelfAsync(t)).StockQty);
    }

    [Fact]
    public async Task Declaring_a_shortage_puts_what_did_come_back_at_the_items_default_location()
    {
        using var t = await SeedAsync(planQty: 5);

        var res = await new ReturnsService(t.Db).RegisterShortageAsync("RTN-001", new ReturnShortageDto(2, "RS-SHORT", "01"), default);

        Assert.Equal(3m, ResultAssert.Ok<ApiSuccess<ReturnPlanDto>>(res).Data!.ActualQty);
        var stock = Assert.Single(await ShelfAsync(t));
        Assert.Equal(3m, stock.StockQty);
        Assert.Equal("W1_ZA", stock.LocationCode);
        Assert.Equal("LOT-A", stock.LotNo);
    }

    [Fact]
    public async Task A_shortage_with_nowhere_to_put_the_goods_is_LOC001_and_changes_nothing()
    {
        using var t = await SeedAsync(planQty: 5, defaultLocation: null);

        var res = await new ReturnsService(t.Db).RegisterShortageAsync("RTN-001", new ReturnShortageDto(2, "RS-SHORT", "01"), default);

        ResultAssert.BadRequest(res, "LOC-001");
        Assert.Empty(await ShelfAsync(t));
        Assert.Equal(0m, (await t.Db.ReturnPlans.FirstAsync()).ActualQty);
    }

    [Fact]
    public async Task A_shortage_below_what_was_already_put_back_is_QTY001()
    {
        using var t = await SeedAsync(planQty: 5);
        ResultAssert.Created<ApiSuccess<ReturnResultDto>>(await ConfirmAsync(t, 4));

        var res = await new ReturnsService(t.Db).RegisterShortageAsync("RTN-001", new ReturnShortageDto(2, "RS-SHORT", "01"), default);

        ResultAssert.BadRequest(res, "QTY-001");
        Assert.Equal(4m, (await ShelfAsync(t)).Sum(s => s.StockQty));
    }
}
