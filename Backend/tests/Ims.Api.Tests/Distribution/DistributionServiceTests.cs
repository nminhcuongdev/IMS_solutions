using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Distribution;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Distribution;
using Xunit;

namespace Ims.Api.Tests.Distribution;

public class DistributionServiceTests
{
    private static PackageUploadResultDto Upload(DistFixture f, byte[] bytes, string name = "imsht.apk") =>
        ResultAssert.Ok<ApiSuccess<PackageUploadResultDto>>(
            f.Service().UploadPackageAsync(name, bytes, DistFixture.Admin, default).Result).Data!;

    private static string Check(PackageUploadResultDto r, string id) => r.Checks.Single(c => c.Id == id).Status;

    [Fact]
    public async Task A_valid_apk_is_stored_with_every_check_ok()
    {
        using var f = new DistFixture();

        var r = Upload(f, ApkBuilder.Build(versionCode: 10500, versionName: "1.5.0"));

        Assert.True(r.Ok);
        Assert.All(r.Checks, c => Assert.Equal("ok", c.Status));
        Assert.Equal(["health", "package", "version", "signature", "hash"], r.Checks.Select(c => c.Id));
        Assert.Equal(10500, r.Package!.VersionCode);
        Assert.DoesNotContain('/', r.Package.FileName);
        Assert.EndsWith(".apk", r.Package.FileName);
        Assert.True(f.Storage.Files.ContainsKey(r.Package.FileName));
        Assert.Equal(1, await f.T.Db.DistAudits.CountAsync(a => a.Action == "PACKAGE_ADD"));
    }

    [Fact]
    public void Another_apps_package_is_rejected_and_not_stored()
    {
        using var f = new DistFixture();

        var r = Upload(f, ApkBuilder.Build(package: "com.other.app"));

        Assert.False(r.Ok);
        Assert.Equal("ng", Check(r, "package"));
        Assert.Null(r.Package);
        Assert.Empty(f.Storage.Files);
    }

    [Fact]
    public void A_file_that_is_not_an_apk_fails_health_and_skips_the_rest()
    {
        using var f = new DistFixture();

        var r = Upload(f, "hello"u8.ToArray(), "notes.txt");

        Assert.False(r.Ok);
        Assert.Equal("ng", Check(r, "health"));
        Assert.Equal("na", Check(r, "package"));
    }

    [Fact]
    public async Task A_file_over_the_upload_limit_fails_health()
    {
        using var f = new DistFixture();
        await f.Service().SaveSettingsAsync(new DistSettingsDto("com.imssolutions.ht", 1, 3, 14, 3, true), DistFixture.Admin, default);

        var r = Upload(f, ApkBuilder.Build(padding: 1_100_000));

        Assert.Equal("ng", Check(r, "health"));
    }

