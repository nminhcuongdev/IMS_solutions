using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Distribution;
using Ims.Api.Tests.Infrastructure;
using Xunit;

namespace Ims.Api.Tests.Distribution;

public class DistributionDeliveryTests
{
    private static string Status(IEnumerable<DistDeviceDto> list, string code) => list.Single(d => d.DeviceCode == code).Status;

    [Fact]
    public async Task A_target_device_gets_the_entry_without_a_targets_list()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync(10500, "1.5.0");
        var rule = await f.DistributeAsync(p, new RuleWriteDto(p.Id, Force: true, MinVersionCode: 10300, NoteVi: "Sửa lỗi"));

        var r = await f.Delivery().VersionJsonAsync("HT1", "10400", default);

        var e = Assert.Single(r.Body.Apps);
        Assert.Equal("com.imssolutions.ht", e.PackageName);
        Assert.Equal(10500, e.VersionCode);
        Assert.Equal(p.FileName, e.ApkFile);
        Assert.Equal(p.Sha256, e.Sha256);
        Assert.Equal(64, e.Sha256.Length);
        Assert.True(e.Force);
        Assert.Equal(10300, e.MinVersionCode);
        Assert.Equal(new Dictionary<string, string> { ["vi"] = "Sửa lỗi" }, e.Notes);
        Assert.Equal(rule.PublishedAt, r.LastModified);
        Assert.Matches(@"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d[+-]\d\d:\d\d$", e.UpdatedAt);
    }

    [Fact]
    public async Task A_device_outside_the_targets_gets_an_empty_list()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();
        await f.DistributeAsync(p, new RuleWriteDto(p.Id, Models: ["MT95"]));

        Assert.Empty((await f.Delivery().VersionJsonAsync("HT1", "1", default)).Body.Apps);
        Assert.Single((await f.Delivery().VersionJsonAsync("HT3", "1", default)).Body.Apps);
    }

    [Fact]
    public async Task An_unregistered_device_id_gets_nothing_and_is_listed()
    {
        using var f = new DistFixture();
        await f.DistributeAsync(await f.UploadAsync());

        Assert.Empty((await f.Delivery().VersionJsonAsync("HT-999", "1", default)).Body.Apps);
        await f.Delivery().VersionJsonAsync("HT-999", "1", default);
        Assert.Empty((await f.Delivery().VersionJsonAsync(null, "1", default)).Body.Apps);

        var u = Assert.Single(ResultAssert.Ok<ApiListSuccess<UnknownDeviceDto>>(await f.Service().ListUnknownAsync(default)).Data);
        Assert.Equal("HT-999", u.DeviceCode);
        Assert.Equal(2, u.RequestCount);
    }

    [Fact]
    public async Task A_check_records_the_version_and_a_rise_records_the_update_time()
    {
        using var f = new DistFixture();
        await f.DistributeAsync(await f.UploadAsync(10500, "1.5.0"));

        await f.Delivery().VersionJsonAsync("HT1", "10400", default);
        var first = (await f.DevicesAsync()).Single(d => d.DeviceCode == "HT1");
        Assert.Equal(10400, first.VersionCode);
        Assert.Null(first.LastUpdateAt);
        Assert.Equal(f.Clock.Local, first.LastCheckAt);

        f.Clock.Advance(TimeSpan.FromHours(1));
        await f.Delivery().VersionJsonAsync("HT1", "10500", default);
        var after = (await f.DevicesAsync()).Single(d => d.DeviceCode == "HT1");
        Assert.Equal(f.Clock.Local, after.LastUpdateAt);
        Assert.Equal("1.5.0", after.VersionName);

        var hist = ResultAssert.Ok<ApiListSuccess<DeviceCheckDto>>(await f.Service().DeviceHistoryAsync("HT1", default)).Data;
        Assert.Equal([10500, 10400], hist.Select(h => h.AppVersion));
    }

    [Fact]
    public async Task A_device_that_stays_on_the_same_version_keeps_its_update_time_empty()
    {
        using var f = new DistFixture();
        await f.DistributeAsync(await f.UploadAsync());

        await f.Delivery().VersionJsonAsync("HT1", "10400", default);
        await f.Delivery().VersionJsonAsync("HT1", "10400", default);

        Assert.Null((await f.DevicesAsync()).Single(d => d.DeviceCode == "HT1").LastUpdateAt);
    }

    [Fact]
    public async Task Status_follows_spec_6_3_on_every_branch()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync(10500, "1.5.0");
        await f.DistributeAsync(p, new RuleWriteDto(p.Id, Models: ["MT93"]));
        var d = f.Delivery();
        var src = (await d.FindApkAsync(p.FileName, default))!;

        await d.VersionJsonAsync("HT1", "10500", default);
        await d.VersionJsonAsync("HT2", "10400", default);
        await d.RecordDownloadAsync("HT2", src, default);

        var now = await f.DevicesAsync();
        Assert.Equal("latest", Status(now, "HT1"));
        Assert.Equal("downloaded", Status(now, "HT2"));
        Assert.Equal("notTarget", Status(now, "HT3"));

        f.Clock.Advance(TimeSpan.FromDays(3));
        await d.VersionJsonAsync("HT2", "10400", default);
        Assert.Equal("attention", Status(await f.DevicesAsync(), "HT2"));

        f.Clock.Advance(TimeSpan.FromDays(11));
        var later = await f.DevicesAsync();
        Assert.Equal("unconfirmed", Status(later, "HT1"));
        Assert.Equal("attention", Status(later, "HT2"));
    }

    [Fact]
    public async Task A_target_that_checked_but_has_not_downloaded_is_waiting_and_one_never_seen_is_unconfirmed()
    {
        using var f = new DistFixture();
        await f.DistributeAsync(await f.UploadAsync());

        await f.Delivery().VersionJsonAsync("HT1", "10400", default);
        var list = await f.DevicesAsync();

        Assert.Equal("waiting", Status(list, "HT1"));
        Assert.Equal("unconfirmed", Status(list, "HT2"));
    }

    [Fact]
    public async Task Without_an_active_rule_every_device_is_not_a_target()
    {
        using var f = new DistFixture();

        Assert.All(await f.DevicesAsync(), d => Assert.Equal("notTarget", d.Status));
    }

    [Fact]
    public async Task The_device_master_filter_returns_the_codes_in_a_status()
    {
        using var f = new DistFixture();
        await f.DistributeAsync(await f.UploadAsync(10500, "1.5.0"));
        await f.Delivery().VersionJsonAsync("HT1", "10500", default);

        var codes = await f.Service().DeviceCodesWithStatusAsync("latest", default);

        Assert.Equal(["HT1"], codes);
    }

    [Fact]
    public async Task The_device_master_list_filters_by_update_status()
    {
        using var f = new DistFixture();
        await f.DistributeAsync(await f.UploadAsync(10500, "1.5.0"));
        await f.Delivery().VersionJsonAsync("HT2", "10500", default);
        var devices = new Ims.Api.Features.Devices.DeviceService(f.T.Db, f.Service());

        var latest = await devices.ListAsync(default, updateStatus: "latest");
        var all = await devices.ListAsync(default, updateStatus: "all");

        Assert.Equal(["HT2"], ResultAssert.Ok<ApiListSuccess<Ims.Api.Features.Devices.DeviceDto>>(latest).Data.Select(d => d.DeviceCode));
        Assert.Equal(4, ResultAssert.Ok<ApiListSuccess<Ims.Api.Features.Devices.DeviceDto>>(all).Data.Count);
    }

    [Fact]
    public async Task The_device_master_sorts_on_the_recorded_version_and_times()
    {
        using var f = new DistFixture();
        await f.DistributeAsync(await f.UploadAsync(10500, "1.5.0"));
        await f.Delivery().VersionJsonAsync("HT2", "10300", default);
        f.Clock.Advance(TimeSpan.FromMinutes(1));
        await f.Delivery().VersionJsonAsync("HT1", "10500", default);
        f.Clock.Advance(TimeSpan.FromMinutes(1));
        await f.Delivery().VersionJsonAsync("HT3", "10400", default);
        await f.Delivery().VersionJsonAsync("HT3", "10600", default);
        var devices = new Ims.Api.Features.Devices.DeviceService(f.T.Db, f.Service());
        async Task<string[]> Codes(string sort) =>
            ResultAssert.Ok<ApiListSuccess<Ims.Api.Features.Devices.DeviceDto>>(await devices.ListAsync(default, sort: sort))
                .Data.Where(d => d.DeviceCode.StartsWith("HT")).Select(d => d.DeviceCode).ToArray();

        Assert.Equal(["HT2", "HT1", "HT3"], await Codes("app_version"));
        Assert.Equal(["HT3", "HT1", "HT2"], await Codes("-app_version"));
        Assert.Equal(["HT2", "HT1", "HT3"], await Codes("last_check_at"));
        Assert.Equal(["HT3", "HT1", "HT2"], await Codes("-last_update_at"));
    }

    [Fact]
    public async Task A_rule_summary_counts_its_targets_by_status()
    {
        using var f = new DistFixture();
        var rule = await f.DistributeAsync(await f.UploadAsync(10500, "1.5.0"));
        await f.Delivery().VersionJsonAsync("HT1", "10500", default);
        await f.Delivery().VersionJsonAsync("HT2", "10400", default);

        var s = (await f.RuleAsync(rule.Id)).Summary;

        Assert.Equal(new RuleSummaryDto(3, 1, 0, 1, 0, 1), s);
    }

    [Fact]
    public async Task Downloads_are_only_recorded_for_registered_devices()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();
        await f.DistributeAsync(p);
        var src = (await f.Delivery().FindApkAsync(p.FileName, default))!;

        await f.Delivery().RecordDownloadAsync("HT-999", src, default);
        await f.Delivery().RecordDownloadAsync("HT1", src, default);
        await f.Delivery().RecordDownloadAsync("HT1", src, default);

        Assert.Equal(["HT1"], await f.T.Db.DistDownloads.Select(x => x.DeviceCode).ToListAsync());
    }

    [Fact]
    public async Task An_unknown_or_path_like_apk_name_is_not_found()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();

        Assert.NotNull(await f.Delivery().FindApkAsync(p.FileName, default));
        Assert.Null(await f.Delivery().FindApkAsync("nope.apk", default));
        Assert.Null(await f.Delivery().FindApkAsync("../" + p.FileName, default));
    }
}
