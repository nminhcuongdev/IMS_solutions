using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.StockScan;
using Ims.Api.Tests.Infrastructure;
using Xunit;
using Scan = Ims.Domain.Stocktaking.StockScan;

namespace Ims.Api.Tests.StockScan;

public class StockScanRegisterTests
{
    private static ClaimsPrincipal User() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "U-RFID")], "test"));

    private static TestDb Board()
    {
        var t = TestDb.Seeded();
        t.Db.StockScans.AddRange(
            Row("A0003000000000000000001", "PN-100", "Head", scanned: true),
            Row("A0003000000000000000002", "PN-100", "Head", scanned: false),
            Row("A0004000000000000000001", "PN-200", "Platen", scanned: false));
        t.Db.SaveChanges();
        t.Db.ChangeTracker.Clear();
        return t;
    }

    private static Scan Row(string epc, string? code, string? name, bool scanned, bool target = true) => new()
    {
        Epc = epc, ItemCode = code, ItemName = name, IsScanned = scanned, IsTarget = target,
    };

    private static StockScanService Svc(TestDb t) => new(t.Db);

    private static StockScanRegisterGroupDto Group(string? code, string? name, params string?[] epcs) =>
        new(code, name, epcs);

    private static Task<ServiceResult> Register(TestDb t, params StockScanRegisterGroupDto[] groups) =>
        Svc(t).RegisterAsync(new StockScanRegisterDto(groups), default);

    private static async Task<IReadOnlyList<StockScanPartDto>> Parts(TestDb t) =>
        ResultAssert.Ok<ApiSuccess<IReadOnlyList<StockScanPartDto>>>(await Svc(t).PartsAsync(default)).Data!;

    [Fact]
    public async Task Register_adds_every_epc_of_every_group_under_its_part()
    {
        using var t = Board();
        var r = ResultAssert.Created<ApiSuccess<StockScanRegisterResultDto>>(await Register(t,
            Group(" PN-300 ", " Ribbon ", "a0005000000000000000001", "A0005000000000000000002"),
            Group("PN-400", "Label", "A0006000000000000000001"))).Data!;

        Assert.Equal((3, 2), (r.Registered, r.Parts));
        var row = await t.Db.StockScans.AsNoTracking().SingleAsync(x => x.Epc == "A0005000000000000000001");
        Assert.Equal(("PN-300", "Ribbon", false, true), (row.ItemCode, row.ItemName, row.IsScanned, row.IsTarget));
        Assert.Equal(6, await t.Db.StockScans.CountAsync());
    }

    [Fact]
    public async Task Register_refuses_an_epc_that_is_already_registered_and_writes_nothing()
    {
        using var t = Board();
        var r = await Register(t, Group("PN-300", "Ribbon", "A0005000000000000000001", "a0003000000000000000002"));

        ResultAssert.Error(r, 409, "DUP-002");
        Assert.Equal(3, await t.Db.StockScans.CountAsync());
    }

    [Fact]
    public async Task Register_refuses_the_same_epc_twice_in_one_request()
    {
        using var t = Board();
        var r = await Register(t,
            Group("PN-300", "Ribbon", "A0005000000000000000001"),
            Group("PN-400", "Label", " a0005000000000000000001 "));

        ResultAssert.Error(r, 409, "DUP-002");
        Assert.Equal(3, await t.Db.StockScans.CountAsync());
    }

    [Fact]
    public async Task Register_requires_a_part_no_and_a_part_name()
    {
        using var t = Board();
        ResultAssert.BadRequest(await Register(t, Group("  ", "Ribbon", "A0005000000000000000001")), "VAL-001");
        ResultAssert.BadRequest(await Register(t, Group("PN-300", null, "A0005000000000000000001")), "VAL-001");
        Assert.Equal(3, await t.Db.StockScans.CountAsync());
    }

    [Fact]
    public async Task Register_refuses_a_part_no_or_name_longer_than_its_column()
    {
        using var t = Board();
        ResultAssert.BadRequest(await Register(t, Group(new string('P', 51), "Ribbon", "A0005000000000000000001")), "VAL-005");
        ResultAssert.BadRequest(await Register(t, Group("PN-300", new string('N', 256), "A0005000000000000000001")), "VAL-005");
    }

    [Theory]
    [InlineData("E2801170000Z")]
    [InlineData("A000300000000000000000010")]
    [InlineData("ABC")]
    [InlineData("ABCD")]
    public async Task Register_refuses_an_epc_that_is_not_hex_or_has_the_wrong_length(string epc)
    {
        using var t = Board();
        ResultAssert.BadRequest(await Register(t, Group("PN-300", "Ribbon", epc)), "VAL-005");
    }

    [Fact]
    public async Task Register_with_no_epc_at_all_asks_for_a_selection()
    {
        using var t = Board();
        ResultAssert.BadRequest(await Svc(t).RegisterAsync(new StockScanRegisterDto(null), default), "SEL-004");
        ResultAssert.BadRequest(await Register(t, Group("PN-300", "Ribbon", " ", null)), "SEL-004");
    }

    [Fact]
    public async Task Register_under_a_part_that_is_not_being_counted_keeps_it_out_of_the_count()
    {
        using var t = Board();
        await t.Db.StockScans.Where(x => x.ItemCode == "PN-200").ExecuteUpdateAsync(s => s.SetProperty(x => x.IsTarget, false));

        await Register(t, Group("PN-200", "Platen", "A0004000000000000000002"));

        var row = await t.Db.StockScans.AsNoTracking().SingleAsync(x => x.Epc == "A0004000000000000000002");
        Assert.False(row.IsTarget);
    }

    [Fact]
    public async Task Lookup_reports_registered_epcs_and_the_part_already_known_for_a_prefix()
    {
        using var t = Board();
        var r = ResultAssert.Ok<ApiSuccess<StockScanLookupResultDto>>(await Svc(t).LookupAsync(
            new StockScanLookupDto(["a0003000000000000000002", "A0003000000000000000009", "A0007000000000000000001", "XYZ"]),
            default)).Data!;

        var reg = Assert.Single(r.Registered);
        Assert.Equal(("A0003000000000000000002", "PN-100", "Head"), (reg.Epc, reg.ItemCode, reg.ItemName));
        var known = Assert.Single(r.Prefixes);
        Assert.Equal(("A0003", "PN-100", "Head"), (known.Prefix, known.ItemCode, known.ItemName));
    }

    [Fact]
    public async Task Lookup_groups_by_the_first_five_characters_whatever_the_length()
    {
        using var t = Board();
        t.Db.StockScans.AddRange(
            Row("A0003FFF", "PN-LONG", "Long", scanned: false),
            Row("A0005ABCDEF", "PN-500", "Short", scanned: false));
        await t.Db.SaveChangesAsync();

        var r = ResultAssert.Ok<ApiSuccess<StockScanLookupResultDto>>(await Svc(t).LookupAsync(
            new StockScanLookupDto(["A0003ABCDEF0123456789", "A0005000000000000000001", "A00060"]), default)).Data!;

        Assert.Equal(["A0003", "A0005"], r.Prefixes.Select(p => p.Prefix));
        Assert.Equal(["PN-100", "PN-500"], r.Prefixes.Select(p => p.ItemCode));
    }

    [Fact]
    public async Task Parts_lists_one_row_per_part_with_its_counts_and_target_flag()
    {
        using var t = Board();
        var p = await Parts(t);

        Assert.Equal(["PN-100", "PN-200"], p.Select(x => x.ItemCode));
        Assert.Equal(("Head", 2, 1, true), (p[0].ItemName, p[0].Total, p[0].Scanned, p[0].IsTarget));
    }

    [Fact]
    public async Task Targets_limit_the_board_summary_and_reads_to_the_chosen_parts()
    {
        using var t = Board();
        var p = ResultAssert.Ok<ApiSuccess<IReadOnlyList<StockScanPartDto>>>(
            await Svc(t).SetTargetsAsync(new StockScanTargetsDto(["PN-200"]), default)).Data!;

        Assert.Equal([false, true], p.Select(x => x.IsTarget));

        var b = ResultAssert.Ok<ApiSuccess<StockScanBoardDto>>(await Svc(t).ListAsync(default)).Data!;
        Assert.Equal(["A0004000000000000000001"], b.Rows.Select(x => x.Epc));
        Assert.Equal((0, 1), (b.Scanned, b.Total));

        var s = ResultAssert.Ok<ApiSuccess<StockScanSummaryDto>>(await Svc(t).SummaryAsync(default)).Data!;
        Assert.Equal("PN-200", Assert.Single(s.Items).ItemCode);

        var read = ResultAssert.Ok<ApiSuccess<StockScanReadResultDto>>(await Svc(t).ReadAsync(
            new StockScanReadDto(["A0003000000000000000002", "A0004000000000000000001"]), User(), default)).Data!;
        Assert.Equal((1, 0, 1, 1, 1), (read.Accepted, read.AlreadyScanned, read.Ignored, read.Scanned, read.Total));
        Assert.False((await t.Db.StockScans.AsNoTracking().SingleAsync(x => x.Epc == "A0003000000000000000002")).IsScanned);
    }

    [Fact]
    public async Task Targets_address_a_part_without_a_code_as_an_empty_string()
    {
        using var t = Board();
        t.Db.StockScans.Add(Row("A0009000000000000000001", null, null, scanned: false));
        await t.Db.SaveChangesAsync();

        var p = ResultAssert.Ok<ApiSuccess<IReadOnlyList<StockScanPartDto>>>(
            await Svc(t).SetTargetsAsync(new StockScanTargetsDto([""]), default)).Data!;

        Assert.Equal([true, false, false], p.Select(x => x.IsTarget));
        Assert.Null(p[0].ItemCode);
    }

    [Fact]
    public async Task Delete_part_removes_every_tag_of_that_part_scanned_or_not()
    {
        using var t = Board();
        var r = ResultAssert.Ok<ApiSuccess<StockScanDeletePartResultDto>>(
            await Svc(t).DeletePartAsync(" PN-100 ", default)).Data!;

        Assert.Equal(2, r.Deleted);
        Assert.Equal(["PN-200"], r.Parts.Select(p => p.ItemCode));
        Assert.Equal(1, await t.Db.StockScans.CountAsync());
    }

    [Fact]
    public async Task Delete_part_addresses_a_part_without_a_code_as_an_empty_string()
    {
        using var t = Board();
        t.Db.StockScans.Add(Row("A0009000000000000000001", null, null, scanned: false));
        await t.Db.SaveChangesAsync();

        var r = ResultAssert.Ok<ApiSuccess<StockScanDeletePartResultDto>>(await Svc(t).DeletePartAsync(null, default)).Data!;

        Assert.Equal(1, r.Deleted);
        Assert.Equal(3, await t.Db.StockScans.CountAsync());
    }

    [Fact]
    public async Task Delete_part_that_does_not_exist_is_not_found_and_deletes_nothing()
    {
        using var t = Board();
        ResultAssert.NotFound(await Svc(t).DeletePartAsync("NOPE", default));
        ResultAssert.NotFound(await Svc(t).DeletePartAsync("", default));
        Assert.Equal(3, await t.Db.StockScans.CountAsync());
    }

    [Fact]
    public async Task Targets_refuse_an_empty_selection_or_one_that_matches_no_part()
    {
        using var t = Board();
        ResultAssert.BadRequest(await Svc(t).SetTargetsAsync(new StockScanTargetsDto([]), default), "SEL-004");
        ResultAssert.BadRequest(await Svc(t).SetTargetsAsync(new StockScanTargetsDto(null), default), "SEL-004");
        ResultAssert.BadRequest(await Svc(t).SetTargetsAsync(new StockScanTargetsDto(["NOPE"]), default), "SEL-004");
        Assert.All(await Parts(t), x => Assert.True(x.IsTarget));
    }
}
