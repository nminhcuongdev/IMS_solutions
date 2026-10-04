using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Stocktaking;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Stocktaking;
using Xunit;

namespace Ims.Api.Tests.Stocktaking;

public class StocktakingSessionTests
{
    internal static ClaimsPrincipal User(string code = "U001") =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, code) }, "test"));

    internal static TestDb Warehouse()
    {
        var t = TestDb.Seeded();
        var db = t.Db;
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Locations.Add(new Location
        {
            LocationCode = "W1_ZA_S1", LocationType = LocationType.Small,
            LargeCode = "W1", MiddleCode = "ZA", SmallCode = "S1",
            LocationName = "Shelf 1", ZoneCode = "ZONE-A",
            ParentLocationCode = "W1_ZA", IsActive = true, CreatedAt = now,
        });
        db.Locations.Add(new Location
        {
            LocationCode = "W1_ZA_S2", LocationType = LocationType.Small,
            LargeCode = "W1", MiddleCode = "ZA", SmallCode = "S2",
            LocationName = "Shelf 2", ZoneCode = "ZONE-A",
            ParentLocationCode = "W1_ZA", IsActive = true, CreatedAt = now,
        });

        foreach (var (code, name) in new[] { ("ITM-A", "Item A"), ("ITM-B", "Item B") })
            db.Items.Add(new Item
            {
                ItemCode = code, ItemName = name, ItemGroupCode = "IG-01",
                UomCode = "pcs", IsActive = true, CreatedAt = now,
            });

        Stock S(string item, string serial, string loc, string? lot, decimal qty) => new()
        {
            ItemCode = item, SerialNo = serial, FactoryCode = "FAC-01",
            LocationCode = loc, LotNo = lot, StockQty = qty, AvailableQty = qty,
            UpdatedAt = now,
        };
        db.Stocks.Add(S("ITM-A", "SN-001", "W1_ZA_S1", "LOT-1", 10));
        db.Stocks.Add(S("ITM-A", "SN-002", "W1_ZA_S1", "LOT-1", 10));
        db.Stocks.Add(S("ITM-B", "SN-003", "W1_ZA_S2", "LOT-2", 5));

        db.SaveChanges();
        db.ChangeTracker.Clear();
        return t;
    }

    internal static async Task<StocktakingSessionDto> StartAsync(TestDb t, string name = "Dot 2026-H2")
    {
        var res = await new StocktakingService(t.Db).StartAsync(new StartStocktakingDto(name), User(), default);
        return ResultAssert.Ok<ApiSuccess<StocktakingSessionDto>>(res).Data!;
    }


    [Fact]
    public async Task Start_requires_a_name()
    {
        using var t = Warehouse();
        var res = await new StocktakingService(t.Db).StartAsync(new StartStocktakingDto("   "), User(), default);
        ResultAssert.BadRequest(res, "VAL-002");
    }

    [Fact]
    public async Task Start_snapshots_every_stocked_label()
    {
        using var t = Warehouse();
        var s = await StartAsync(t);

        Assert.Equal(StocktakingSessionStatus.InProgress, s.Status);
        Assert.Equal(3, s.LabelCount);
        Assert.Equal(0, s.ScannedCount);

        var labels = await t.Db.StocktakingLabels.OrderBy(x => x.SerialNo).ToListAsync();
        Assert.Equal(new[] { "SN-001", "SN-002", "SN-003" }, labels.Select(x => x.SerialNo));
        Assert.All(labels, l => Assert.False(l.IsScanned));
        Assert.All(labels, l => Assert.Equal(StocktakingLabelStatus.NotStarted, l.Status));
    }

    [Fact]
    public async Task Start_skips_rows_with_nothing_left_on_the_shelf()
    {
        using var t = Warehouse();
        t.Db.Stocks.Add(new Stock
        {
            ItemCode = "ITM-B", SerialNo = "SN-EMPTY", FactoryCode = "FAC-01",
            LocationCode = "W1_ZA_S2", StockQty = 0, UpdatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var s = await StartAsync(t);
        Assert.Equal(3, s.LabelCount);
    }

    [Fact]
    public async Task Only_one_session_can_run_at_a_time()
    {
        using var t = Warehouse();
        await StartAsync(t);

        var res = await new StocktakingService(t.Db).StartAsync(new StartStocktakingDto("Dot thu hai"), User(), default);
        ResultAssert.Conflict(res);
    }

    [Fact]
    public async Task A_finalized_session_no_longer_blocks_the_next_one()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var res = await new StocktakingService(t.Db).StartAsync(new StartStocktakingDto("Dot sau"), User(), default);
        ResultAssert.Ok<ApiSuccess<StocktakingSessionDto>>(res);
    }

    [Fact]
    public async Task A_new_session_snapshots_the_warehouse_again()
    {
        using var t = Warehouse();
        var first = await StartAsync(t);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var second = await StartAsync(t, "Dot sau");
        Assert.NotEqual(first.No, second.No);
        Assert.Equal(3, second.LabelCount);

        var bySession = await t.Db.StocktakingLabels
            .Where(x => x.StocktakingSessionNo == second.No).CountAsync();
        Assert.Equal(3, bySession);
    }

    [Fact]
    public async Task Starting_again_replaces_the_previous_snapshot()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var second = await StartAsync(t, "Dot sau");

        Assert.Equal(3, await t.Db.StocktakingLabels.CountAsync());
        Assert.True(await t.Db.StocktakingLabels.AllAsync(x => x.StocktakingSessionNo == second.No));
    }


    [Fact]
    public async Task No_session_at_all_reads_as_null()
    {
        using var t = Warehouse();
        var res = await new StocktakingService(t.Db).GetSessionAsync(default);
        Assert.Null(ResultAssert.Ok<ApiSuccess<StocktakingSessionDto?>>(res).Data);
    }

    [Fact]
    public async Task The_current_session_carries_the_scan_progress()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await SaveAsync(t, "W1_ZA_S1", "ITM-A", "LOT-1", 1);

        var res = await new StocktakingService(t.Db).GetSessionAsync(default);
        var s = ResultAssert.Ok<ApiSuccess<StocktakingSessionDto?>>(res).Data!;
        Assert.Equal(3, s.LabelCount);
        Assert.Equal(1, s.ScannedCount);
    }


    [Fact]
    public async Task Complete_without_a_running_session_is_refused()
    {
        using var t = Warehouse();
        var res = await new StocktakingService(t.Db).CompleteAsync(User(), default);
        ResultAssert.Error(res, 409, "STK-001");
    }

    [Fact]
    public async Task Complete_locks_the_handheld_out()
    {
        using var t = Warehouse();
        await StartAsync(t);
        var res = await new StocktakingService(t.Db).CompleteAsync(User(), default);

        var s = ResultAssert.Ok<ApiSuccess<StocktakingSessionDto>>(res).Data!;
        Assert.Equal(StocktakingSessionStatus.Completed, s.Status);
        Assert.False(s.AcceptsResults);
        Assert.NotNull(s.CompletedAt);
    }

    [Fact]
    public async Task A_completed_session_has_its_own_badge()
    {
        using var t = Warehouse();
        var started = await StartAsync(t);
        var res = await new StocktakingService(t.Db).CompleteAsync(User(), default);

        Assert.Equal("slStatusInProgressBadge", started.StatusLabelKey);
        Assert.Equal("slStatusCompletedBadge",
            ResultAssert.Ok<ApiSuccess<StocktakingSessionDto>>(res).Data!.StatusLabelKey);
    }

    [Fact]
    public async Task An_uncounted_label_does_not_read_as_normal()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await SaveAsync(t, "W1_ZA_S2", "ITM-B", "LOT-2", 1);

        var page = ResultAssert.Ok<ApiListSuccess<StocktakingLabelDto>>(
            await new StocktakingService(t.Db).ListLabelsAsync(default, view: "label"));
        Assert.Equal("slFilterNotScanned", page.Data!.First(r => r.SerialNo == "SN-001").StatusLabelKey);
        Assert.Equal("slStatusNormal", page.Data!.Single(r => r.SerialNo == "SN-003").StatusLabelKey);
    }

    [Fact]
    public async Task Completing_twice_is_refused()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);

        var res = await new StocktakingService(t.Db).CompleteAsync(User(), default);
        ResultAssert.Error(res, 409, "STK-004");
    }


    [Fact]
    public async Task Finalize_before_complete_is_refused()
    {
        using var t = Warehouse();
        await StartAsync(t);
        var res = await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);
        ResultAssert.Error(res, 409, "STK-001");
    }

    [Fact]
    public async Task Finalize_marks_the_named_labels_missing()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await SaveAsync(t, "W1_ZA_S1", "ITM-A", "LOT-1", 1);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);

        var lost = await t.Db.StocktakingLabels
            .Where(x => !x.IsScanned && x.ItemCode == "ITM-A").Select(x => x.SerialNo).ToListAsync();
        Assert.Single(lost);

        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(lost), User(), default);

        var row = await t.Db.StocktakingLabels.SingleAsync(x => x.SerialNo == lost[0]);
        Assert.Equal(StocktakingLabelStatus.Missing, row.Status);
    }

    [Fact]
    public async Task A_label_that_was_counted_cannot_be_written_off()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await SaveAsync(t, "W1_ZA_S2", "ITM-B", "LOT-2", 1);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);

        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(new[] { "SN-003" }), User(), default);

        var row = await t.Db.StocktakingLabels.SingleAsync(x => x.SerialNo == "SN-003");
        Assert.Equal(StocktakingLabelStatus.Normal, row.Status);
    }

    [Fact]
    public async Task Finalize_writes_one_history_row_per_item_and_lot()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await SaveAsync(t, "W1_ZA_S1", "ITM-A", "LOT-1", 1);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var rows = await t.Db.StocktakingHistories.OrderBy(x => x.ItemCode).ToListAsync();
        Assert.Equal(2, rows.Count);

        var a = rows[0];
        Assert.Equal("ITM-A", a.ItemCode);
        Assert.Equal("LOT-1", a.LotNo);
        Assert.Equal("Dot 2026-H2", a.StocktakingName);
        Assert.Equal(20m, a.PlannedQty);
        Assert.Equal(10m, a.ResultQty);
        Assert.Equal(0m, a.MissingQty);
    }

    [Fact]
    public async Task Only_the_labels_ticked_at_finalize_count_as_missing()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await SaveAsync(t, "W1_ZA_S1", "ITM-A", "LOT-1", 1);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);

        var lost = await t.Db.StocktakingLabels
            .Where(x => !x.IsScanned && x.ItemCode == "ITM-A").Select(x => x.SerialNo).ToListAsync();
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(lost), User(), default);

        var a = await t.Db.StocktakingHistories.SingleAsync(x => x.ItemCode == "ITM-A");
        var b = await t.Db.StocktakingHistories.SingleAsync(x => x.ItemCode == "ITM-B");
        Assert.Equal(10m, a.MissingQty);
        Assert.Equal(0m, b.MissingQty);
    }

    [Fact]
    public async Task A_found_package_is_valued_in_the_history_and_is_not_a_match()
    {
        using var t = Warehouse();
        var b = await t.Db.Items.SingleAsync(i => i.ItemCode == "ITM-B");
        b.PackageQty = 5;
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();

        var s = await StartAsync(t);
        await SaveAsync(t, "W1_ZA_S2", "ITM-B", "LOT-2", 1);
        t.Db.StocktakingLabels.Add(new StocktakingLabel
        {
            SerialNo = "OVR-OLD-1", StocktakingSessionNo = s.No,
            ItemCode = "ITM-B", LocationCode = "W1_ZA_S2", LotNo = "LOT-2",
            IsScanned = true, Status = StocktakingLabelStatus.Over,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        await SaveAsync(t, "W1_ZA_S1", "ITM-A", "LOT-1", 2);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var row = await t.Db.StocktakingHistories.SingleAsync(x => x.ItemCode == "ITM-B");
        Assert.Equal((5m, 10m, 0m), (row.PlannedQty, row.ResultQty, row.MissingQty));

        var page = ResultAssert.Ok<ApiListSuccess<StocktakingHistoryDto>>(
            await new StocktakingService(t.Db).ListHistoryAsync(default));
        Assert.Equal("badgeDiff", page.Data!.Single(r => r.ItemCode == "ITM-B").BadgeKey);
        Assert.Equal("badgeMatch", page.Data!.Single(r => r.ItemCode == "ITM-A").BadgeKey);
    }

    [Fact]
    public async Task A_later_session_overwrites_the_summary()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        await StartAsync(t, "Dot sau");
        await SaveAsync(t, "W1_ZA_S1", "ITM-A", "LOT-1", 2);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var a = await t.Db.StocktakingHistories.SingleAsync(x => x.ItemCode == "ITM-A");
        Assert.Equal("Dot sau", a.StocktakingName);
        Assert.Equal(20m, a.ResultQty);
        Assert.Equal(0m, a.MissingQty);
        Assert.Equal(2, await t.Db.StocktakingHistories.CountAsync());
    }

    [Fact]
    public async Task Finalize_does_not_touch_the_stock_table()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(new[] { "SN-001", "SN-002" }), User(), default);

        var qty = await t.Db.Stocks.Where(x => x.ItemCode == "ITM-A").SumAsync(x => x.StockQty);
        Assert.Equal(20m, qty);
        Assert.Empty(await t.Db.InventoryHistories.ToListAsync());
    }

    [Fact]
    public async Task Finalize_closes_the_session()
    {
        using var t = Warehouse();
        await StartAsync(t);
        await new StocktakingService(t.Db).CompleteAsync(User(), default);
        await new StocktakingService(t.Db).FinalizeAsync(new FinalizeStocktakingDto(null), User(), default);

        var res = await new StocktakingService(t.Db).GetSessionAsync(default);
        Assert.Null(ResultAssert.Ok<ApiSuccess<StocktakingSessionDto?>>(res).Data);

        var closed = await t.Db.StocktakingSessions.SingleAsync();
        Assert.Equal(StocktakingSessionStatus.Finalized, closed.Status);
    }

    internal static async Task SaveAsync(TestDb t, string loc, string item, string? lot, int counted)
    {
        var res = await new StocktakingService(t.Db).SaveResultAsync(new StocktakingResultDto(loc, item, lot, counted), User(), default);
        ResultAssert.Ok<ApiSuccess<StocktakingGroupDto>>(res);
    }
}
