using Ims.Api.Contracts;
using Ims.Api.Features.Ht;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Ims.Domain.System;
using Xunit;

namespace Ims.Api.Tests.Ht;

public class HtServiceTests
{
    private static async Task SeedItemAsync(TestDb t)
    {
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Industrial Printer 4in", ItemGroupCode = "IG-01",
            UomCode = "pcs", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
    }

    private static ReceivingPlan Plan(string poNo, string? lot, string item = "IT-01") => new()
    {
        PoNo = poNo, PoBranchNo = "01", ItemCode = item, Qty = 1,
        ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), LotNo = lot,
        CreatedAt = DateTime.UtcNow,
    };


    [Fact]
    public async Task Config_falls_back_to_defaults_when_nothing_is_configured()
    {
        using var t = TestDb.Empty();

        var res = await new HtService(t.Db).GetConfigAsync(default);

        var cfg = ResultAssert.Ok<ApiSuccess<HtConfigDto>>(res).Data!;
        Assert.Equal("printNow", cfg.LabelModeRcv);
        Assert.Equal("byFunction", cfg.ReasonScope);
        Assert.Null(cfg.ApkVersion);
        Assert.Empty(cfg.PrinterDrivers);
    }

    [Fact]
    public async Task Config_reads_the_ht_rows_and_splits_the_printer_driver_list()
    {
        using var t = TestDb.Empty();
        var now = DateTime.UtcNow;
        t.Db.GeneralSettings.AddRange(
            new GeneralSetting { ConfigCode = "ht_label_mode_rcv", ConfigValue = "preIssue", Category = "HT", CreatedAt = now },
            new GeneralSetting { ConfigCode = "ht_apk_version", ConfigValue = "1.4.2", Category = "HT", CreatedAt = now },
            new GeneralSetting { ConfigCode = "ht_printer_drivers", ConfigValue = "IND4-J, HND208 ,", Category = "HT", CreatedAt = now },
            new GeneralSetting { ConfigCode = "default_factory", ConfigValue = "FAC-01", Category = "Factory", CreatedAt = now });
        await t.Db.SaveChangesAsync();

        var res = await new HtService(t.Db).GetConfigAsync(default);

        var cfg = ResultAssert.Ok<ApiSuccess<HtConfigDto>>(res).Data!;
        Assert.Equal("preIssue", cfg.LabelModeRcv);
        Assert.Equal("printNow", cfg.LabelModeShp);
        Assert.Equal("1.4.2", cfg.ApkVersion);
        Assert.Equal(new[] { "IND4-J", "HND208" }, cfg.PrinterDrivers);
    }

    [Fact]
    public async Task Config_treats_a_blank_value_as_not_configured()
    {
        using var t = TestDb.Empty();
        t.Db.GeneralSettings.Add(new GeneralSetting
        {
            ConfigCode = "ht_reason_scope", ConfigValue = "   ", Category = "HT", CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new HtService(t.Db).GetConfigAsync(default);

        Assert.Equal("byFunction", ResultAssert.Ok<ApiSuccess<HtConfigDto>>(res).Data!.ReasonScope);
    }


    [Fact]
    public async Task Lots_returns_the_distinct_pool_and_skips_lines_without_a_lot()
    {
        using var t = TestDb.Seeded();
        await SeedItemAsync(t);
        t.Db.ReceivingPlans.AddRange(
            Plan("PO-001", "LOT-B"),
            Plan("PO-002", "LOT-A"),
            Plan("PO-003", "LOT-A"),
            Plan("PO-004", null),
            Plan("PO-005", ""));
        await t.Db.SaveChangesAsync();

        var res = await new HtService(t.Db).ListLotsAsync(default);

        var rows = ResultAssert.Ok<ApiListSuccess<HtLotDto>>(res).Data;
        Assert.Equal(new[] { "LOT-A", "LOT-B" }, rows.Select(r => r.LotNo).ToArray());
    }

    [Fact]
    public async Task Lots_filters_by_item_code()
    {
        using var t = TestDb.Seeded();
        await SeedItemAsync(t);
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-02", ItemName = "Other", ItemGroupCode = "IG-01",
            IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.ReceivingPlans.AddRange(
            Plan("PO-001", "LOT-A", "IT-01"),
            Plan("PO-002", "LOT-B", "IT-02"));
        await t.Db.SaveChangesAsync();

        var res = await new HtService(t.Db).ListLotsAsync(default, itemCode: "IT-02");

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<HtLotDto>>(res).Data);
        Assert.Equal("LOT-B", row.LotNo);
    }

    [Fact]
    public async Task Lots_filters_by_partial_match()
    {
        using var t = TestDb.Seeded();
        await SeedItemAsync(t);
        t.Db.ReceivingPlans.AddRange(
            Plan("PO-001", "LOT260809"),
            Plan("PO-002", "ABC-999"));
        await t.Db.SaveChangesAsync();

        var res = await new HtService(t.Db).ListLotsAsync(default, q: "2608");

        var row = Assert.Single(ResultAssert.Ok<ApiListSuccess<HtLotDto>>(res).Data);
        Assert.Equal("LOT260809", row.LotNo);
    }

    [Fact]
    public async Task IsKnownLot_answers_from_the_receiving_plan_pool()
    {
        using var t = TestDb.Seeded();
        await SeedItemAsync(t);
        t.Db.ReceivingPlans.Add(Plan("PO-001", "LOT-A"));
        await t.Db.SaveChangesAsync();

        var known = await new Ims.Api.Features.Ht.LotLookup(t.Db).IsKnownLotAsync("LOT-A", default);
        var unknown = await new Ims.Api.Features.Ht.LotLookup(t.Db).IsKnownLotAsync("LOT-Z", default);

        Assert.True(known);
        Assert.False(unknown);
    }
}
