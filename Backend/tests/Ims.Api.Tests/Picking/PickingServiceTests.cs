using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Picking;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Picking;
using Xunit;

namespace Ims.Api.Tests.Picking;

public class PickingServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static ClaimsPrincipal Worker(string code = "U001") =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, code) }, "test"));

    private static async Task SeedRefsAsync(TestDb t)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Industrial Printer 4in", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackingTypeCode = "BOX", PackageQty = 10,
            IsActive = true, CreatedAt = now,
        });
        var seed = new Domain.Inventory.Stock
        {
            ItemCode = "IT-01", SerialNo = "SN-SEED", FactoryCode = "FAC-01", LocationCode = "W1_ZA",
            LotNo = "LOT-1", StockQty = 100, UpdatedAt = now,
        };
        seed.Recalculate();
        t.Db.Stocks.Add(seed);
        await t.Db.SaveChangesAsync();
    }


    private static Task<decimal> SeedQtyAsync(TestDb t) =>
        t.Db.Stocks.Where(s => s.SerialNo == "SN-SEED").Select(s => s.StockQty).SingleAsync();

    [Fact]
    public async Task A_pick_takes_the_goods_off_the_shelf_at_the_plan_location()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<PickingResultDto>>(await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default));

        Assert.Equal(90m, await SeedQtyAsync(t));
        var h = Assert.Single(await t.Db.InventoryHistories.ToListAsync());
        Assert.Equal(("IT-01", "W1_ZA", "LOT-1", "Picking", -10m, "U001"),
            (h.ItemCode, h.LocationCode, h.LotNo, h.ActionType, h.Qty, h.CreatedBy));
    }

    [Fact]
    public async Task Picking_again_takes_only_the_difference()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 4, "SO-001", "01"), Worker(), default);
        await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default);

        Assert.Equal(90m, await SeedQtyAsync(t));
        Assert.Equal(new[] { -4m, -6m },
            (await t.Db.InventoryHistories.OrderBy(x => x.No).ToListAsync()).Select(x => x.Qty!.Value));
    }

    [Fact]
    public async Task Picking_again_lower_is_refused()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 6, "SO-001", "01"), Worker(), default);
        ResultAssert.BadRequest(await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 4, "SO-001", "01"), Worker(), default), "QTY-001");

        Assert.Equal(94m, await SeedQtyAsync(t));
        Assert.Equal(6m, (await t.Db.PickingPlans.SingleAsync()).PickedQty);
    }

    [Fact]
    public async Task A_pick_at_a_shelf_with_no_stock_is_LOC_003_and_records_nothing()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.Stocks.RemoveRange(t.Db.Stocks);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Error(await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default), 404, "LOC-003");

        Assert.Empty(await t.Db.PickingResults.ToListAsync());
        Assert.Equal(0m, (await t.Db.PickingPlans.SingleAsync()).PickedQty);
    }

    [Fact]
    public async Task A_pick_beyond_the_stock_on_the_shelf_is_INV_005()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 200));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Error(await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 150, "SO-001", "01"), Worker(), default), 409, "INV-005");

        Assert.Equal(100m, await SeedQtyAsync(t));
        Assert.Empty(await t.Db.PickingResults.ToListAsync());
    }

    [Fact]
    public async Task A_pick_from_held_stock_is_HLD_001()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var s = await t.Db.Stocks.SingleAsync();
        s.IsOnHold = true; s.Recalculate();
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Error(await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default), 409, "HLD-001");

        Assert.Equal(100m, await SeedQtyAsync(t));
    }

    [Fact]
    public async Task A_pick_of_zero_takes_nothing_off_the_shelf()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        ResultAssert.Created<ApiSuccess<PickingResultDto>>(await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 0, "SO-001", "01"), Worker(), default));

        Assert.Equal(100m, await SeedQtyAsync(t));
        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
    }


    [Fact]
    public async Task A_pick_records_the_lot_it_took_off_the_shelf()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default);

        Assert.Equal("LOT-1", (await t.Db.PickingResults.SingleAsync()).LotNo);
    }

    [Fact]
    public async Task Picking_again_from_the_same_lot_keeps_naming_it()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 4, "SO-001", "01"), Worker(), default);
        await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default);

        Assert.Equal("LOT-1", (await t.Db.PickingResults.OrderBy(r => r.No).LastAsync()).LotNo);
    }

    [Fact]
    public async Task A_pick_drawn_from_two_lots_names_no_single_lot()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var soon = new Domain.Inventory.Stock
        {
            ItemCode = "IT-01", SerialNo = "SN-SOON", FactoryCode = "FAC-01", LocationCode = "W1_ZA",
            LotNo = "LOT-2", StockQty = 4, ExpiryDate = Today.AddDays(5), UpdatedAt = DateTime.UtcNow,
        };
        soon.Recalculate();
        t.Db.Stocks.Add(soon);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default);

        Assert.Null((await t.Db.PickingResults.SingleAsync()).LotNo);
        Assert.Equal(new[] { "LOT-2", "LOT-1" },
            (await t.Db.InventoryHistories.OrderBy(x => x.No).ToListAsync()).Select(x => x.LotNo));
    }

    private static PickingPlan Plan(
        string soNo = "SO-001", string serial = "01", decimal qty = 10, decimal picked = 0,
        int status = PickingStatus.NotPicked, string item = "IT-01", string? location = "W1_ZA") => new()
        {
            SoNo = soNo, SoBranchNo = serial, ItemCode = item, LocationCode = location,
            Qty = qty, PickedQty = picked, ScheduledDate = Today, Status = status,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), CreatedBy = "admin",
        };

    private static PickingPlanWriteDto Valid(
        string soNo = "SO-100", string item = "IT-01", decimal qty = 10) =>
        new(soNo, "01", item, "W1_ZA", qty, Today);


    [Fact]
    public async Task Create_records_the_signed_in_user_as_the_creator()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        ResultAssert.Created<ApiSuccess<PickingPlanDto>>(
            await new PickingService(t.Db).CreatePlanAsync(Valid(), Worker("U009"), default));

        Assert.Equal("U009", (await t.Db.PickingPlans.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task Create_stores_the_line_and_returns_it_shaped_like_the_grid()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new PickingService(t.Db).CreatePlanAsync(Valid(), Worker(), default);

        var dto = ResultAssert.Created<ApiSuccess<PickingPlanDto>>(r).Data;
        Assert.Equal("SO-100", dto.SoNo);
        Assert.Equal("01", dto.SerialNo);
        Assert.Equal("Industrial Printer 4in", dto.ItemName);
        Assert.Equal("W1_ZA", dto.LocationCode);
        Assert.Equal("pcs", dto.Unit);
        Assert.Equal("statusNotPicked", dto.Status);
        Assert.Equal(0, dto.PickedQty);
    }

    [Fact]
    public async Task Create_defaults_the_serial_number_to_01()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new PickingService(t.Db).CreatePlanAsync(Valid() with { SerialNo = null }, Worker(), default);

        Assert.Equal("01", ResultAssert.Created<ApiSuccess<PickingPlanDto>>(r).Data.SerialNo);
    }

    [Fact]
    public async Task Create_accepts_a_line_with_no_location_at_all()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new PickingService(t.Db).CreatePlanAsync(Valid() with { LocationCode = "" }, Worker(), default);

        Assert.Null(ResultAssert.Created<ApiSuccess<PickingPlanDto>>(r).Data.LocationCode);
        Assert.Null((await t.Db.PickingPlans.SingleAsync()).LocationCode);
    }

    [Fact]
    public async Task Create_rejects_a_quantity_below_one()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new PickingService(t.Db).CreatePlanAsync(Valid(qty: 0), Worker(), default);

        ResultAssert.BadRequest(r, "QTY-001");
    }

    [Fact]
    public async Task Create_rejects_an_unknown_item()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new PickingService(t.Db).CreatePlanAsync(Valid(item: "NOPE"), Worker(), default);

        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task Create_rejects_a_soft_deleted_location()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.Locations.Add(new Location
        {
            LocationCode = "W9_GONE", LocationName = "Gone",
            LocationType = LocationType.Large, LargeCode = "W9",
            IsActive = true, IsDeleted = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreatePlanAsync(Valid() with { LocationCode = "W9_GONE" }, Worker(), default);

        ResultAssert.NotFound(r);
    }


    [Fact]
    public async Task List_filters_by_status_using_the_api_label_key()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.AddRange(
            Plan("SO-001"),
            Plan("SO-002", status: PickingStatus.Picked),
            Plan("SO-003", status: PickingStatus.Cancelled));
        await t.Db.SaveChangesAsync();

        var picked = await new PickingService(t.Db).ListPlansAsync(default, status: "statusPicked");
        var junk = await new PickingService(t.Db).ListPlansAsync(default, status: "statusPartialPick");

        Assert.Equal(1, ResultAssert.Ok<ApiListSuccess<PickingPlanDto>>(picked).Meta.Total);
        Assert.Equal(3, ResultAssert.Ok<ApiListSuccess<PickingPlanDto>>(junk).Meta.Total);
    }

    [Fact]
    public async Task List_still_shows_cancelled_lines()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(status: PickingStatus.Cancelled));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).ListPlansAsync(default);

        var dto = Assert.Single(ResultAssert.Ok<ApiListSuccess<PickingPlanDto>>(r).Data);
        Assert.Equal("statusCancelled", dto.Status);
    }

    [Fact]
    public async Task List_filters_by_item_name_through_the_item_master()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-02", ItemName = "RFID Label UHF", ItemGroupCode = "IG-01",
            UomCode = "rolls", IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        t.Db.PickingPlans.AddRange(Plan("SO-001"), Plan("SO-002", item: "IT-02"));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).ListPlansAsync(default, itemName: "RFID");

        Assert.Equal("IT-02", Assert.Single(ResultAssert.Ok<ApiListSuccess<PickingPlanDto>>(r).Data).ItemCode);
    }

    [Fact]
    public async Task List_sorts_both_ways_and_falls_back_on_an_unknown_key()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.AddRange(Plan("SO-B"), Plan("SO-A"), Plan("SO-C"));
        await t.Db.SaveChangesAsync();

        var asc = await new PickingService(t.Db).ListPlansAsync(default, sort: "so_no");
        var desc = await new PickingService(t.Db).ListPlansAsync(default, sort: "-so_no");
        var junk = await new PickingService(t.Db).ListPlansAsync(default, sort: "nonsense");

        Assert.Equal("SO-A", ResultAssert.Ok<ApiListSuccess<PickingPlanDto>>(asc).Data[0].SoNo);
        Assert.Equal("SO-C", ResultAssert.Ok<ApiListSuccess<PickingPlanDto>>(desc).Data[0].SoNo);
        Assert.Equal(3, ResultAssert.Ok<ApiListSuccess<PickingPlanDto>>(junk).Meta.Total);
    }

    [Fact]
    public async Task List_normalises_an_out_of_range_page_size()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).ListPlansAsync(default, page: 0, pageSize: 500);

        var meta = ResultAssert.Ok<ApiListSuccess<PickingPlanDto>>(r).Meta;
        Assert.Equal(1, meta.Page);
        Assert.Equal(50, meta.PageSize);
    }


    [Fact]
    public async Task Status_change_marks_open_lines_picked()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan("SO-001"));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).ChangeStatusAsync(new PickingStatusChangeDto(new[] { "SO-001" }, "done"), Worker(), default);

        Assert.Equal(1, ResultAssert.Ok<ApiSuccess<PickingStatusChangeResult>>(r).Data.Updated);
        Assert.Equal(PickingStatus.Picked, (await t.Db.PickingPlans.SingleAsync()).Status);
    }

    [Fact]
    public async Task Status_change_skips_lines_that_are_already_finished_and_reports_them()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.AddRange(
            Plan("SO-001"),
            Plan("SO-002", status: PickingStatus.Picked),
            Plan("SO-003", status: PickingStatus.Cancelled));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).ChangeStatusAsync(new PickingStatusChangeDto(new[] { "SO-001", "SO-002", "SO-003" }, "cancelled"), Worker(), default);

        var res = ResultAssert.Ok<ApiSuccess<PickingStatusChangeResult>>(r).Data;
        Assert.Equal(1, res.Updated);
        Assert.Equal(new[] { "SO-002", "SO-003" }, res.Skipped.OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task Status_change_with_lines_changes_only_the_selected_serial()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.AddRange(
            Plan("SO-001", serial: "01"), Plan("SO-001", serial: "02"),
            Plan("SO-001", serial: "03", status: PickingStatus.Picked));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).ChangeStatusAsync(new PickingStatusChangeDto(null, "done",
            new[]
            {
                new PickingPlanKeyDto("SO-001", "02"), new PickingPlanKeyDto("SO-001", "03"),
                new PickingPlanKeyDto("SO-001", "09"),
            }), Worker(), default);

        var res = ResultAssert.Ok<ApiSuccess<PickingStatusChangeResult>>(r).Data;
        Assert.Equal(1, res.Updated);
        Assert.Equal(new[] { "SO-001/03", "SO-001/09" }, res.Skipped.OrderBy(x => x).ToArray());
        var rows = await t.Db.PickingPlans.AsNoTracking().ToDictionaryAsync(p => p.SoBranchNo, p => p.Status);
        Assert.Equal(PickingStatus.NotPicked, rows["01"]);
        Assert.Equal(PickingStatus.Picked, rows["02"]);
    }

    [Fact]
    public async Task Status_change_rejects_an_empty_selection_or_a_missing_status()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var noRows = await new PickingService(t.Db).ChangeStatusAsync(new PickingStatusChangeDto(Array.Empty<string>(), "done"), Worker(), default);
        var noStatus = await new PickingService(t.Db).ChangeStatusAsync(new PickingStatusChangeDto(new[] { "SO-001" }, null), Worker(), default);

        ResultAssert.BadRequest(noRows, "SEL-006");
        ResultAssert.BadRequest(noStatus, "SEL-006");
    }

    [Fact]
    public async Task Status_change_records_who_did_it()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan("SO-001"));
        await t.Db.SaveChangesAsync();

        await new PickingService(t.Db).ChangeStatusAsync(new PickingStatusChangeDto(new[] { "SO-001" }, "done"), Worker("U009"), default);

        Assert.Equal("U009", (await t.Db.PickingPlans.SingleAsync()).UpdatedBy);
    }


    [Fact]
    public async Task A_full_pick_closes_the_line()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default);

        var dto = ResultAssert.Created<ApiSuccess<PickingResultDto>>(r).Data;
        Assert.Equal(10, dto.Qty);
        var plan = await t.Db.PickingPlans.SingleAsync();
        Assert.Equal(PickingStatus.Picked, plan.Status);
        Assert.Equal(10, plan.PickedQty);
    }

    [Fact]
    public async Task A_short_pick_leaves_the_line_NOT_PICKED_and_asks_for_no_reason()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 6, "SO-001", "01"), Worker(), default);

        ResultAssert.Created<ApiSuccess<PickingResultDto>>(r);
        var plan = await t.Db.PickingPlans.SingleAsync();
        Assert.Equal(PickingStatus.NotPicked, plan.Status);
        Assert.Equal(6, plan.PickedQty);
    }

    [Fact]
    public async Task Picking_more_than_planned_is_refused()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 11, "SO-001", "01"), Worker(), default);

        ResultAssert.BadRequest(r, "QTY-002");
        Assert.Empty(await t.Db.PickingResults.ToListAsync());
    }

    [Fact]
    public async Task The_picked_quantity_overwrites_rather_than_accumulating()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(qty: 10, picked: 4));
        await t.Db.SaveChangesAsync();

        await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 6, "SO-001", "01"), Worker(), default);

        Assert.Equal(6, (await t.Db.PickingPlans.SingleAsync()).PickedQty);
    }

    [Fact]
    public async Task A_missing_quantity_is_not_the_same_as_zero()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", null, "SO-001", "01"), Worker(), default);

        ResultAssert.BadRequest(r, "QTY-001");
    }

    [Fact]
    public async Task A_result_without_a_slip_number_is_refused()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 5, null, null), Worker(), default);

        ResultAssert.BadRequest(r, "SEL-006");
    }

    [Fact]
    public async Task Scanning_the_wrong_item_finds_no_line()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-02", ItemName = "Other", ItemGroupCode = "IG-01", UomCode = "pcs",
            IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        t.Db.PickingPlans.Add(Plan(item: "IT-01"));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-02", 5, "SO-001", "01"), Worker(), default);

        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task Two_open_lines_and_no_serial_number_is_refused_rather_than_guessed()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.AddRange(Plan(serial: "01"), Plan(serial: "02"));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 5, "SO-001"), Worker(), default);

        ResultAssert.NotFound(r);
        Assert.Empty(await t.Db.PickingResults.ToListAsync());
    }

    [Fact]
    public async Task Confirming_an_already_picked_line_finds_nothing()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(status: PickingStatus.Picked));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 5, "SO-001", "01"), Worker(), default);

        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task The_result_takes_the_location_from_the_plan_when_none_is_sent()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(location: "W1_ZA"));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default);

        Assert.Equal("W1_ZA", ResultAssert.Created<ApiSuccess<PickingResultDto>>(r).Data.LocationCode);
    }

    [Fact]
    public async Task A_location_sent_by_the_scanner_wins_over_the_planned_one()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.Locations.Add(new Location
        {
            LocationCode = "W1_ZB", LocationName = "Zone B",
            LocationType = LocationType.Middle, LargeCode = "W1", MiddleCode = "ZB",
            IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        t.Db.PickingPlans.Add(Plan(location: "W1_ZA"));
        await t.Db.SaveChangesAsync();
        var seed = await t.Db.Stocks.SingleAsync(s => s.SerialNo == "SN-SEED");
        seed.LocationCode = "W1_ZB";
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01", "W1_ZB"), Worker(), default);

        Assert.Equal("W1_ZB", ResultAssert.Created<ApiSuccess<PickingResultDto>>(r).Data.LocationCode);
        Assert.Equal(90m, await SeedQtyAsync(t));
    }

    [Fact]
    public async Task A_plan_with_no_location_and_no_scanned_location_is_refused()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan(location: null));
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default);

        ResultAssert.BadRequest(r, "LOC-001");
    }

    [Fact]
    public async Task Result_records_the_worker_from_the_token()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker("U007"), default);

        Assert.Equal("U007", (await t.Db.PickingResults.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task A_lot_number_is_optional_here()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan());
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).CreateResultAsync(new PickingResultWriteDto("IT-01", 10, "SO-001", "01"), Worker(), default);

        ResultAssert.Created<ApiSuccess<PickingResultDto>>(r);
        Assert.Equal("LOT-1", (await t.Db.PickingResults.SingleAsync()).LotNo);
    }


    [Fact]
    public async Task ListResults_filters_by_date_range_inclusive_at_both_ends()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        var day = new DateOnly(2026, 3, 10);
        t.Db.PickingResults.AddRange(
            new PickingResult { SoNo = "SO-1", ItemCode = "IT-01", Qty = 1, LocationCode = "W1_ZA", CreatedAt = day.ToDateTime(new TimeOnly(0, 0)) },
            new PickingResult { SoNo = "SO-1", ItemCode = "IT-01", Qty = 1, LocationCode = "W1_ZA", CreatedAt = day.ToDateTime(new TimeOnly(23, 59)) },
            new PickingResult { SoNo = "SO-1", ItemCode = "IT-01", Qty = 1, LocationCode = "W1_ZA", CreatedAt = day.AddDays(1).ToDateTime(new TimeOnly(0, 1)) });
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).ListResultsAsync(default, dateFrom: day, dateTo: day);

        Assert.Equal(2, ResultAssert.Ok<ApiListSuccess<PickingResultDto>>(r).Meta.Total);
    }

    [Fact]
    public async Task ListResults_shows_the_scheduled_date_from_the_matching_plan()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingPlans.Add(Plan("SO-001"));
        t.Db.PickingResults.Add(new PickingResult
        {
            SoNo = "SO-001", SoBranchNo = "01", ItemCode = "IT-01", Qty = 5,
            LocationCode = "W1_ZA", CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var r = await new PickingService(t.Db).ListResultsAsync(default);

        Assert.Equal(Today, Assert.Single(ResultAssert.Ok<ApiListSuccess<PickingResultDto>>(r).Data).ScheduledDate);
    }


    [Fact]
    public async Task ExportPlans_writes_physical_column_names_and_escapes_commas()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-COMMA", ItemName = "Alpha, Beta & Co.", ItemGroupCode = "IG-01",
            UomCode = "pcs", IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        t.Db.PickingPlans.Add(Plan(item: "IT-COMMA"));
        await t.Db.SaveChangesAsync();

        var csv = await CsvReader.ReadAsync(await new PickingService(t.Db).ExportPlansCsvAsync(default));

        Assert.StartsWith("so_no,so_branch_no,item_code,item_name,location_code,", csv);
        Assert.Contains("\"Alpha, Beta & Co.\"", csv);
        Assert.Contains("statusNotPicked", csv);
    }

    [Fact]
    public async Task ExportResults_writes_physical_column_names()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.PickingResults.Add(new PickingResult
        {
            SoNo = "SO-001", SoBranchNo = "01", ItemCode = "IT-01", Qty = 4, LotNo = "LOT-1",
            LocationCode = "W1_ZA",
            CreatedAt = new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc), CreatedBy = "U007",
        });
        await t.Db.SaveChangesAsync();

        var csv = await CsvReader.ReadAsync(await new PickingService(t.Db).ExportResultsCsvAsync(default));

        Assert.StartsWith("no,created_at,item_code,item_name,qty,unit,so_no,so_branch_no,", csv);
        Assert.Contains("LOT-1", csv);
        Assert.Contains("W1_ZA", csv);
    }
}
