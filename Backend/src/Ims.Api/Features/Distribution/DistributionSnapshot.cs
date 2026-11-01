using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ims.Domain.Distribution;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Distribution;

internal sealed class DistributionSnapshot
{
    public const string Latest = "latest";
    public const string Downloaded = "downloaded";
    public const string Waiting = "waiting";
    public const string Attention = "attention";
    public const string Unconfirmed = "unconfirmed";
    public const string NotTarget = "notTarget";

    public required IReadOnlyList<Device> Handhelds { get; init; }
    public required DistRule? ActiveRule { get; init; }
    public required IReadOnlyDictionary<string, DistDeviceState> States { get; init; }
    public required IReadOnlyDictionary<string, DateTime> ActiveDownloads { get; init; }
    public required IReadOnlyDictionary<int, string> VersionNames { get; init; }
    public required DistSettingsDto Settings { get; init; }
    public required DateTime Now { get; init; }

    public static async Task<DistributionSnapshot> LoadAsync(ImsDbContext db, DistSettingsDto settings, DateTime now, CancellationToken ct)
    {
        var handhelds = await db.Devices.AsNoTracking()
            .Where(d => !d.IsDeleted && d.DeviceType == DeviceType.Handheld)
            .OrderBy(d => d.DeviceCode).ToListAsync(ct);
        var active = await db.DistRules.AsNoTracking().Include(r => r.Package)
            .Where(r => !r.IsDeleted && r.State == DistRuleState.Active)
            .OrderByDescending(r => r.ActivatedAt).FirstOrDefaultAsync(ct);
        var states = await db.DistDeviceStates.AsNoTracking().ToDictionaryAsync(s => s.DeviceCode, ct);
        var downloads = active is null
            ? new Dictionary<string, DateTime>()
            : await db.DistDownloads.AsNoTracking().Where(x => x.RuleId == active.Id)
                .ToDictionaryAsync(x => x.DeviceCode, x => x.CompletedAt, ct);
        var names = (await db.DistPackages.AsNoTracking()
                .Select(p => new { p.VersionCode, p.VersionName, p.Id }).ToListAsync(ct))
            .GroupBy(p => p.VersionCode)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Id).First().VersionName);
        return new DistributionSnapshot
        {
            Handhelds = handhelds, ActiveRule = active, States = states, ActiveDownloads = downloads,
            VersionNames = names, Settings = settings, Now = now,
        };
    }

    public static string[] ReadList(string json) =>
        string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<string[]>(json) ?? [];

    public static string WriteList(IEnumerable<string>? values) =>
        JsonSerializer.Serialize((values ?? []).Select(v => v.Trim()).Where(v => v.Length > 0).Distinct().ToArray());

    public IReadOnlyList<Device> Resolve(string[] models, string[] locations, string[] include, string[] exclude)
    {
        var live = Handhelds.Where(d => d.IsActive).ToList();
        var set = live.Where(d => (models.Length == 0 || models.Contains(d.Model ?? ""))
                                 && (locations.Length == 0 || locations.Contains(d.LocationCode ?? "")))
            .Select(d => d.DeviceCode).ToHashSet();
        foreach (var code in include.Where(c => live.Any(d => d.DeviceCode == c))) set.Add(code);
        set.ExceptWith(exclude);
        return live.Where(d => set.Contains(d.DeviceCode)).ToList();
    }

    public IReadOnlyList<Device> TargetsOf(DistRule rule)
    {
        var live = Resolve(ReadList(rule.Models), ReadList(rule.Locations),
            ReadList(rule.IncludeDevices), ReadList(rule.ExcludeDevices));
        if (rule.AutoAdd || rule.State != DistRuleState.Active) return live;
        var frozen = ReadList(rule.StaticTargets).ToHashSet();
        return live.Where(d => frozen.Contains(d.DeviceCode)).ToList();
    }

    private HashSet<string>? _activeTargets;

    public bool IsActiveTarget(string deviceCode) =>
        ActiveRule is not null
        && (_activeTargets ??= TargetsOf(ActiveRule).Select(d => d.DeviceCode).ToHashSet()).Contains(deviceCode);

    public string StatusOf(string deviceCode)
    {
        if (ActiveRule is null || !IsActiveTarget(deviceCode)) return NotTarget;
        States.TryGetValue(deviceCode, out var s);
        if (s?.LastCheckAt is not { } check || Now - check >= TimeSpan.FromDays(Settings.NDays)) return Unconfirmed;
        if ((s.VersionCode ?? 0) >= ActiveRule.Package.VersionCode) return Latest;
        if (ActiveDownloads.TryGetValue(deviceCode, out var dl))
            return Now - dl >= TimeSpan.FromDays(Settings.MDays) ? Attention : Downloaded;
        return Waiting;
    }

    public string? VersionNameOf(int? versionCode) =>
        versionCode is { } v && VersionNames.TryGetValue(v, out var n) ? n : null;
}
