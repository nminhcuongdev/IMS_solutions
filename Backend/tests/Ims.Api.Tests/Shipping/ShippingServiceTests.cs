using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Shipping;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Shipping;
using Xunit;

namespace Ims.Api.Tests.Shipping;

public class ShippingServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static ClaimsPrincipal Worker(string code = "U001") =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, code) }, "test"));

    private static async Task SeedRefsAsync(TestDb t)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            IsLotManaged = true,
            ItemCode = "IT-01", ItemName = "Industrial Printer 4in", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackingTypeCode = "BOX", PackageQty = 10,
            IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP001", PartnerName = "ABC Manufacturing",
            IsCustomer = true, IsActive = true, CreatedAt = now,
        });
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP-DEL", PartnerName = "Gone",
            IsCustomer = true, IsActive = true, IsDeleted = true, CreatedAt = now,
        });
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-SHORT", ReasonName = "Insufficient stock",
            ReasonCategoryValue = ReasonCategory.Shortage, IsActive = true, CreatedAt = now,
        });
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-DEFECT", ReasonName = "Broken on the shelf",
            ReasonCategoryValue = ReasonCategory.Defect, IsActive = true, CreatedAt = now,
        });
        var seedStock = new Domain.Inventory.Stock
        {
            ItemCode = "IT-01", SerialNo = "SN-SEED", FactoryCode = "FAC-01", LocationCode = "W1_ZA",
            LotNo = "LOT-1", StockQty = 100, UpdatedAt = now,
        };
        seedStock.Recalculate();
        t.Db.Stocks.Add(seedStock);
        await t.Db.SaveChangesAsync();
    }

    private static ShippingPlan Plan(
        string soNo = "SO-001", string serial = "01", decimal qty = 10, decimal actual = 0,
        int status = ShippingStatus.Pending, string item = "IT-01", string? partner = "BP001",
        string? gate = "GATE-1") => new()
        {
            SoNo = soNo, SoBranchNo = serial, PartnerCode = partner, ItemCode = item,
            Qty = qty, ActualQty = actual, FactoryCode = "FAC-01", GateCode = gate,
            ScheduledDate = Today, Status = status,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), CreatedBy = "admin",
        };

    private static ShippingPlanWriteDto Valid(
        string slipNo = "SO-100", string item = "IT-01", decimal qty = 10) =>
        new(slipNo, "01", "BP001", item, qty, Today, "FAC-01", "GATE-1");

    [Fact]
    public async Task Direct_shipment_records_the_allocated_serial()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(await new ShippingService(t.Db).CreateResultAsync(
            new ShippingResultWriteDto("IT-01", 10, "SO-001", "01", LotNo: "LOT-1"), Worker(), default));

        var allocation = Assert.Single(await t.Db.ShippingResultLots.ToListAsync());
        Assert.Equal(("LOT-1", "SN-SEED", 10m), (allocation.LotNo, allocation.SerialNo, allocation.Qty));
    }


    [Fact]
    public async Task Create_records_the_signed_in_user_as_the_creator()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        ResultAssert.Created<ApiSuccess<ShippingPlanDto>>(
            await new ShippingService(t.Db).CreatePlanAsync(Valid(), Worker("U009"), default));

        Assert.Equal("U009", (await t.Db.ShippingPlans.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task Create_stores_the_line_and_returns_it_shaped_like_the_grid()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new ShippingService(t.Db).CreatePlanAsync(Valid(), Worker(), default);

        var dto = ResultAssert.Created<ApiSuccess<ShippingPlanDto>>(r).Data;
        Assert.Equal("SO-100", dto.SlipNo);
        Assert.Equal("01", dto.SerialNo);
        Assert.Equal("BP001", dto.CustomerCode);
        Assert.Equal("ABC Manufacturing", dto.CustomerName);
        Assert.Equal("Industrial Printer 4in", dto.ItemName);
        Assert.Equal("pcs", dto.Unit);
        Assert.Equal("GATE-1", dto.GateCode);
        Assert.Equal("statusNotShipped", dto.Status);
        Assert.Equal(0, dto.ActualQty);
    }

    [Fact]
    public async Task Create_defaults_the_serial_number_to_01()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new ShippingService(t.Db).CreatePlanAsync(Valid() with { SerialNo = null }, Worker(), default);

        Assert.Equal("01", ResultAssert.Created<ApiSuccess<ShippingPlanDto>>(r).Data.SerialNo);
    }

    [Fact]
    public async Task Create_rejects_a_missing_slip_number()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var r = await new ShippingService(t.Db).CreatePlanAsync(Valid(slipNo: "   "), Worker(), default);
        ResultAssert.BadRequest(r, "VAL-001");
    }

    [Fact]
    public async Task Create_rejects_a_slip_number_longer_than_the_column()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var r = await new ShippingService(t.Db).CreatePlanAsync(Valid(slipNo: new string('S', 31)), Worker(), default);
        ResultAssert.BadRequest(r, "VAL-005");
    }

    [Fact]
    public async Task Create_rejects_a_quantity_below_one()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var r = await new ShippingService(t.Db).CreatePlanAsync(Valid(qty: 0), Worker(), default);
        ResultAssert.BadRequest(r, "QTY-001");
    }

    [Fact]
    public async Task Create_rejects_an_unknown_item()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var r = await new ShippingService(t.Db).CreatePlanAsync(Valid(item: "NOPE"), Worker(), default);
        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task Create_rejects_a_soft_deleted_customer()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var r = await new ShippingService(t.Db).CreatePlanAsync(Valid() with { CustomerCode = "BP-DEL" }, Worker(), default);
        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task Create_keeps_the_gate_code_without_checking_any_master()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new ShippingService(t.Db).CreatePlanAsync(Valid() with { GateCode = "GATE-KHONG-CO-TRONG-MASTER" }, Worker(), default);

        Assert.Equal("GATE-KHONG-CO-TRONG-MASTER",
            ResultAssert.Created<ApiSuccess<ShippingPlanDto>>(r).Data.GateCode);
    }

    [Fact]
    public async Task Create_allows_the_same_slip_number_twice()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        await new ShippingService(t.Db).CreatePlanAsync(Valid(), Worker(), default);
        var r = await new ShippingService(t.Db).CreatePlanAsync(Valid() with { SerialNo = "02" }, Worker(), default);

        ResultAssert.Created<ApiSuccess<ShippingPlanDto>>(r);
        Assert.Equal(2, await t.Db.ShippingPlans.CountAsync());
    }


    [Fact]
    public async Task List_filters_by_status_using_the_shipping_names()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.AddRange(
            Plan("SO-A"), Plan("SO-B", status: ShippingStatus.Shipped));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).ListPlansAsync(default, status: "statusShipped");

        var page = ResultAssert.Ok<ApiListSuccess<ShippingPlanDto>>(r);
        Assert.Single(page.Data);
        Assert.Equal("SO-B", page.Data[0].SlipNo);
    }

    [Fact]
    public async Task List_ignores_a_status_filter_it_does_not_recognise()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.AddRange(Plan("SO-A"), Plan("SO-B"));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).ListPlansAsync(default, status: "statusArrived");

        Assert.Equal(2, ResultAssert.Ok<ApiListSuccess<ShippingPlanDto>>(r).Meta.Total);
    }

    [Fact]
    public async Task List_searches_slip_and_item_code_together()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.AddRange(Plan("SO-AAA"), Plan("SO-BBB"));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).ListPlansAsync(default, q: "AAA");

        Assert.Single(ResultAssert.Ok<ApiListSuccess<ShippingPlanDto>>(r).Data);
    }

    [Fact]
    public async Task List_keeps_cancelled_lines_visible()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(status: ShippingStatus.Cancelled));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).ListPlansAsync(default);

        Assert.Single(ResultAssert.Ok<ApiListSuccess<ShippingPlanDto>>(r).Data);
    }


    [Fact]
    public async Task ChangeStatus_rejects_an_empty_selection()
    {
        using var t = TestDb.Seeded();
        var r = await new ShippingService(t.Db).ChangeStatusAsync(new ShippingStatusChangeDto(), default);
        ResultAssert.BadRequest(r, "SEL-004");
    }

    [Fact]
    public async Task ChangeStatus_rejects_an_unknown_action()
    {
        using var t = TestDb.Seeded();
        var r = await new ShippingService(t.Db).ChangeStatusAsync(new ShippingStatusChangeDto(new[] { "SO-001" }, "explode"), default);
        ResultAssert.BadRequest(r, "SEL-006");
    }

    [Fact]
    public async Task ChangeStatus_skips_lines_that_are_already_finished()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.AddRange(
            Plan("SO-OPEN"), Plan("SO-DONE", status: ShippingStatus.Shipped));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).ChangeStatusAsync(new ShippingStatusChangeDto(new[] { "SO-OPEN", "SO-DONE" }, "done"), default);

        var res = ResultAssert.Ok<ApiSuccess<ShippingStatusChangeResult>>(r).Data;
        Assert.Equal(1, res.Updated);
        Assert.Equal(new[] { "SO-DONE" }, res.Skipped);
    }

    [Fact]
    public async Task ChangeStatus_with_lines_changes_only_the_selected_serial()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.AddRange(
            Plan("SO-001", serial: "01"), Plan("SO-001", serial: "02"),
            Plan("SO-001", serial: "03", status: ShippingStatus.Shipped));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).ChangeStatusAsync(new ShippingStatusChangeDto(null, "cancelled",
            new[]
            {
                new ShippingPlanKeyDto("SO-001", "02"), new ShippingPlanKeyDto("SO-001", "03"),
                new ShippingPlanKeyDto("SO-001", "09"),
            }), default);

        var res = ResultAssert.Ok<ApiSuccess<ShippingStatusChangeResult>>(r).Data;
        Assert.Equal(1, res.Updated);
        Assert.Equal(new[] { "SO-001/03", "SO-001/09" }, res.Skipped);
        var rows = await t.Db.ShippingPlans.AsNoTracking().ToDictionaryAsync(p => p.SoBranchNo, p => p.Status);
        Assert.Equal(ShippingStatus.Pending, rows["01"]);
        Assert.Equal(ShippingStatus.Cancelled, rows["02"]);
        Assert.Equal(ShippingStatus.Shipped, rows["03"]);
    }

    [Fact]
    public async Task ChangeStatus_reports_slips_it_could_not_find()
    {
        using var t = TestDb.Seeded();
        var r = await new ShippingService(t.Db).ChangeStatusAsync(new ShippingStatusChangeDto(new[] { "SO-GHOST" }, "cancelled"), default);

        var res = ResultAssert.Ok<ApiSuccess<ShippingStatusChangeResult>>(r).Data;
        Assert.Equal(0, res.Updated);
        Assert.Equal(new[] { "SO-GHOST" }, res.Skipped);
    }


    [Fact]
    public async Task Shortage_sets_actual_to_planned_minus_shortage()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        (await t.Db.Items.SingleAsync(i => i.ItemCode == "IT-01")).IsLotManaged = false;
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-SHORT"), default);

        var dto = ResultAssert.Ok<ApiSuccess<ShippingPlanDto>>(r).Data;
        Assert.Equal(7, dto.ActualQty);
        Assert.Equal("statusShipped", dto.Status);
        Assert.Equal("RS-SHORT", dto.ShortageReason);
    }

    [Fact]
    public async Task Shortage_requires_a_reason()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3), default);

        ResultAssert.BadRequest(r, "RSN-001");
    }

    [Fact]
    public async Task Shortage_refuses_a_reason_from_another_category()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(3, "RS-DEFECT"), default);

        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task Shortage_larger_than_planned_would_make_actual_negative()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(11, "RS-SHORT"), default);

        ResultAssert.BadRequest(r, "QTY-001");
    }

    [Fact]
    public async Task Shortage_does_not_guess_when_two_lines_share_the_slip()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.AddRange(Plan(serial: "01"), Plan(serial: "02"));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).RegisterShortageAsync("SO-001", new ShippingShortageDto(1, "RS-SHORT"), default);

        ResultAssert.NotFound(r);
    }


    [Fact]
    public async Task Result_marks_the_line_shipped_when_the_whole_quantity_goes_out()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 10, "SO-001", "01", "W1_ZA", "LOT-1"), Worker(), default);

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(r);
        var plan = await t.Db.ShippingPlans.SingleAsync();
        Assert.Equal(10, plan.ActualQty);
        Assert.Equal(ShippingStatus.Shipped, plan.Status);
    }

    [Fact]
    public async Task Result_short_of_the_plan_needs_no_reason()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 4, "SO-001", "01", "W1_ZA", "LOT-1"), Worker(), default);

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(r);
    }

    [Fact]
    public async Task Result_short_of_the_plan_is_accepted_with_a_reason()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 4, "SO-001", "01", "W1_ZA", "LOT-1", ReasonCode: "RS-SHORT"), Worker(), default);

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(r);
        var plan = await t.Db.ShippingPlans.SingleAsync();
        Assert.Equal(4, plan.ActualQty);
        Assert.Equal(ShippingStatus.Partial, plan.Status);
        Assert.Equal("RS-SHORT", plan.ReasonCode);
    }

    [Fact]
    public async Task Shipping_the_full_quantity_clears_an_earlier_shortage_reason()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();

        var short_ = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 4, "SO-001", "01", "W1_ZA", "LOT-1", ReasonCode: "RS-SHORT"), Worker(), default);
        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(short_);
        Assert.Equal("RS-SHORT", (await t.Db.ShippingPlans.SingleAsync()).ReasonCode);

        var full = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 10, "SO-001", "01", "W1_ZA", "LOT-1"), Worker(), default);

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(full);
        var plan = await t.Db.ShippingPlans.SingleAsync();
        Assert.Equal(10, plan.ActualQty);
        Assert.Equal(ShippingStatus.Shipped, plan.Status);
        Assert.Null(plan.ReasonCode);
    }

    [Fact]
    public async Task Result_refuses_to_ship_more_than_planned()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 11, "SO-001", "01", "W1_ZA", "LOT-1"), Worker(), default);

        ResultAssert.BadRequest(r, "QTY-002");
    }

    [Fact]
    public async Task Result_requires_a_lot_number()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 10, "SO-001", "01", "W1_ZA"), Worker(), default);

        ResultAssert.BadRequest(r, "VAL-001");
    }

    [Fact]
    public async Task Result_rejects_a_quantity_that_was_never_sent()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var r = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", null, "SO-001", "01", "W1_ZA", "LOT-1"), Worker(), default);

        ResultAssert.BadRequest(r, "QTY-001");
    }

    [Fact]
    public async Task Result_rejects_an_unknown_location()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 10, "SO-001", "01", "NOWHERE", "LOT-1"), Worker(), default);

        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task Result_without_a_slip_still_records_the_movement()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 3, null, null, "W1_ZA", "LOT-1"), Worker(), default);

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(r);
        Assert.Equal(1, await t.Db.ShippingResults.CountAsync());
    }

    [Fact]
    public async Task Result_does_not_touch_a_line_that_is_already_shipped()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10, actual: 10, status: ShippingStatus.Shipped));
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 5, "SO-001", "01", "W1_ZA", "LOT-1"), Worker(), default);

        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task Result_records_the_worker_from_the_token()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 3, null, null, "W1_ZA", "LOT-1"), Worker("U007"), default);

        Assert.Equal("U007", (await t.Db.ShippingResults.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task Result_keeps_the_expiry_date_it_was_given()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var expiry = Today.AddDays(90);

        await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 3, null, null, "W1_ZA", "LOT-1", expiry), Worker(), default);

        Assert.Equal(expiry, (await t.Db.ShippingResults.SingleAsync()).ExpiryDate);
    }



    private static async Task AddStockAsync(TestDb t, string serial, decimal qty,
        bool onHold = false, int? expiresInDays = null, string lot = "LOT-S")
    {
        var s = new Domain.Inventory.Stock
        {
            ItemCode = "IT-01", SerialNo = serial, FactoryCode = "FAC-01", LocationCode = "W1_ZA",
            LotNo = lot, StockQty = qty, IsOnHold = onHold,
            ExpiryDate = expiresInDays is { } d ? Today.AddDays(d) : null, UpdatedAt = DateTime.UtcNow,
        };
        s.Recalculate();
        t.Db.Stocks.Add(s);
        await t.Db.SaveChangesAsync();
    }

    private static ShippingResultWriteDto Ship(decimal qty, string? reason = null, string lot = "LOT-S") =>
        new("IT-01", qty, "SO-001", "01", LotNo: lot, ReasonCode: reason);

    [Fact]
    public async Task Result_takes_the_shipped_quantity_off_the_shelf_and_records_a_Shipping_history_row()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddStockAsync(t, "SN-1", 10);
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(
            await new ShippingService(t.Db).CreateResultAsync(Ship(10), Worker(), default));

        var stock = await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-1");
        Assert.Equal((0m, 0m), (stock.StockQty, stock.AvailableQty));
        var h = Assert.Single(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(("IT-01", "FAC-01", "W1_ZA", "LOT-S", "Shipping", -10m, "U001"),
            (h.ItemCode, h.FactoryCode, h.LocationCode, h.LotNo, h.ActionType, h.Qty, h.CreatedBy));
    }

    [Fact]
    public async Task Result_draws_from_the_earliest_expiring_stock_row_first()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddStockAsync(t, "SN-LATE", 6, expiresInDays: 30);
        await AddStockAsync(t, "SN-SOON", 6, expiresInDays: 10);
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(
            await new ShippingService(t.Db).CreateResultAsync(Ship(8, "RS-SHORT"), Worker(), default));

        Assert.Equal(0m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-SOON")).StockQty);
        Assert.Equal(4m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-LATE")).StockQty);
        Assert.Equal(new[] { -6m, -2m },
            (await t.Db.InventoryHistories.OrderBy(x => x.No).ToListAsync()).Select(x => x.Qty!.Value));
    }

    [Fact]
    public async Task Reconfirming_a_partial_line_takes_only_the_difference_off_the_shelf()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddStockAsync(t, "SN-1", 12);
        t.Db.ChangeTracker.Clear();

        await new ShippingService(t.Db).CreateResultAsync(Ship(4, "RS-SHORT"), Worker(), default);
        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(
            await new ShippingService(t.Db).CreateResultAsync(Ship(10), Worker(), default));

        Assert.Equal(2m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-1")).StockQty);
        Assert.Equal(new[] { -4m, -6m },
            (await t.Db.InventoryHistories.OrderBy(x => x.No).ToListAsync()).Select(x => x.Qty!.Value));
    }

    [Fact]
    public async Task Reconfirming_a_partial_line_lower_is_refused()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddStockAsync(t, "SN-1", 10);
        t.Db.ChangeTracker.Clear();

        await new ShippingService(t.Db).CreateResultAsync(Ship(6, "RS-SHORT"), Worker(), default);
        ResultAssert.BadRequest(
            await new ShippingService(t.Db).CreateResultAsync(Ship(4, "RS-SHORT"), Worker(), default), "QTY-001");

        Assert.Equal(4m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-1")).StockQty);
        Assert.Equal(6m, (await t.Db.ShippingPlans.SingleAsync()).ActualQty);
    }

    [Fact]
    public async Task Result_for_a_lot_with_no_stock_is_LOT_002_and_records_nothing()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddStockAsync(t, "SN-1", 10, lot: "OTHER-LOT");
        t.Db.ChangeTracker.Clear();

        ResultAssert.Error(
            await new ShippingService(t.Db).CreateResultAsync(Ship(10), Worker(), default), 409, "SCN-005");

        Assert.Empty(await t.Db.ShippingResults.ToListAsync());
        Assert.Equal(0m, (await t.Db.ShippingPlans.SingleAsync()).ActualQty);
        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
    }

    [Fact]
    public async Task Result_beyond_the_stock_on_the_shelf_is_INV_005_and_records_nothing()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddStockAsync(t, "SN-1", 5);
        t.Db.ChangeTracker.Clear();

        ResultAssert.Error(
            await new ShippingService(t.Db).CreateResultAsync(Ship(10), Worker(), default), 409, "INV-005");

        Assert.Equal(5m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-1")).StockQty);
        Assert.Empty(await t.Db.ShippingResults.ToListAsync());
    }

    [Fact]
    public async Task Result_that_only_held_stock_could_cover_is_HLD_001()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddStockAsync(t, "SN-FREE", 4);
        await AddStockAsync(t, "SN-HELD", 10, onHold: true);
        t.Db.ChangeTracker.Clear();

        ResultAssert.Error(
            await new ShippingService(t.Db).CreateResultAsync(Ship(10), Worker(), default), 409, "HLD-001");

        Assert.Equal(4m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-FREE")).StockQty);
    }

    [Fact]
    public async Task Result_of_zero_takes_nothing_off_the_shelf()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(
            await new ShippingService(t.Db).CreateResultAsync(Ship(0, "RS-SHORT"), Worker(), default));

        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
    }


    private static async Task AddPickAsync(TestDb t, decimal qty, decimal picked)
    {
        t.Db.PickingPlans.Add(new Domain.Picking.PickingPlan
        {
            SoNo = "SO-001", SoBranchNo = "01", ItemCode = "IT-01", LocationCode = "W1_ZA",
            Qty = qty, PickedQty = picked, ScheduledDate = Today,
            Status = picked >= qty ? Domain.Picking.PickingStatus.Picked : Domain.Picking.PickingStatus.NotPicked,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
    }


    [Fact]
    public async Task Result_records_the_shelf_the_goods_were_taken_from()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddStockAsync(t, "SN-1", 10);
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(
            await new ShippingService(t.Db).CreateResultAsync(Ship(10), Worker(), default));

        Assert.Equal("W1_ZA", (await t.Db.ShippingResults.SingleAsync()).LocationCode);
    }

    [Fact]
    public async Task Result_of_a_picked_line_records_the_shelf_it_was_picked_from()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddPickAsync(t, qty: 10, picked: 10);
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(
            await new ShippingService(t.Db).CreateResultAsync(Ship(10), Worker(), default));

        Assert.Equal("W1_ZA", (await t.Db.ShippingResults.SingleAsync()).LocationCode);
    }

    [Fact]
    public async Task Shipping_a_picked_line_does_not_take_the_stock_a_second_time()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddPickAsync(t, qty: 10, picked: 10);
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 10, "SO-001", "01", LotNo: "LOT-1"), Worker(), default));

        Assert.Equal(100m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-SEED")).StockQty);
        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(ShippingStatus.Shipped, (await t.Db.ShippingPlans.SingleAsync()).Status);
    }

    [Fact]
    public async Task Shipping_more_than_was_picked_is_QTY_001_and_records_nothing()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddPickAsync(t, qty: 10, picked: 6);
        t.Db.ChangeTracker.Clear();

        ResultAssert.BadRequest(await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 10, "SO-001", "01", LotNo: "LOT-1"), Worker(), default), "QTY-002");

        Assert.Empty(await t.Db.ShippingResults.ToListAsync());
        Assert.Equal(0m, (await t.Db.ShippingPlans.SingleAsync()).ActualQty);
    }

    [Fact]
    public async Task Shipping_exactly_what_was_picked_short_of_plan_is_accepted_with_a_reason()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan(qty: 10));
        await AddPickAsync(t, qty: 10, picked: 6);
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<ShippingResultDto>>(await new ShippingService(t.Db).CreateResultAsync(new ShippingResultWriteDto("IT-01", 6, "SO-001", "01", LotNo: "LOT-1", ReasonCode: "RS-SHORT"), Worker(), default));

        Assert.Equal(100m, (await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-SEED")).StockQty);
        Assert.Equal(ShippingStatus.Partial, (await t.Db.ShippingPlans.SingleAsync()).Status);
    }

    [Fact]
    public async Task ListResults_filters_by_date_range_inclusive_at_both_ends()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var day = new DateOnly(2026, 3, 10);
        t.Db.ShippingResults.AddRange(
            new ShippingResult { ItemCode = "IT-01", Qty = 1, CreatedAt = day.ToDateTime(new TimeOnly(0, 0)) },
            new ShippingResult { ItemCode = "IT-01", Qty = 1, CreatedAt = day.ToDateTime(new TimeOnly(23, 59)) },
            new ShippingResult { ItemCode = "IT-01", Qty = 1, CreatedAt = day.AddDays(1).ToDateTime(new TimeOnly(0, 1)) });
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).ListResultsAsync(default, dateFrom: day, dateTo: day);

        Assert.Equal(2, ResultAssert.Ok<ApiListSuccess<ShippingResultDto>>(r).Meta.Total);
    }


    [Fact]
    public async Task Export_writes_physical_column_names_and_escapes_commas()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.Partners.Add(new Partner
        {
            PartnerCode = "BP-COMMA", PartnerName = "Alpha, Beta & Co.",
            IsCustomer = true, IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        t.Db.ShippingPlans.Add(Plan(partner: "BP-COMMA"));
        await t.Db.SaveChangesAsync();

        var csv = await CsvReader.ReadAsync(await new ShippingService(t.Db).ExportPlansCsvAsync(default));

        Assert.StartsWith("so_no,so_branch_no,partner_code,partner_name,", csv);
        Assert.Contains("\"Alpha, Beta & Co.\"", csv);
        Assert.Contains("statusNotShipped", csv);
    }

    [Fact]
    public async Task ListResults_can_separate_lines_that_have_no_slip_number()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan());
        t.Db.ShippingResults.AddRange(
            new ShippingResult { ItemCode = "IT-01", Qty = 1, SoNo = "SO-001", SoBranchNo = "01", CreatedAt = DateTime.UtcNow },
            new ShippingResult { ItemCode = "IT-01", Qty = 1, SoNo = null, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var with = await new ShippingService(t.Db).ListResultsAsync(default, slipNoPresence: "with");
        var without = await new ShippingService(t.Db).ListResultsAsync(default, slipNoPresence: "without");
        var junk = await new ShippingService(t.Db).ListResultsAsync(default, slipNoPresence: "maybe");

        Assert.Equal(1, ResultAssert.Ok<ApiListSuccess<ShippingResultDto>>(with).Meta.Total);
        Assert.Equal(1, ResultAssert.Ok<ApiListSuccess<ShippingResultDto>>(without).Meta.Total);
        Assert.Equal(2, ResultAssert.Ok<ApiListSuccess<ShippingResultDto>>(junk).Meta.Total);
    }

    [Fact]
    public async Task ListResults_filters_by_item_name_through_the_item_master()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.Items.Add(new Item
        {
            IsLotManaged = true,
            ItemCode = "IT-02", ItemName = "RFID Label UHF", ItemGroupCode = "IG-01",
            UomCode = "rolls", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        t.Db.ShippingResults.AddRange(
            new ShippingResult { ItemCode = "IT-01", Qty = 1, CreatedAt = DateTime.UtcNow },
            new ShippingResult { ItemCode = "IT-02", Qty = 1, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).ListResultsAsync(default, itemName: "RFID");

        var list = ResultAssert.Ok<ApiListSuccess<ShippingResultDto>>(r);
        Assert.Equal("IT-02", Assert.Single(list.Data).ItemCode);
    }

    [Fact]
    public async Task Plan_grid_carries_the_packing_columns_from_the_item_master()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var r = await new ShippingService(t.Db).ListPlansAsync(default);

        var dto = Assert.Single(ResultAssert.Ok<ApiListSuccess<ShippingPlanDto>>(r).Data);
        Assert.Equal("BOX", dto.PackingTypeCode);
        Assert.Equal(10, dto.PackingQty);
    }

    [Fact]
    public async Task ExportResults_writes_physical_column_names()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.ShippingResults.Add(new ShippingResult
        {
            ItemCode = "IT-01", Qty = 4, LotNo = "LOT-1", LocationCode = "W1_ZA",
            CreatedAt = new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc), CreatedBy = "U007",
        });
        await t.Db.SaveChangesAsync();

        var csv = await CsvReader.ReadAsync(await new ShippingService(t.Db).ExportResultsCsvAsync(default));

        Assert.StartsWith("no,created_at,item_code,item_name,qty,unit,so_no,so_branch_no,", csv);
        Assert.Contains("LOT-1", csv);
        Assert.Contains("statusShipped", csv);
    }

    [Fact]
    public async Task CreatePlan_rejects_the_same_slip_and_branch_twice_even_padded()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        ResultAssert.Created<ApiSuccess<ShippingPlanDto>>(await new ShippingService(t.Db).CreatePlanAsync(Valid(), Worker(), default));
        t.Db.ChangeTracker.Clear();
        var first = Valid();

        var dup = await new ShippingService(t.Db).CreatePlanAsync(first with { SlipNo = " " + first.SlipNo + " ", SerialNo = " " + (first.SerialNo ?? "01") + " " }, Worker(), default);

        ResultAssert.Conflict(dup);
        Assert.Equal(1, await t.Db.ShippingPlans.CountAsync());
    }

    [Fact]
    public async Task CreatePlan_refuses_a_partner_that_is_not_a_customer()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.Partners.Add(new Partner { PartnerCode = "SUP-1", PartnerName = "Supplier", IsSupplier = true, IsActive = true, CreatedAt = DateTime.Now });
        await t.Db.SaveChangesAsync();
        ResultAssert.NotFound(await new ShippingService(t.Db).CreatePlanAsync(Valid() with { CustomerCode = "SUP-1" }, Worker(), default));
    }
}
