using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ims.Domain.Distribution;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Distribution;

internal sealed class DistributionDeliveryService(
    ImsDbContext db, IDistStorage storage, TimeProvider clock, IDistributionService distribution)
    : IDistributionDeliveryService
{
    private const int HistoryKeep = 50;

    private DateTime Now => clock.GetLocalNow().DateTime;

    public static VersionEntryDto Entry(DistRule rule, TimeProvider clock)
    {
        var published = rule.PublishedAt ?? rule.DeliverAt;
        var offset = clock.LocalTimeZone.GetUtcOffset(published);
        var notes = new Dictionary<string, string>();
        if (rule.NoteJa is { Length: > 0 } ja) notes["ja"] = ja;
        if (rule.NoteEn is { Length: > 0 } en) notes["en"] = en;
        if (rule.NoteVi is { Length: > 0 } vi) notes["vi"] = vi;
        return new VersionEntryDto(rule.Package.PackageName, rule.Package.VersionCode, rule.Package.VersionName,
            new DateTimeOffset(DateTime.SpecifyKind(published, DateTimeKind.Unspecified), offset)
                .ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
            rule.Package.FileName, rule.Package.Sha256, rule.MinVersionCode, rule.Force ? true : null,
            notes.Count > 0 ? notes : null);
    }

    public async Task<VersionJsonResponse> VersionJsonAsync(string? deviceId, string? appVersion, CancellationToken ct)
    {
        await distribution.TickAsync(ct);
        var now = Now;
        var snap = await DistributionSnapshot.LoadAsync(db, await DistSettingsStore.ReadAsync(db, ct), now, ct);
        var lastModified = snap.ActiveRule?.PublishedAt;
        var code = deviceId?.Trim() ?? "";
        var device = snap.Handhelds.FirstOrDefault(d => d.DeviceCode == code);
        if (device is null)
        {
            if (code.Length > 0) await RecordUnknownAsync(code.Length > 50 ? code[..50] : code, now, ct);
            return new(new VersionJsonDto([]), lastModified);
        }

        var version = int.TryParse(appVersion?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : (int?)null;
        var state = await db.DistDeviceStates.FirstOrDefaultAsync(s => s.DeviceCode == code, ct);
        if (state is null) db.DistDeviceStates.Add(state = new DistDeviceState { DeviceCode = code });
        state.LastCheckAt = now;
        if (version is { } ver)
        {
            if (state.VersionCode is { } prev && ver > prev) state.LastUpdateAt = now;
            state.VersionCode = ver;
            db.DistChecks.Add(new DistCheck { DeviceCode = code, ReceivedAt = now, AppVersion = ver });
        }
        await db.SaveChangesAsync(ct);
        await TrimHistoryAsync(code, ct);

        var apps = snap.ActiveRule is { } rule && snap.IsActiveTarget(code) ? new[] { Entry(rule, clock) } : [];
        return new(new VersionJsonDto(apps), lastModified);
    }

    private async Task RecordUnknownAsync(string code, DateTime now, CancellationToken ct)
    {
        var u = await db.DistUnknownDevices.FirstOrDefaultAsync(x => x.DeviceCode == code, ct);
        if (u is null) db.DistUnknownDevices.Add(u = new DistUnknownDevice { DeviceCode = code, FirstAt = now });
        u.LastAt = now;
        u.RequestCount++;
        await db.SaveChangesAsync(ct);
    }

    private async Task TrimHistoryAsync(string code, CancellationToken ct)
    {
        var stale = await db.DistChecks.Where(c => c.DeviceCode == code)
            .OrderByDescending(c => c.ReceivedAt).ThenByDescending(c => c.Id).Skip(HistoryKeep).ToListAsync(ct);
        if (stale.Count == 0) return;
        db.DistChecks.RemoveRange(stale);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ApkSource?> FindApkAsync(string fileName, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            return null;
        var p = await db.DistPackages.AsNoTracking().FirstOrDefaultAsync(x => x.FileName == fileName && !x.IsDeleted, ct);
        return p is null ? null : new ApkSource(p.Id, p.FileName, p.SizeBytes);
    }

    public Stream OpenApk(ApkSource source) =>
        storage.OpenRead(source.FileName) ?? throw new FileNotFoundException(source.FileName);

    public async Task RecordDownloadAsync(string? deviceId, ApkSource source, CancellationToken ct)
    {
        var code = deviceId?.Trim() ?? "";
        if (code.Length == 0) return;
        if (!await db.Devices.AnyAsync(d => d.DeviceCode == code && !d.IsDeleted, ct)) return;
        var rule = await db.DistRules.AsNoTracking()
            .Where(r => r.PackageId == source.PackageId && !r.IsDeleted)
            .OrderByDescending(r => r.State == DistRuleState.Active).ThenByDescending(r => r.ActivatedAt)
            .FirstOrDefaultAsync(ct);
        if (rule is null) return;
        var row = await db.DistDownloads.FirstOrDefaultAsync(x => x.DeviceCode == code && x.RuleId == rule.Id, ct);
        if (row is null) db.DistDownloads.Add(new DistDownload { DeviceCode = code, RuleId = rule.Id, CompletedAt = Now });
        else row.CompletedAt = Now;
        await db.SaveChangesAsync(ct);
    }
}