    [Fact]
    public async Task Uploading_the_same_bytes_twice_returns_the_existing_package()
    {
        using var f = new DistFixture();
        var bytes = ApkBuilder.Build();

        var first = Upload(f, bytes).Package!;
        var second = Upload(f, bytes).Package!;

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await f.T.Db.DistPackages.CountAsync());
    }

    [Fact]
    public async Task A_different_signing_key_from_the_active_apk_is_rejected()
    {
        using var f = new DistFixture();
        await f.DistributeAsync(await f.UploadAsync(10400, "1.4.0"));

        var r = Upload(f, ApkBuilder.Build(versionCode: 10500, cert: ApkBuilder.DebugCert));

        Assert.False(r.Ok);
        Assert.Equal("ng", Check(r, "signature"));
    }

    [Fact]
    public async Task An_unsigned_apk_is_rejected()
    {
        using var f = new DistFixture();

        var r = Upload(f, ApkBuilder.Build(scheme: null));

        Assert.Equal("ng", Check(r, "signature"));
        Assert.False(r.Ok);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task A_version_not_above_the_active_one_only_warns()
    {
        using var f = new DistFixture();
        await f.DistributeAsync(await f.UploadAsync(10400, "1.4.0"));

        var r = Upload(f, ApkBuilder.Build(versionCode: 10400, versionName: "1.4.0-b", padding: 3));

        Assert.True(r.Ok);
        Assert.Equal("warn", Check(r, "version"));
    }

    [Fact]
    public async Task Distributing_now_activates_the_rule_and_supersedes_the_previous_one()
    {
        using var f = new DistFixture();
        var r1 = await f.DistributeAsync(await f.UploadAsync(10400, "1.4.0"));
        f.Clock.Advance(TimeSpan.FromMinutes(5));

        var r2 = await f.DistributeAsync(await f.UploadAsync(10500, "1.5.0"));

        Assert.Equal("Active", r2.State);
        Assert.Equal("Superseded", (await f.RuleAsync(r1.Id)).State);
        Assert.Equal(["stored", "verify", "publish"], r2.DeploySteps);
        Assert.Equal(1, await f.T.Db.DistRules.CountAsync(r => r.State == DistRuleState.Active));
    }

    [Fact]
    public async Task A_future_rule_stays_scheduled_until_its_time()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();
        var at = f.Clock.Local.AddHours(2);

        var rule = await f.DistributeAsync(p, new RuleWriteDto(p.Id, DeliverAt: at));
        Assert.Equal("Scheduled", rule.State);
        Assert.Empty((await f.Delivery().VersionJsonAsync("HT1", "10400", default)).Body.Apps);

        f.Clock.Advance(TimeSpan.FromHours(2));
        Assert.True(await f.Service().TickAsync(default));

        Assert.Equal("Active", (await f.RuleAsync(rule.Id)).State);
    }

    [Fact]
    public async Task An_end_date_not_after_the_delivery_date_is_VAL_006()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();

        var r = await f.Service().CreateRuleAsync(
            new RuleWriteDto(p.Id, DeliverAt: f.Clock.Local.AddHours(1), EndAt: f.Clock.Local.AddHours(1)),
            DistFixture.Admin, default);

        ResultAssert.BadRequest(r, "VAL-006");
    }

    [Fact]
    public async Task A_rule_that_targets_no_device_is_refused()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();

        var r = await f.Service().CreateRuleAsync(new RuleWriteDto(p.Id, Models: ["NOPE"]), DistFixture.Admin, default);

        ResultAssert.BadRequest(r, "VAL-001");
        Assert.Equal(0, await f.T.Db.DistRules.CountAsync());
    }

    [Fact]
    public async Task An_unknown_package_is_SYS_404()
    {
        using var f = new DistFixture();

        var r = await f.Service().CreateRuleAsync(new RuleWriteDto(99), DistFixture.Admin, default);

        ResultAssert.NotFound(r);
    }

    [Fact]
    public async Task Targets_are_model_AND_location_plus_include_minus_exclude_active_handhelds_only()
    {
        using var f = new DistFixture();

        var r = await f.Service().PreviewTargetsAsync(
            new TargetQueryDto(Models: ["MT93"], Locations: ["W1_ZA"], Include: ["HT3"], Exclude: ["HT1"]), default);
        var all = await f.Service().PreviewTargetsAsync(new TargetQueryDto(), default);

        Assert.Equal(["HT3"], ResultAssert.Ok<ApiListSuccess<DistDeviceDto>>(r).Data.Select(d => d.DeviceCode));
        Assert.Equal(["HT1", "HT2", "HT3"], ResultAssert.Ok<ApiListSuccess<DistDeviceDto>>(all).Data.Select(d => d.DeviceCode));
    }

    [Fact]
    public async Task Device_attributes_list_distinct_models_and_locations_of_handhelds()
    {
        using var f = new DistFixture();

        var r = ResultAssert.Ok<ApiSuccess<DeviceAttributesDto>>(await f.Service().AttributesAsync(default)).Data!;

        Assert.Equal(["MT93", "MT95"], r.Models);
        Assert.Equal(["W1", "W1_ZA"], r.Locations);
    }

    [Fact]
    public async Task A_stored_file_that_no_longer_matches_its_hash_puts_the_rule_in_error_and_retry_recovers()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();
        var good = f.Storage.Files[p.FileName];
        f.Storage.Files[p.FileName] = [1, 2, 3];

        var rule = await f.DistributeAsync(p);
        Assert.Equal("Error", rule.State);
        Assert.Equal("verify", rule.Error);
        Assert.Empty((await f.Delivery().VersionJsonAsync("HT1", "1", default)).Body.Apps);

        f.Storage.Files[p.FileName] = good;
        var retried = await f.Service().RetryRuleAsync(rule.Id, DistFixture.Admin, default);
        ResultAssert.Ok<ApiSuccess<RuleDto>>(retried);
        Assert.Equal("Active", (await f.RuleAsync(rule.Id)).State);
    }

    [Fact]
    public async Task Stop_and_resume_follow_the_state_model_and_resume_republishes()
    {
        using var f = new DistFixture();
        var rule = await f.DistributeAsync(await f.UploadAsync());
        var published = rule.PublishedAt;

        ResultAssert.Ok<ApiSuccess<RuleDto>>(await f.Service().StopRuleAsync(rule.Id, DistFixture.Admin, default));
        Assert.Empty((await f.Delivery().VersionJsonAsync("HT1", "1", default)).Body.Apps);
        ResultAssert.Error(await f.Service().StopRuleAsync(rule.Id, DistFixture.Admin, default), 409, "STS-002");

        f.Clock.Advance(TimeSpan.FromMinutes(1));
        ResultAssert.Ok<ApiSuccess<RuleDto>>(await f.Service().ResumeRuleAsync(rule.Id, DistFixture.Admin, default));
        var after = await f.RuleAsync(rule.Id);
        Assert.Equal("Active", after.State);
        Assert.True(after.PublishedAt > published);
        ResultAssert.Error(await f.Service().RetryRuleAsync(rule.Id, DistFixture.Admin, default), 409, "STS-002");
    }

    [Fact]
    public async Task A_rule_past_its_end_date_stops_by_itself()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();
        var rule = await f.DistributeAsync(p, new RuleWriteDto(p.Id, EndAt: f.Clock.Local.AddDays(1)));

        f.Clock.Advance(TimeSpan.FromDays(1));
        await f.Service().TickAsync(default);

        var after = await f.RuleAsync(rule.Id);
        Assert.Equal("Stopped", after.State);
        Assert.Equal("ended", after.StopReason);
    }

    [Fact]
    public async Task Published_dates_only_ever_increase()
    {
        using var f = new DistFixture();
        var a = await f.DistributeAsync(await f.UploadAsync(10400, "1.4.0"));
        var b = await f.DistributeAsync(await f.UploadAsync(10500, "1.5.0"));

        Assert.True(b.PublishedAt > a.PublishedAt);
    }

    [Fact]
    public async Task A_package_used_by_an_active_rule_cannot_be_deleted()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();
        await f.DistributeAsync(p);

        ResultAssert.InUse(await f.Service().DeletePackageAsync(p.Id, DistFixture.Admin, default));
    }

    [Fact]
    public async Task An_unused_package_is_deleted_with_its_file()
    {
        using var f = new DistFixture();
        var p = await f.UploadAsync();

        ResultAssert.NoContent(await f.Service().DeletePackageAsync(p.Id, DistFixture.Admin, default));

        Assert.Empty(f.Storage.Files);
        var list = ResultAssert.Ok<ApiListSuccess<PackageDto>>(await f.Service().ListPackagesAsync(default)).Data;
        Assert.Empty(list);
    }

    [Fact]
    public async Task Only_the_configured_number_of_old_versions_is_kept()
    {
        using var f = new DistFixture();
        await f.Service().SaveSettingsAsync(new DistSettingsDto("com.imssolutions.ht", 150, 1, 14, 3, true), DistFixture.Admin, default);
        var p1 = await f.UploadAsync(10100, "1.1.0");
        await f.DistributeAsync(p1);
        var p2 = await f.UploadAsync(10200, "1.2.0");
        await f.DistributeAsync(p2);
        var p3 = await f.UploadAsync(10300, "1.3.0");
        await f.DistributeAsync(p3);

        var left = ResultAssert.Ok<ApiListSuccess<PackageDto>>(await f.Service().ListPackagesAsync(default)).Data;

        Assert.Equal([10300, 10200], left.Select(p => p.VersionCode));
        Assert.False(f.Storage.Files.ContainsKey(p1.FileName));
    }

    [Fact]
    public async Task Settings_round_trip_and_reject_non_positive_numbers()
    {
        using var f = new DistFixture();

        var defaults = ResultAssert.Ok<ApiSuccess<DistSettingsDto>>(await f.Service().GetSettingsAsync(default)).Data!;
        Assert.Equal(new DistSettingsDto("com.imssolutions.ht", 150, 3, 14, 3, true), defaults);

        ResultAssert.BadRequest(await f.Service().SaveSettingsAsync(defaults with { NDays = 0 }, DistFixture.Admin, default), "QTY-003");
        ResultAssert.Ok<ApiSuccess<DistSettingsDto>>(await f.Service().SaveSettingsAsync(defaults with { MDays = 5 }, DistFixture.Admin, default));
        Assert.Equal(5, ResultAssert.Ok<ApiSuccess<DistSettingsDto>>(await f.Service().GetSettingsAsync(default)).Data!.MDays);
    }

    [Fact]
    public async Task Deleting_a_rule_hides_it_and_keeps_the_audit()
    {
        using var f = new DistFixture();
        var rule = await f.DistributeAsync(await f.UploadAsync());

        ResultAssert.NoContent(await f.Service().DeleteRuleAsync(rule.Id, DistFixture.Admin, default));

        Assert.Empty(ResultAssert.Ok<ApiListSuccess<RuleDto>>(await f.Service().ListRulesAsync(default)).Data);
        Assert.True(await f.T.Db.DistAudits.AnyAsync(a => a.Action == "RULE_DELETE"));
        ResultAssert.NotFound(await f.Service().GetRuleAsync(rule.Id, default));
    }
}
