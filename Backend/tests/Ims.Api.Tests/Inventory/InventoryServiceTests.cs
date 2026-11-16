using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Inventory;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Production;
using Xunit;

namespace Ims.Api.Tests.Inventory;

public class InventoryServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 8, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private static readonly DateOnly NowDate = DateOnly.FromDateTime(Now);

    private static ClaimsPrincipal User(string code = "admin") =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, code) }, "test"));

    private static Task<TestDb> WithRowsAsync(params Stock[] rows) =>
        InventoryTestData.WithStockAsync(rows);

    private static async Task SeedRefsAsync(TestDb t) => await InventoryTestData.SeedRefsAsync(t);

    private static Stock Row(
        string item = "IT-01", string serial = "0000000001", decimal qty = 30,
        string? location = "W1_ZA", string? lot = "LOT260831",
        bool hold = false, string? reason = null, DateOnly? expiry = null,
        decimal reserved = 0) =>
        InventoryTestData.Stock(item, serial, qty, location, lot, hold, reason, expiry, reserved);

    [Fact]
    public async Task Stock_category_accepts_item_group_classification()
    {
        using var t = await WithRowsAsync(Row(), Row(item: "IT-02", serial: "0000000002"));
        t.Db.ItemGroups.Single(x => x.ItemGroupCode == "IG-01").Category = Ims.Domain.Masters.ItemGroupCategory.FinishedGoods;
        await t.Db.SaveChangesAsync();
        var rows = ResultAssert.Ok<ApiListSuccess<StockDto>>(
            await new InventoryService(t.Db).ListStockAsync(default, category: "RawMaterials")).Data;
        Assert.Equal(new[] { "IT-02" }, rows.Select(x => x.ItemCode));
    }

    [Fact]
    public async Task Work_history_pages_and_filters_derived_production_work()
    {
        using var t = await WithRowsAsync(Row());
        t.Db.ProductionOrders.Add(new ProductionOrder
        {
            ProductionOrderNo = "MO-H", FactoryCode = "FAC-01", OutputItemCode = "IT-01",
            StorageLocationCode = "W1_ZA", UomCode = "pcs", CreatedBy = "admin"
        });
        t.Db.Processes.Add(new Ims.Domain.Masters.Process
        {
            ProcessCode = "P1", ProcessName = "Process", CreatedAt = Now
        });
        t.Db.ProductionOrderLines.Add(new ProductionOrderLine
        {
            ProductionOrderNo = "MO-H", LineNo = 1, ProcessCode = "P1", ItemCode = "IT-02",
            UomCode = "rolls", PlannedQty = 7, PickedAt = Now, PickedBy = "admin", CreatedBy = "admin"
        });
        var orderH = t.Db.ProductionOrders.Local.Single(o => o.ProductionOrderNo == "MO-H");
        orderH.ReceivedAt = Now.AddMinutes(1);
        orderH.ReceivedBy = "admin";
        foreach (var n in new[] { 1, 2 })
            t.Db.ProductionResults.Add(new ProductionResult
            {
                ResultType = ProductionResultType.Output, ProductionOrderNo = "MO-H", RequestId = "O-H", SerialNo = $"PRD-H-{n}",
                ItemCode = "IT-01", LotNo = "FG-H", Qty = 1, LocationCode = "W1_ZA", FactoryCode = "FAC-01",
                LabelIssuedAt = Now.AddMinutes(2), LabelIssuedBy = "admin", CreatedAt = Now.AddMinutes(2), CreatedBy = "admin"
            });
        await t.Db.SaveChangesAsync();
        var service = new InventoryService(t.Db);
        var page = ResultAssert.Ok<ApiListSuccess<InventoryHistoryDto>>(
            await service.ListHistoryAsync(default, page: 1, pageSize: 20, sort: "-created_at"));
        Assert.Equal(3, page.Meta.Total);
        Assert.Equal(InventoryAction.ProdLabel, page.Data.First().ActionType);
        var picked = ResultAssert.Ok<ApiListSuccess<InventoryHistoryDto>>(
            await service.ListHistoryAsync(default, action: InventoryAction.ProdPick));
        Assert.True(InventoryAction.IsDefined(InventoryAction.ProdPick));
        Assert.Equal(7, Assert.Single(picked.Data).Qty);
    }


    [Fact]
    public async Task The_grid_row_joins_in_everything_the_demo_shows_beside_the_quantity()
    {
        using var t = await WithRowsAsync(Row());

        var r = await new InventoryService(t.Db).ListStockAsync(default);

        var row = ResultAssert.Ok<ApiListSuccess<StockDto>>(r).Data.Single();
        Assert.Equal("IT-01", row.ItemCode);
        Assert.Equal("Industrial Printer 4in", row.ItemName);
        Assert.Equal("Group 1", row.Category);
        Assert.Equal("pcs", row.Unit);
        Assert.Equal("W1", row.Warehouse);
        Assert.Equal("W1_ZA", row.LocationCode);
        Assert.Equal("LOT260831", row.LotNo);
        Assert.Equal(30m, row.StockQty);
        Assert.Equal(10m, row.ReorderPoint);
        Assert.Equal(20m, row.OverShort);
        Assert.False(row.IsOnHold);
        Assert.Equal(60m, row.SurplusPoint);
    }

    [Fact]
    public async Task A_row_with_no_location_still_lists_rather_than_disappearing()
    {
        using var t = await WithRowsAsync(Row(location: null));

        var r = await new InventoryService(t.Db).ListStockAsync(default);

        var row = ResultAssert.Ok<ApiListSuccess<StockDto>>(r).Data.Single();
        Assert.Null(row.LocationCode);
        Assert.Null(row.Warehouse);
    }

    [Fact]
    public async Task Remaining_days_counts_from_today_and_is_null_without_an_expiry()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", expiry: Today.AddDays(10)),
            Row(serial: "0000000002", expiry: null));

        var r = await new InventoryService(t.Db).ListStockAsync(default, sort: "serial_no");

        var rows = ResultAssert.Ok<ApiListSuccess<StockDto>>(r).Data;
        Assert.Equal(10, rows[0].RemainingDays);
        Assert.Null(rows[1].RemainingDays);
    }

    [Fact]
    public async Task The_expiration_filter_splits_expired_from_expiring_soon_at_thirty_days()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", expiry: Today.AddDays(-1)),
            Row(serial: "0000000002", expiry: Today.AddDays(30)),
            Row(serial: "0000000003", expiry: Today.AddDays(31)),
            Row(serial: "0000000004", expiry: null));

        async Task<string[]> Serials(string filter)
        {
            var r = await new InventoryService(t.Db).ListStockAsync(default, expiration: filter, sort: "serial_no");
            return ResultAssert.Ok<ApiListSuccess<StockDto>>(r).Data.Select(x => x.SerialNo).ToArray();
        }

        Assert.Equal(new[] { "0000000001" }, await Serials("expired"));
        Assert.Equal(new[] { "0000000002" }, await Serials("expiringSoon"));
        Assert.Equal(4, (await Serials("all")).Length);
    }

    [Fact]
    public async Task The_stock_level_filter_reads_the_item_master_thresholds()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", qty: 9),
            Row(serial: "0000000002", qty: 30),
            Row(serial: "0000000003", qty: 60));

        async Task<string[]> Serials(string filter)
        {
            var r = await new InventoryService(t.Db).ListStockAsync(default, stockLevel: filter, sort: "serial_no");
            return ResultAssert.Ok<ApiListSuccess<StockDto>>(r).Data.Select(x => x.SerialNo).ToArray();
        }

        Assert.Equal(new[] { "0000000001" }, await Serials("low"));
        Assert.Equal(new[] { "0000000003" }, await Serials("overflow"));
    }

    [Fact]
    public async Task Warehouse_item_category_lot_and_hold_each_narrow_the_grid()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", item: "IT-01", location: "W1_ZA", lot: "L-A"),
            Row(serial: "0000000002", item: "IT-02", location: "W2_ZB", lot: "L-B", hold: true, reason: "RS-QC"));

        async Task<int> Count(
            string? warehouse = null, string? itemCode = null, string? itemName = null,
            string? category = null, string? lot = null, string? hold = null)
        {
            var r = await new InventoryService(t.Db).ListStockAsync(default, warehouse: warehouse, itemCode: itemCode, itemName: itemName, category: category, lot: lot, hold: hold);
            return ResultAssert.Ok<ApiListSuccess<StockDto>>(r).Data.Count;
        }

        Assert.Equal(1, await Count(warehouse: "W2"));
        Assert.Equal(1, await Count(itemCode: "IT-01"));
        Assert.Equal(1, await Count(itemName: "Thermal"));
        Assert.Equal(1, await Count(category: "IG-LBL"));
        Assert.Equal(1, await Count(lot: "L-A"));
        Assert.Equal(1, await Count(hold: "hold"));
        Assert.Equal(1, await Count(hold: "notHold"));
        Assert.Equal(2, await Count(hold: "all"));
    }

    [Fact]
    public async Task Grouping_by_item_sums_the_lots_and_counts_them()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", item: "IT-01", qty: 30, lot: "L-A"),
            Row(serial: "0000000002", item: "IT-01", qty: 10, lot: "L-B"),
            Row(serial: "0000000003", item: "IT-02", qty: 500, lot: "L-C"));

        var r = await new InventoryService(t.Db).ListStockAsync(default, groupBy: "item");

        var rows = ResultAssert.Ok<ApiListSuccess<StockGroupDto>>(r).Data;
        var it01 = rows.Single(x => x.Key == "IT-01");
        Assert.Equal(40m, it01.StockQty);
        Assert.Equal(2, it01.LotCount);
        Assert.Equal("Industrial Printer 4in", it01.Name);
        Assert.Equal("pcs", it01.Unit);
    }

    [Fact]
    public async Task Grouping_by_item_reports_the_WORST_lot_not_an_average()
    {
        var expired = DateOnly.FromDateTime(DateTime.Today.AddDays(-3));
        var far = DateOnly.FromDateTime(DateTime.Today.AddDays(400));
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", item: "IT-01", qty: 30, lot: "L-A", expiry: far),
            Row(serial: "0000000002", item: "IT-01", qty: 10, lot: "L-B", hold: true, expiry: expired));

        var r = await new InventoryService(t.Db).ListStockAsync(default, groupBy: "item");

        var row = ResultAssert.Ok<ApiListSuccess<StockGroupDto>>(r).Data.Single();
        Assert.Equal(40m, row.StockQty);
        Assert.True(row.AnyOnHold);
        Assert.Equal(expired, row.EarliestExpiry);
        Assert.Equal(-3, row.RemainingDays);
        Assert.Equal(10m, row.ReorderPoint);
    }

    [Fact]
    public async Task Grouping_by_item_leaves_the_flags_clear_when_every_lot_is_healthy()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", item: "IT-01", qty: 30, lot: "L-A"),
            Row(serial: "0000000002", item: "IT-01", qty: 10, lot: "L-B"));

        var r = await new InventoryService(t.Db).ListStockAsync(default, groupBy: "item");

        var row = ResultAssert.Ok<ApiListSuccess<StockGroupDto>>(r).Data.Single();
        Assert.False(row.AnyOnHold);
        Assert.Null(row.EarliestExpiry);
        Assert.Null(row.RemainingDays);
    }

    [Fact]
    public async Task Grouping_by_location_counts_distinct_items_not_rows()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", item: "IT-01", qty: 30, location: "W1_ZA", lot: "L-A"),
            Row(serial: "0000000002", item: "IT-01", qty: 10, location: "W1_ZA", lot: "L-B"),
            Row(serial: "0000000003", item: "IT-02", qty: 5, location: "W1_ZA", lot: "L-C"));

        var r = await new InventoryService(t.Db).ListStockAsync(default, groupBy: "location");

        var row = ResultAssert.Ok<ApiListSuccess<StockGroupDto>>(r).Data.Single();
        Assert.Equal("W1_ZA", row.Key);
        Assert.Equal("W1", row.Warehouse);
        Assert.Equal(2, row.ItemCount);
        Assert.Equal(45m, row.StockQty);
    }

    [Fact]
    public async Task The_export_carries_the_grid_columns_and_the_filters_still_apply()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", item: "IT-01"),
            Row(serial: "0000000002", item: "IT-02", location: "W2_ZB"));

        var r = await new InventoryService(t.Db).ExportStockCsvAsync(default, warehouse: "W2");
        var csv = await CsvReader.ReadAsync(r);

        Assert.Contains("item_code,item_name,category,warehouse,location_code", csv);
        Assert.Contains("IT-02", csv);
        Assert.DoesNotContain("IT-01", csv);
    }


    [Fact]
    public async Task An_adjustment_records_the_CHANGE_not_the_new_total()
    {
        using var t = await WithRowsAsync(Row(qty: 30));

        var r = await new InventoryService(t.Db).AdjustStockAsync(new StockAdjustDto("IT-01", "0000000001", 12, "RS-ADJ"), User(), default);

        ResultAssert.Ok<ApiSuccess<StockDto>>(r);
        var stock = await t.Db.Stocks.SingleAsync();
        Assert.Equal(12m, stock.StockQty);
        Assert.Equal(12m, stock.AvailableQty);

        var h = await t.Db.InventoryHistories.SingleAsync();
        Assert.Equal(InventoryAction.Adjust, h.ActionType);
        Assert.Equal(-18m, h.Qty);
        Assert.Equal("RS-ADJ", h.ReasonCode);
        Assert.Equal("admin", h.CreatedBy);
    }

    [Fact]
    public async Task An_adjustment_that_changes_nothing_still_writes_no_history()
    {
        using var t = await WithRowsAsync(Row(qty: 30));

        var r = await new InventoryService(t.Db).AdjustStockAsync(new StockAdjustDto("IT-01", "0000000001", 30, "RS-ADJ"), User(), default);

        ResultAssert.Ok<ApiSuccess<StockDto>>(r);
        Assert.False(await t.Db.InventoryHistories.AnyAsync());
    }

    [Fact]
    public async Task An_adjustment_without_a_reason_is_refused()
    {
        using var t = await WithRowsAsync(Row());

        var r = await new InventoryService(t.Db).AdjustStockAsync(new StockAdjustDto("IT-01", "0000000001", 12, null), User(), default);

        ResultAssert.BadRequest(r, "RSN-001");
    }

    [Fact]
    public async Task An_adjustment_to_a_negative_quantity_is_refused()
    {
        using var t = await WithRowsAsync(Row());

        var r = await new InventoryService(t.Db).AdjustStockAsync(new StockAdjustDto("IT-01", "0000000001", -1, "RS-ADJ"), User(), default);

        ResultAssert.BadRequest(r, "QTY-001");
    }

    [Fact]
    public async Task Adjusting_a_row_that_does_not_exist_is_a_404()
    {
        using var t = await WithRowsAsync(Row());

        var r = await new InventoryService(t.Db).AdjustStockAsync(new StockAdjustDto("IT-01", "9999999999", 12, "RS-ADJ"), User(), default);

        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task An_adjustment_keeps_reserved_stock_out_of_the_available_figure()
    {
        using var t = await WithRowsAsync(Row(qty: 30, reserved: 5));

        await new InventoryService(t.Db).AdjustStockAsync(new StockAdjustDto("IT-01", "0000000001", 20, "RS-ADJ"), User(), default);

        var stock = await t.Db.Stocks.SingleAsync();
        Assert.Equal(15m, stock.AvailableQty);
    }


    [Fact]
    public async Task Holding_stock_zeroes_the_available_quantity_without_touching_the_shelf()
    {
        using var t = await WithRowsAsync(Row(qty: 30));

        var r = await new InventoryService(t.Db).HoldStockAsync(new StockHoldDto([new StockKeyDto("IT-01", "0000000001")], "RS-QC"), User(), default);

        var res = ResultAssert.Ok<ApiSuccess<StockBulkResult>>(r).Data;
        Assert.Equal(1, res.Updated);
        Assert.Empty(res.Skipped);

        var stock = await t.Db.Stocks.SingleAsync();
        Assert.True(stock.IsOnHold);
        Assert.Equal("RS-QC", stock.ReasonCode);
        Assert.Equal(30m, stock.StockQty);
        Assert.Equal(0m, stock.AvailableQty);

        var h = await t.Db.InventoryHistories.SingleAsync();
        Assert.Equal(InventoryAction.Hold, h.ActionType);
        Assert.Null(h.Qty);
    }

    [Fact]
    public async Task Holding_without_a_reason_is_refused()
    {
        using var t = await WithRowsAsync(Row());

        var r = await new InventoryService(t.Db).HoldStockAsync(new StockHoldDto([new StockKeyDto("IT-01", "0000000001")], null), User(), default);

        ResultAssert.BadRequest(r, "RSN-001");
    }

    [Fact]
    public async Task Holding_nothing_is_refused_rather_than_silently_doing_nothing()
    {
        using var t = await WithRowsAsync(Row());

        var r = await new InventoryService(t.Db).HoldStockAsync(new StockHoldDto([], "RS-QC"), User(), default);

        ResultAssert.BadRequest(r, "SEL-004");
    }

    [Fact]
    public async Task A_row_already_on_hold_is_skipped_and_named_in_the_reply()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", hold: true, reason: "RS-QC"),
            Row(serial: "0000000002"));

        var r = await new InventoryService(t.Db).HoldStockAsync(new StockHoldDto(
                [new StockKeyDto("IT-01", "0000000001"), new StockKeyDto("IT-01", "0000000002")],
                "RS-QC"), User(), default);

        var res = ResultAssert.Ok<ApiSuccess<StockBulkResult>>(r).Data;
        Assert.Equal(1, res.Updated);
        Assert.Equal(new[] { "IT-01/0000000001" }, res.Skipped);
        Assert.Equal(1, await t.Db.InventoryHistories.CountAsync());
    }

    [Fact]
    public async Task Releasing_a_hold_clears_the_reason_and_gives_the_stock_back()
    {
        using var t = await WithRowsAsync(Row(qty: 30, reserved: 5, hold: true, reason: "RS-QC"));

        var r = await new InventoryService(t.Db).ReleaseHoldAsync(new StockKeysDto([new StockKeyDto("IT-01", "0000000001")]), User(), default);

        var res = ResultAssert.Ok<ApiSuccess<StockBulkResult>>(r).Data;
        Assert.Equal(1, res.Updated);

        var stock = await t.Db.Stocks.SingleAsync();
        Assert.False(stock.IsOnHold);
        Assert.Null(stock.ReasonCode);
        Assert.Equal(25m, stock.AvailableQty);

        var h = await t.Db.InventoryHistories.SingleAsync();
        Assert.Equal(InventoryAction.HoldRelease, h.ActionType);
    }

    [Fact]
    public async Task Releasing_a_row_that_is_not_on_hold_is_skipped()
    {
        using var t = await WithRowsAsync(Row());

        var r = await new InventoryService(t.Db).ReleaseHoldAsync(new StockKeysDto([new StockKeyDto("IT-01", "0000000001")]), User(), default);

        var res = ResultAssert.Ok<ApiSuccess<StockBulkResult>>(r).Data;
        Assert.Equal(0, res.Updated);
        Assert.Single(res.Skipped);
        Assert.False(await t.Db.InventoryHistories.AnyAsync());
    }


    [Fact]
    public async Task Disposal_empties_the_row_and_records_the_whole_quantity_as_a_loss()
    {
        using var t = await WithRowsAsync(Row(qty: 8, lot: "LOT260809", location: "W2_ZB"));

        var r = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", "RS-QC"), User(), default);

        ResultAssert.Ok<ApiSuccess<StockDto>>(r);
        var stock = await t.Db.Stocks.SingleAsync();
        Assert.Equal(0m, stock.StockQty);
        Assert.Equal(0m, stock.AvailableQty);

        var h = await t.Db.InventoryHistories.SingleAsync();
        Assert.Equal(InventoryAction.Disposal, h.ActionType);
        Assert.Equal(-8m, h.Qty);
        Assert.Equal("LOT260809", h.LotNo);
        Assert.Equal("W2_ZB", h.LocationCode);
    }

    [Fact]
    public async Task Disposing_of_an_empty_row_is_refused()
    {
        using var t = await WithRowsAsync(Row(qty: 0));

        var r = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", "RS-QC"), User(), default);

        ResultAssert.Error(r, 409, "INV-009");
    }

    [Fact]
    public async Task Disposal_of_a_quantity_scraps_only_that_much_and_leaves_the_rest()
    {
        using var t = await WithRowsAsync(Row(qty: 50));

        var r = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", "RS-QC", Qty: 3), User(), default);

        ResultAssert.Ok<ApiSuccess<StockDto>>(r);
        Assert.Equal(47m, (await t.Db.Stocks.SingleAsync()).StockQty);
        var h = await t.Db.InventoryHistories.SingleAsync();
        Assert.Equal(-3m, h.Qty);
        Assert.Equal(InventoryAction.Disposal, h.ActionType);
    }

    [Fact]
    public async Task Disposal_without_a_quantity_still_empties_the_row()
    {
        using var t = await WithRowsAsync(Row(qty: 50));

        var r = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", "RS-QC"), User(), default);

        ResultAssert.Ok<ApiSuccess<StockDto>>(r);
        Assert.Equal(0m, (await t.Db.Stocks.SingleAsync()).StockQty);
        Assert.Equal(-50m, (await t.Db.InventoryHistories.SingleAsync()).Qty);
    }

    [Fact]
    public async Task Disposal_of_more_than_is_on_the_shelf_is_refused()
    {
        using var t = await WithRowsAsync(Row(qty: 5));

        var r = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", "RS-QC", Qty: 6), User(), default);

        ResultAssert.Error(r, 409, "INV-009");
        Assert.Equal(5m, (await t.Db.Stocks.SingleAsync()).StockQty);
    }

    [Fact]
    public async Task Disposal_of_nothing_is_refused_rather_than_written_as_a_zero_row()
    {
        using var t = await WithRowsAsync(Row(qty: 5));

        var r = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", "RS-QC", Qty: 0), User(), default);

        ResultAssert.BadRequest(r, "QTY-003");
        Assert.Empty(t.Db.InventoryHistories);
    }

    [Fact]
    public async Task Disposal_of_held_stock_is_HLD_001()
    {
        using var t = await WithRowsAsync(Row(qty: 38, hold: true, reason: "RS-QC"));

        var r = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", "RS-QC", Qty: 1), User(), default);

        ResultAssert.Error(r, 409, "HLD-001");
        Assert.Empty(t.Db.InventoryHistories);
    }

    [Fact]
    public async Task Disposal_above_the_available_quantity_is_INV_009()
    {
        using var t = await WithRowsAsync(Row(qty: 38, reserved: 30));

        var r = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", "RS-QC", Qty: 9), User(), default);

        ResultAssert.Error(r, 409, "INV-009");
        Assert.Equal(38m, (await t.Db.Stocks.SingleAsync()).StockQty);
    }

    [Fact]
    public async Task Disposal_without_a_reason_is_refused()
    {
        using var t = await WithRowsAsync(Row());

        var r = await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", null), User(), default);

        ResultAssert.BadRequest(r, "RSN-001");
    }


    private static InventoryHistory Hist(
        string action, decimal? qty, string item = "IT-01",
        DateTime? at = null, string? reason = null, string? location = "W1_ZA") => new()
        {
            CreatedAt = at ?? Now, ItemCode = item, FactoryCode = "FAC-01",
            LocationCode = location, LotNo = "LOT260831",
            ActionType = action, Qty = qty, ReasonCode = reason, CreatedBy = "admin",
        };

    [Fact]
    public async Task The_history_row_carries_the_label_key_rather_than_translated_text()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.InventoryHistories.Add(Hist(InventoryAction.HoldRelease, null));
        await t.Db.SaveChangesAsync();

        var r = await new InventoryService(t.Db).ListHistoryAsync(default);

        var row = ResultAssert.Ok<ApiListSuccess<InventoryHistoryDto>>(r).Data.Single();
        Assert.Equal("actionHoldRelease", row.ActionLabelKey);
        Assert.Equal("HoldRelease", row.ActionType);
        Assert.Equal("Industrial Printer 4in", row.ItemName);
    }

    [Fact]
    public async Task The_history_filters_by_action_by_date_and_by_free_text()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.InventoryHistories.AddRange(
            Hist(InventoryAction.Receiving, 20, at: Now.AddDays(-5)),
            Hist(InventoryAction.Disposal, -8, item: "IT-02", at: Now));
        await t.Db.SaveChangesAsync();

        async Task<int> Count(string? action = null, DateOnly? from = null, string? q = null)
        {
            var r = await new InventoryService(t.Db).ListHistoryAsync(default, action: action, dateFrom: from, q: q);
            return ResultAssert.Ok<ApiListSuccess<InventoryHistoryDto>>(r).Data.Count;
        }

        Assert.Equal(1, await Count(action: "Disposal"));
        Assert.Equal(1, await Count(from: NowDate));
        Assert.Equal(1, await Count(q: "IT-02"));
        Assert.Equal(2, await Count());
    }

    [Fact]
    public async Task An_unknown_action_filter_is_ignored_rather_than_emptying_the_grid()
    {
        using var t = TestDb.Seeded(); await SeedRefsAsync(t);
        t.Db.InventoryHistories.Add(Hist(InventoryAction.Receiving, 20));
        await t.Db.SaveChangesAsync();

        var r = await new InventoryService(t.Db).ListHistoryAsync(default, action: "Teleport");

        Assert.Single(ResultAssert.Ok<ApiListSuccess<InventoryHistoryDto>>(r).Data);
    }


    [Fact]
    public async Task Restoring_a_disposal_adds_a_NEW_RestoreDisposal_row_and_leaves_the_disposal_intact()
    {
        using var t = await WithRowsAsync(Row(qty: 0));
        t.Db.InventoryHistories.Add(Hist(InventoryAction.Disposal, -8, reason: "RS-QC"));
        await t.Db.SaveChangesAsync();
        var disposalNo = await t.Db.InventoryHistories.Select(x => x.No).SingleAsync();
        t.Db.ChangeTracker.Clear();

        var r = await new InventoryService(t.Db).RestoreDisposalAsync(new RestoreDisposalDto([disposalNo]), User(), default);

        var res = ResultAssert.Ok<ApiSuccess<StockBulkResult>>(r).Data;
        Assert.Equal(1, res.Updated);

        var stock = await t.Db.Stocks.SingleAsync();
        Assert.Equal(8m, stock.StockQty);

        var rows = await t.Db.InventoryHistories.OrderBy(x => x.No).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal(InventoryAction.Disposal, rows[0].ActionType);
        Assert.Equal(InventoryAction.RestoreDisposal, rows[1].ActionType);
        Assert.Equal(8m, rows[1].Qty);
    }

    [Fact]
    public async Task Restoring_something_that_was_never_disposed_of_is_skipped()
    {
        using var t = await WithRowsAsync(Row(qty: 30));
        t.Db.InventoryHistories.Add(Hist(InventoryAction.Receiving, 20));
        await t.Db.SaveChangesAsync();
        var no = await t.Db.InventoryHistories.Select(x => x.No).SingleAsync();
        t.Db.ChangeTracker.Clear();

        var r = await new InventoryService(t.Db).RestoreDisposalAsync(new RestoreDisposalDto([no]), User(), default);

        var res = ResultAssert.Ok<ApiSuccess<StockBulkResult>>(r).Data;
        Assert.Equal(0, res.Updated);
        Assert.Single(res.Skipped);
        Assert.Equal(30m, (await t.Db.Stocks.SingleAsync()).StockQty);
    }

    [Fact]
    public async Task Restoring_a_disposal_twice_only_puts_the_stock_back_once()
    {
        using var t = await WithRowsAsync(Row(qty: 0));
        t.Db.InventoryHistories.Add(Hist(InventoryAction.Disposal, -8, reason: "RS-QC"));
        await t.Db.SaveChangesAsync();
        var no = await t.Db.InventoryHistories.Select(x => x.No).SingleAsync();
        t.Db.ChangeTracker.Clear();

        await new InventoryService(t.Db).RestoreDisposalAsync(new RestoreDisposalDto([no]), User(), default);
        t.Db.ChangeTracker.Clear();
        var second = await new InventoryService(t.Db).RestoreDisposalAsync(new RestoreDisposalDto([no]), User(), default);

        var res = ResultAssert.Ok<ApiSuccess<StockBulkResult>>(second).Data;
        Assert.Equal(0, res.Updated);
        Assert.Equal(8m, (await t.Db.Stocks.SingleAsync()).StockQty);
    }


    [Fact]
    public async Task Paging_and_sorting_follow_the_same_rules_as_every_other_grid()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", qty: 30),
            Row(serial: "0000000002", qty: 10),
            Row(serial: "0000000003", qty: 20));

        var r = await new InventoryService(t.Db).ListStockAsync(default, sort: "-stock_qty", page: 1, pageSize: 20);
        var ok = ResultAssert.Ok<ApiListSuccess<StockDto>>(r);
        Assert.Equal(new[] { 30m, 20m, 10m }, ok.Data.Select(x => x.StockQty));
        Assert.Equal(3, ok.Meta.Total);

        var r2 = await new InventoryService(t.Db).ListStockAsync(default, pageSize: 7);
        Assert.Equal(50, ResultAssert.Ok<ApiListSuccess<StockDto>>(r2).Meta.PageSize);
    }

    [Fact]
    public async Task Adjust_below_the_reserved_quantity_is_refused()
    {
        using var t = await WithRowsAsync(Row(qty: 30, reserved: 10));

        var r = await new InventoryService(t.Db).AdjustStockAsync(new StockAdjustDto("IT-01", "0000000001", 5, "RS-ADJ"), User(), default);

        ResultAssert.BadRequest(r, "QTY-001");
        Assert.Equal(30m, (await t.Db.Stocks.AsNoTracking().SingleAsync()).StockQty);
    }

    [Fact]
    public async Task Adjust_with_a_reason_outside_the_Adjustment_category_is_refused()
    {
        using var t = await WithRowsAsync(Row(qty: 30));

        var r = await new InventoryService(t.Db).AdjustStockAsync(new StockAdjustDto("IT-01", "0000000001", 12, "RS-QC"), User(), default);

        ResultAssert.BadRequest(r, "RSN-001");
        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
    }

    [Fact]
    public async Task Restore_puts_the_goods_back_on_the_serial_they_were_disposed_from()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", qty: 10),
            Row(serial: "0000000002", qty: 10));
        await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000002", "RS-QC", 4), User(), default);
        var no = await t.Db.InventoryHistories.Select(x => x.No).SingleAsync();
        t.Db.ChangeTracker.Clear();

        var r = await new InventoryService(t.Db).RestoreDisposalAsync(new RestoreDisposalDto([no]), User(), default);

        Assert.Equal(1, ResultAssert.Ok<ApiSuccess<StockBulkResult>>(r).Data.Updated);
        var rows = await t.Db.Stocks.AsNoTracking().OrderBy(s => s.SerialNo).ToListAsync();
        Assert.Equal(10m, rows[0].StockQty);
        Assert.Equal(10m, rows[1].StockQty);
    }

    [Fact]
    public async Task An_unrelated_adjust_of_the_same_quantity_does_not_count_as_a_restore()
    {
        using var t = await WithRowsAsync(Row(serial: "0000000001", qty: 10));
        await new InventoryService(t.Db).DisposeStockAsync(new StockDisposalDto("IT-01", "0000000001", "RS-QC", 4), User(), default);
        var no = await t.Db.InventoryHistories.Select(x => x.No).SingleAsync();
        t.Db.ChangeTracker.Clear();
        await new InventoryService(t.Db).AdjustStockAsync(new StockAdjustDto("IT-01", "0000000001", 10, "RS-ADJ"), User(), default);
        t.Db.ChangeTracker.Clear();

        var r = await new InventoryService(t.Db).RestoreDisposalAsync(new RestoreDisposalDto([no]), User(), default);

        Assert.Equal(1, ResultAssert.Ok<ApiSuccess<StockBulkResult>>(r).Data.Updated);
        Assert.Equal(14m, (await t.Db.Stocks.AsNoTracking().SingleAsync()).StockQty);
    }

    [Fact]
    public async Task The_lot_count_counts_distinct_lots_not_stock_rows()
    {
        using var t = await WithRowsAsync(
            Row(serial: "0000000001", item: "IT-01", qty: 1, lot: "L-A"),
            Row(serial: "0000000002", item: "IT-01", qty: 1, lot: "L-A"),
            Row(serial: "0000000003", item: "IT-01", qty: 1, lot: "L-B"),
            Row(serial: "0000000004", item: "IT-01", qty: 1, lot: null),
            Row(serial: "0000000005", item: "IT-01", qty: 1, lot: null));

        var byItem = ResultAssert.Ok<ApiListSuccess<StockGroupDto>>(
            await new InventoryService(t.Db).ListStockAsync(default, groupBy: "item")).Data.Single();
        var byLocation = ResultAssert.Ok<ApiListSuccess<StockGroupDto>>(
            await new InventoryService(t.Db).ListStockAsync(default, groupBy: "location")).Data.Single();

        Assert.Equal(3, byItem.LotCount);
        Assert.Equal(3, byLocation.LotCount);
    }
}
