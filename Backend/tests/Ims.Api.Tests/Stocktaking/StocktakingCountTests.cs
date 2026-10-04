using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Stocktaking;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Stocktaking;
using Xunit;
using static Ims.Api.Tests.Stocktaking.StocktakingSessionTests;

namespace Ims.Api.Tests.Stocktaking;

public class StocktakingCountTests
{
    private static async Task<StocktakingGroupDto> Count(
        TestDb t, string loc, string item, string? lot, int counted)
    {
        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto(loc, item, lot, counted), User(), default);
        return ResultAssert.Ok<ApiSuccess<StocktakingGroupDto>>(res).Data!;
    }


    [Fact]
    public async Task Counting_before_the_pc_starts_the_session_is_refused()
    {
        using var t = Warehouse();
        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto("W1_ZA_S1", "ITM-A", "LOT-1", 1), User(), default);
        ResultAssert.Error(res, 409, "STK-001");
    }

    [Fact]
    public async Task Counting_after_complete_is_refused()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);

        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto("W1_ZA_S1", "ITM-A", "LOT-1", 1), User(), default);
        ResultAssert.Error(res, 409, "STK-004");
    }

    [Fact]
    public async Task A_negative_count_is_refused()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto("W1_ZA_S1", "ITM-A", "LOT-1", -1), User(), default);
        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Fact]
    public async Task Finding_nothing_is_a_result_not_an_error()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var g = await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 0);
        Assert.Equal(0, g.ScannedLabels);
        Assert.Equal(2, g.PlannedLabels);
    }

    [Fact]
    public async Task An_unknown_location_is_refused()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto("W1_ZA_S9", "ITM-A", "LOT-1", 1), User(), default);
        ResultAssert.Error(res, 404, "LOC-003");
    }


    [Fact]
    public async Task Counting_marks_exactly_that_many_labels()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var g = await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 1);
        Assert.Equal(2, g.PlannedLabels);
        Assert.Equal(1, g.ScannedLabels);
        Assert.Equal(20m, g.PlannedQty);
        Assert.Equal(10m, g.ResultQty);

        Assert.Equal(1, await t.Db.StocktakingLabels.CountAsync(x => x.IsScanned));
    }

    [Fact]
    public async Task A_smaller_recount_takes_labels_back_off()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 2);

        var g = await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 1);
        Assert.Equal(1, g.ScannedLabels);
        Assert.Equal(1, await t.Db.StocktakingLabels.CountAsync(x => x.IsScanned));
    }

    [Fact]
    public async Task A_counted_label_is_marked_normal()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await Count(t, "W1_ZA_S2", "ITM-B", "LOT-2", 1);

        var row = await t.Db.StocktakingLabels.SingleAsync(x => x.SerialNo == "SN-003");
        Assert.True(row.IsScanned);
        Assert.Equal(StocktakingLabelStatus.Normal, row.Status);
        Assert.NotNull(row.UpdatedAt);
    }

    [Fact]
    public async Task Counting_more_than_the_books_say_is_refused()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto("W1_ZA_S2", "ITM-B", "LOT-2", 3), User(), default);
        ResultAssert.BadRequest(res, "QTY-002");

        Assert.Equal(0, await t.Db.StocktakingLabels.CountAsync(x => x.IsScanned));
        Assert.Equal(0, await t.Db.StocktakingLabels
            .CountAsync(x => x.Status == StocktakingLabelStatus.Over));
    }

    [Fact]
    public async Task Counting_exactly_what_the_books_say_is_accepted()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var g = await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 2);
        Assert.Equal(2, g.PlannedLabels);
        Assert.Equal(2, g.ScannedLabels);
    }

    [Fact]
    public async Task A_count_is_confined_to_the_shelf_it_was_made_at()
    {
        using var t = Warehouse();
        await StartAsync(t);
        var moved = await t.Db.StocktakingLabels.SingleAsync(x => x.SerialNo == "SN-002");
        moved.LocationCode = "W1_ZA_S2";
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 1);

        Assert.True(await t.Db.StocktakingLabels.AnyAsync(x => x.SerialNo == "SN-001" && x.IsScanned));
        Assert.False(await t.Db.StocktakingLabels.AnyAsync(x => x.SerialNo == "SN-002" && x.IsScanned));
    }


    [Fact]
    public async Task An_empty_shelf_is_reported_as_such()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).ListItemsAtLocationAsync(default, "W1_ZA");
        ResultAssert.Error(res, 404, "LOC-003");
    }

    [Fact]
    public async Task Listing_a_shelf_without_a_session_is_refused()
    {
        using var t = Warehouse();
        var res = await new StocktakingService(t.Db).ListItemsAtLocationAsync(default, "W1_ZA_S1");
        ResultAssert.Error(res, 409, "STK-001");
    }

    [Fact]
    public async Task A_shelf_lists_one_row_per_item_and_lot()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 1);

        var res = await new StocktakingService(t.Db).ListItemsAtLocationAsync(default, "W1_ZA_S1");
        var rows = ResultAssert.Ok<ApiSuccess<List<StocktakingGroupDto>>>(res).Data!;

        var row = Assert.Single(rows);
        Assert.Equal("ITM-A", row.ItemCode);
        Assert.Equal("Item A", row.ItemName);
        Assert.Equal("LOT-1", row.LotNo);
        Assert.Equal("pcs", row.Unit);
        Assert.Equal(2, row.PlannedLabels);
        Assert.Equal(1, row.ScannedLabels);
        Assert.Equal(20m, row.PlannedQty);
    }


    [Fact]
    public async Task The_label_view_lists_one_row_per_label()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).ListLabelsAsync(default, view: "label");
        var page = ResultAssert.Ok<ApiListSuccess<StocktakingLabelDto>>(res);
        Assert.Equal(3, page.Meta!.Total);
        Assert.Contains(page.Data!, r => r.SerialNo == "SN-001" && r.Qty == 10m && r.Unit == "pcs");
    }

    [Fact]
    public async Task The_location_view_groups_the_labels()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 2);

        var res = await new StocktakingService(t.Db).ListLabelsAsync(default, view: "model");
        var page = ResultAssert.Ok<ApiListSuccess<StocktakingGroupDto>>(res);
        Assert.Equal(2, page.Meta!.Total);

        var a = page.Data!.Single(r => r.ItemCode == "ITM-A");
        Assert.Equal(2, a.PlannedLabels);
        Assert.Equal(2, a.ScannedLabels);
        Assert.Equal("W1_ZA_S1", a.LocationText);
    }

    [Fact]
    public async Task The_not_scanned_filter_hides_groups_already_touched()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 1);

        var res = await new StocktakingService(t.Db).ListLabelsAsync(default, view: "model", notScanned: true);
        var page = ResultAssert.Ok<ApiListSuccess<StocktakingGroupDto>>(res);
        Assert.Equal("ITM-B", Assert.Single(page.Data!).ItemCode);
    }

    [Fact]
    public async Task The_mismatch_filter_keeps_only_groups_that_do_not_add_up()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 2);
        await Count(t, "W1_ZA_S2", "ITM-B", "LOT-2", 0);

        var res = await new StocktakingService(t.Db).ListLabelsAsync(default, view: "model", statusDiff: true);
        var page = ResultAssert.Ok<ApiListSuccess<StocktakingGroupDto>>(res);
        Assert.Equal("ITM-B", Assert.Single(page.Data!).ItemCode);
    }

    [Fact]
    public async Task The_label_view_can_be_filtered_by_location()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).ListLabelsAsync(default, view: "label", locationCode: "W1_ZA_S2");
        var page = ResultAssert.Ok<ApiListSuccess<StocktakingLabelDto>>(res);
        Assert.Equal("SN-003", Assert.Single(page.Data!).SerialNo);
    }

    [Fact]
    public async Task The_grid_is_empty_before_any_session()
    {
        using var t = Warehouse();
        var res = await new StocktakingService(t.Db).ListLabelsAsync(default, view: "label");
        var page = ResultAssert.Ok<ApiListSuccess<StocktakingLabelDto>>(res);
        Assert.Equal(0, page.Meta!.Total);
    }

    [Fact]
    public async Task The_label_csv_carries_the_grid_columns()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).ExportLabelsCsvAsync(default, view: "label");
        var text = await CsvReader.ReadAsync(res);

        var head = text.Split('\n')[0].Trim();
        Assert.StartsWith("serial_no,item_code,item_name,qty,unit,location_code,lot_no,status", head);
        Assert.Contains("SN-001", text);
    }


    [Fact]
    public async Task History_badges_match_and_diff()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 2);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var res = await new StocktakingService(t.Db).ListHistoryAsync(default);
        var page = ResultAssert.Ok<ApiListSuccess<StocktakingHistoryDto>>(res);

        Assert.Equal("badgeMatch", page.Data!.Single(r => r.ItemCode == "ITM-A").BadgeKey);
        Assert.Equal("badgeDiff", page.Data!.Single(r => r.ItemCode == "ITM-B").BadgeKey);
    }

    [Fact]
    public async Task History_search_covers_the_session_name_and_the_item()
    {
        using var t = Warehouse();
        await StartAsync(t, "Kiem ke thang 9");
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var byName = ResultAssert.Ok<ApiListSuccess<StocktakingHistoryDto>>(
            await new StocktakingService(t.Db).ListHistoryAsync(default, q: "thang 9"));
        Assert.Equal(2, byName.Meta!.Total);

        var byItem = ResultAssert.Ok<ApiListSuccess<StocktakingHistoryDto>>(
            await new StocktakingService(t.Db).ListHistoryAsync(default, q: "ITM-B"));
        Assert.Equal(1, byItem.Meta!.Total);
    }

    [Fact]
    public async Task The_history_csv_carries_the_grid_columns()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var res = await new StocktakingService(t.Db).ExportHistoryCsvAsync(default);
        var text = await CsvReader.ReadAsync(res);

        var head = text.Split('\n')[0].Trim();
        Assert.StartsWith("stocktaking_name,stocktaking_date,item_code,item_name,lot_no,unit,planned_qty,result_qty,missing_qty", head);
    }

    [Fact]
    public async Task Counting_an_item_the_books_never_put_here_is_refused()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto("W1_ZA_S1", "ITM-B", "LOT-2", 2), User(), default);
        ResultAssert.Error(res, 404, "LOC-003");

        Assert.False(await t.Db.StocktakingLabels
            .AnyAsync(x => x.ItemCode == "ITM-B" && x.LocationCode == "W1_ZA_S1"));
        var home = await t.Db.StocktakingLabels.AsNoTracking().SingleAsync(x => x.SerialNo == "SN-003");
        Assert.False(home.IsScanned);
    }

    [Fact]
    public async Task Counting_zero_for_an_item_that_was_never_here_is_refused_too()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto("W1_ZA_S1", "ITM-B", "LOT-2", 0), User(), default);
        ResultAssert.Error(res, 404, "LOC-003");
    }

    [Fact]
    public async Task A_stray_lot_of_a_known_item_is_refused()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto("W1_ZA_S1", "ITM-A", "LOT-9", 1), User(), default);
        ResultAssert.Error(res, 404, "LOC-003");

        var known = await Count(t, "W1_ZA_S1", "ITM-A", "LOT-1", 2);
        Assert.Equal(2, known.ScannedLabels);
    }

    [Fact]
    public async Task An_unknown_item_code_is_refused()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto("W1_ZA_S1", "ITM-ZZZ", "LOT-1", 1), User(), default);
        ResultAssert.Error(res, 404, "SYS-404");
    }


    [Fact]
    public async Task A_leftover_surplus_label_is_valued_at_the_items_package_qty()
    {
        using var t = Warehouse();
        (await t.Db.Items.SingleAsync(i => i.ItemCode == "ITM-B")).PackageQty = 5m;
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        var s = await StartAsync(t);
        await AddSurplusAsync(t, s.No, "ITM-B", "W1_ZA_S2", "LOT-2");

        var res = await new StocktakingService(t.Db).ListItemsAtLocationAsync(default, "W1_ZA_S2");
        var row = Assert.Single(ResultAssert.Ok<ApiSuccess<List<StocktakingGroupDto>>>(res).Data!);
        Assert.Equal(1, row.ScannedLabels);
        Assert.Equal(5m, row.ResultQty);
    }

    [Fact]
    public async Task A_leftover_surplus_label_with_no_packing_spec_carries_no_quantity()
    {
        using var t = Warehouse();
        (await t.Db.Items.SingleAsync(i => i.ItemCode == "ITM-B")).PackageQty = 0m;
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        var s = await StartAsync(t);
        await AddSurplusAsync(t, s.No, "ITM-B", "W1_ZA_S2", "LOT-2");

        var res = await new StocktakingService(t.Db).ListItemsAtLocationAsync(default, "W1_ZA_S2");
        var row = Assert.Single(ResultAssert.Ok<ApiSuccess<List<StocktakingGroupDto>>>(res).Data!);
        Assert.Equal(1, row.ScannedLabels);
        Assert.Equal(0m, row.ResultQty);
    }

    [Fact]
    public async Task A_recount_clears_a_surplus_label_left_by_an_older_session()
    {
        using var t = Warehouse();
        var s = await StartAsync(t);
        await AddSurplusAsync(t, s.No, "ITM-B", "W1_ZA_S2", "LOT-2");

        var g = await Count(t, "W1_ZA_S2", "ITM-B", "LOT-2", 1);
        Assert.Equal(1, g.PlannedLabels);
        Assert.Equal(1, g.ScannedLabels);
        Assert.Equal(0, await t.Db.StocktakingLabels
            .CountAsync(x => x.Status == StocktakingLabelStatus.Over));
    }

    private static async Task AddSurplusAsync(TestDb t, long sessionNo, string item, string loc, string? lot)
    {
        t.Db.StocktakingLabels.Add(new StocktakingLabel
        {
            SerialNo = $"OVR-{sessionNo}-{item}-1",
            StocktakingSessionNo = sessionNo,
            ItemCode = item, LocationCode = loc, LotNo = lot,
            IsScanned = true, Status = StocktakingLabelStatus.Over,
            UpdatedAt = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
    }
}
