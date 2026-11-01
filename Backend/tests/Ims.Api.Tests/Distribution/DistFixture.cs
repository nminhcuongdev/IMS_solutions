using System.Security.Claims;
using Ims.Api.Contracts;
using Ims.Api.Features.Distribution;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;

namespace Ims.Api.Tests.Distribution;

internal sealed class DistFixture : IDisposable
{
    public TestDb T { get; } = TestDb.Seeded();
    public FakeClock Clock { get; } = new(new DateTime(2026, 10, 1, 9, 0, 0));
    public MemoryDistStorage Storage { get; } = new();

    public static ClaimsPrincipal Admin => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "admin")], "test"));

    public DistFixture()
    {
        Handheld("HT1", "MT93", "W1_ZA");
        Handheld("HT2", "MT93", "W1");
        Handheld("HT3", "MT95", "W1_ZA");
        Handheld("HT9", "MT93", "W1_ZA", active: false);
        T.Db.Devices.Add(new Device { DeviceCode = "PRN", DeviceName = "Printer", DeviceType = DeviceType.Printer,
            Model = "MT93", LocationCode = "W1_ZA", CreatedAt = Clock.Local });
        T.Db.SaveChanges();
        T.Db.ChangeTracker.Clear();
    }

    private void Handheld(string code, string model, string location, bool active = true) =>
        T.Db.Devices.Add(new Device { DeviceCode = code, DeviceName = code + " name", DeviceType = DeviceType.Handheld,
            Model = model, LocationCode = location, IsActive = active, CreatedAt = Clock.Local });

    public DistributionService Service() => new(T.Db, Storage, Clock);

    public DistributionDeliveryService Delivery() => new(T.Db, Storage, Clock, Service());

    public async Task<PackageDto> UploadAsync(int versionCode = 10500, string versionName = "1.5.0",
        byte[]? cert = null, int padding = 0)
    {
        var bytes = ApkBuilder.Build(versionCode: versionCode, versionName: versionName, cert: cert, padding: padding);
        var r = await Service().UploadPackageAsync($"imsht-{versionName}.apk", bytes, Admin, default);
        var body = ResultAssert.Ok<ApiSuccess<PackageUploadResultDto>>(r).Data!;
        return body.Package ?? throw new InvalidOperationException(string.Join(",", body.Checks.Select(c => c.Id + ":" + c.Status)));
    }

    public async Task<RuleDto> DistributeAsync(PackageDto p, RuleWriteDto? body = null)
    {
        var r = await Service().CreateRuleAsync(body ?? new RuleWriteDto(p.Id), Admin, default);
        return ResultAssert.Created<ApiSuccess<RuleDto>>(r).Data!;
    }

    public async Task<RuleDto> RuleAsync(int id)
    {
        var r = await Service().GetRuleAsync(id, default);
        return ResultAssert.Ok<ApiSuccess<RuleDetailDto>>(r).Data!.Rule;
    }

    public async Task<IReadOnlyList<DistDeviceDto>> DevicesAsync()
    {
        var r = await Service().ListDevicesAsync(null, default);
        return ResultAssert.Ok<ApiListSuccess<DistDeviceDto>>(r).Data;
    }

    public void Dispose() => T.Dispose();
}

internal sealed class FakeClock(DateTime local) : TimeProvider
{
    public DateTime Local { get; private set; } = local;

    public void Advance(TimeSpan by) => Local += by;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

    public override DateTimeOffset GetUtcNow() =>
        new DateTimeOffset(DateTime.SpecifyKind(Local, DateTimeKind.Unspecified), TimeZoneInfo.Local.GetUtcOffset(Local))
            .ToUniversalTime();
}

internal sealed class MemoryDistStorage : IDistStorage
{
    public Dictionary<string, byte[]> Files { get; } = new();

    public Task SaveAsync(string fileName, byte[] bytes, CancellationToken ct)
    {
        Files[fileName] = bytes;
        return Task.CompletedTask;
    }

    public Stream? OpenRead(string fileName) =>
        Files.TryGetValue(fileName, out var b) ? new MemoryStream(b, false) : null;

    public void Delete(string fileName) => Files.Remove(fileName);
}
