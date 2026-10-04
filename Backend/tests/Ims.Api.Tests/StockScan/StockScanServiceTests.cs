using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.StockScan;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;
using Scan = Ims.Domain.Stocktaking.StockScan;

namespace Ims.Api.Tests.StockScan;

public class StockScanServiceTests
{
    private static ClaimsPrincipal User(string code = "U-RFID") =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, code)], "test"));

    private static TestDb Board()
    {
        var t = TestDb.Seeded();
        t.Db.AppUsers.Add(new AppUser
        {
            UserCode = "U-RFID", UserName = "Trần Thị Bích", PasswordHash = "x",
            RoleCode = "Admin", IsActive = true, CreatedAt = new DateTime(2026, 1, 1),
        });
        t.Db.StockScans.AddRange(
            Row("0000003", "M47903000", "Printer head", scanned: false),
            Row("0000001", "M47903000", "Printer head", scanned: true, worker: "5936", workerName: "Operator 01"),
            Row("0000002", "M47903000", "Printer head", scanned: false),
            Row("0000010", "7100001", "IND4", scanned: true, worker: "5936", workerName: "Operator 01"),
            Row("0000011", "7100001", "IND4", scanned: false));
        t.Db.SaveChanges();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static Scan Row(string epc, string code, string name, bool scanned,
        string? worker = null, string? workerName = null) => new()
    {
        Epc = epc, ItemCode = code, ItemName = name, ItemCategory = "Printer parts",
        IsScanned = scanned, WorkerCode = worker, WorkerName = workerName,
    };

    private static async Task<StockScanBoardDto> List(TestDb t,
        string? itemCode = null, string? itemName = null, string? scanned = null) =>
        ResultAssert.Ok<ApiSuccess<StockScanBoardDto>>(
            await new StockScanService(t.Db).ListAsync(default, itemCode, itemName, scanned)).Data!;

    private static async Task<StockScanReadResultDto> Read(TestDb t, params string?[] epcs) =>
        ResultAssert.Ok<ApiSuccess<StockScanReadResultDto>>(
            await new StockScanService(t.Db).ReadAsync(new StockScanReadDto(epcs), User(), default)).Data!;

    private static Task<Scan> Stored(TestDb t, string epc) =>
        t.Db.StockScans.AsNoTracking().SingleAsync(x => x.Epc == epc);

    [Fact]
    public async Task List_returns_every_tag_ordered_by_item_then_epc()
    {
        using var t = Board();
        var b = await List(t);

        Assert.Equal(["0000010", "0000011", "0000001", "0000002", "0000003"], b.Rows.Select(r => r.Epc));
        Assert.Equal(2, b.Scanned);
        Assert.Equal(5, b.Total);
    }

    [Fact]
    public async Task List_filters_by_partial_item_code_and_name_ignoring_case()
    {
        using var t = Board();

        Assert.Equal(3, (await List(t, itemCode: "4790")).Rows.Count);
        Assert.Equal(3, (await List(t, itemName: "PRINTER")).Rows.Count);
        Assert.Equal(2, (await List(t, itemCode: " 7100 ", itemName: "ind")).Rows.Count);
    }

    [Fact]
    public async Task List_filters_by_scanned_flag_while_the_counter_covers_the_whole_table()
    {
        using var t = Board();

        var yes = await List(t, scanned: "yes");
        Assert.Equal(["0000010", "0000001"], yes.Rows.Select(r => r.Epc));
        Assert.Equal((2, 5), (yes.Scanned, yes.Total));

        var no = await List(t, scanned: "no");
        Assert.Equal(3, no.Rows.Count);
        Assert.All(no.Rows, r => Assert.False(r.IsScanned));
        Assert.Equal((2, 5), (no.Scanned, no.Total));
    }

    [Fact]
    public async Task List_treats_an_unknown_scanned_value_as_all()
    {
        using var t = Board();
        Assert.Equal(5, (await List(t, scanned: "maybe")).Rows.Count);
    }

    [Fact]
    public async Task Read_marks_the_tag_scanned_by_the_signed_in_worker()
    {
        using var t = Board();
        var r = await Read(t, "0000002");

        Assert.Equal((1, 0, 0, 3, 5), (r.Accepted, r.AlreadyScanned, r.Ignored, r.Scanned, r.Total));
        var row = await Stored(t, "0000002");
        Assert.True(row.IsScanned);
        Assert.Equal("U-RFID", row.WorkerCode);
        Assert.Equal("Trần Thị Bích", row.WorkerName);
    }

    [Fact]
    public async Task Read_falls_back_to_the_user_code_when_the_worker_is_not_in_the_user_master()
    {
        using var t = Board();
        await new StockScanService(t.Db).ReadAsync(new StockScanReadDto(["0000002"]), User("GHOST"), default);

        var row = await Stored(t, "0000002");
        Assert.Equal("GHOST", row.WorkerCode);
        Assert.Equal("GHOST", row.WorkerName);
    }

    [Fact]
    public async Task Read_ignores_a_tag_that_is_not_in_the_plan_and_adds_no_row()
    {
        using var t = Board();
        var r = await Read(t, "9999999");

        Assert.Equal((0, 0, 1, 2, 5), (r.Accepted, r.AlreadyScanned, r.Ignored, r.Scanned, r.Total));
        Assert.Equal(5, await t.Db.StockScans.CountAsync());
    }

    [Fact]
    public async Task Read_keeps_the_worker_who_scanned_a_tag_first()
    {
        using var t = Board();
        var r = await Read(t, "0000001");

        Assert.Equal((0, 1, 0), (r.Accepted, r.AlreadyScanned, r.Ignored));
        var row = await Stored(t, "0000001");
        Assert.Equal("5936", row.WorkerCode);
        Assert.Equal("Operator 01", row.WorkerName);
    }

    [Fact]
    public async Task Read_counts_a_tag_repeated_in_one_batch_once_and_skips_blanks()
    {
        using var t = Board();
        var r = await Read(t, "0000002", " 0000002 ", "", null, "0000003");

        Assert.Equal((2, 0, 0, 4, 5), (r.Accepted, r.AlreadyScanned, r.Ignored, r.Scanned, r.Total));
    }

    [Fact]
    public async Task Read_with_nothing_in_the_batch_changes_nothing()
    {
        using var t = Board();
        var r = ResultAssert.Ok<ApiSuccess<StockScanReadResultDto>>(
            await new StockScanService(t.Db).ReadAsync(new StockScanReadDto(null), User(), default)).Data!;

        Assert.Equal((0, 0, 0, 2, 5), (r.Accepted, r.AlreadyScanned, r.Ignored, r.Scanned, r.Total));
    }

    [Fact]
    public async Task Summary_counts_plan_and_scanned_tags_per_item()
    {
        using var t = Board();
        var s = ResultAssert.Ok<ApiSuccess<StockScanSummaryDto>>(
            await new StockScanService(t.Db).SummaryAsync(default)).Data!;

        Assert.Equal((2, 5), (s.Scanned, s.Total));
        Assert.Equal(2, s.Items.Count);
        Assert.Equal(("7100001", "IND4", 2, 1), (s.Items[0].ItemCode, s.Items[0].ItemName, s.Items[0].Plan, s.Items[0].Qty));
        Assert.Equal(("M47903000", "Printer head", 3, 1), (s.Items[1].ItemCode, s.Items[1].ItemName, s.Items[1].Plan, s.Items[1].Qty));
    }

    [Fact]
    public async Task Reset_clears_every_scanned_flag_and_worker()
    {
        using var t = Board();
        var b = ResultAssert.Ok<ApiSuccess<StockScanBoardDto>>(
            await new StockScanService(t.Db).ResetAsync(default)).Data!;

        Assert.Equal((0, 5), (b.Scanned, b.Total));
        var rows = await t.Db.StockScans.AsNoTracking().ToListAsync();
        Assert.Equal(5, rows.Count);
        Assert.All(rows, r =>
        {
            Assert.False(r.IsScanned);
            Assert.Null(r.WorkerCode);
            Assert.Null(r.WorkerName);
        });
    }
}
