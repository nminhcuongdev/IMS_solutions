using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Devices;
using Ims.Domain.Distribution;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Distribution;

internal sealed class DistributionService(ImsDbContext db, IDistStorage storage, TimeProvider clock)
    : IDistributionService, IDeviceUpdateStatus
{
    private const int NoteMax = 600;
    private static readonly DistRuleState[] Live = [DistRuleState.Scheduled, DistRuleState.Active];

    private DateTime Now => clock.GetLocalNow().DateTime;

    private static string Who(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value is { Length: > 0 } c ? c : "system";

    private void Audit(string who, string action, string? target) =>
        db.DistAudits.Add(new DistAudit { At = Now, Who = who.Length > 20 ? who[..20] : who, Action = action, Target = target });

    private async Task<DistributionSnapshot> SnapshotAsync(CancellationToken ct) =>
        await DistributionSnapshot.LoadAsync(db, await DistSettingsStore.ReadAsync(db, ct), Now, ct);

    public async Task<ServiceResult> UploadPackageAsync(string fileName, byte[] bytes, ClaimsPrincipal user, CancellationToken ct)
    {
        await TickAsync(ct);
        var settings = await DistSettingsStore.ReadAsync(db, ct);
        var checks = new List<InspectionCheckDto>();
        var limit = (long)settings.UploadMb * 1024 * 1024;
        var read = bytes.Length == 0 || bytes.Length > limit ? null : ApkReader.Read(bytes);
        if (read is null || read.Failure is "zip" or "manifest")
        {
            var why = bytes.Length == 0 ? "empty" : bytes.Length > limit ? "size" : read!.Failure;
            checks.Add(new("health", "ng", why));
            checks.AddRange(new[] { "package", "version", "signature", "hash" }.Select(id => new InspectionCheckDto(id, "na", null)));
            return Outcome.Ok(new ApiSuccess<PackageUploadResultDto>(new(false, checks, null)));
        }

        var active = await db.DistRules.AsNoTracking().Include(r => r.Package)
            .Where(r => !r.IsDeleted && r.State == DistRuleState.Active)
            .OrderByDescending(r => r.ActivatedAt).Select(r => r.Package).FirstOrDefaultAsync(ct);
        var sha = Convert.ToHexStringLower(SHA256.HashData(bytes));
        checks.Add(new("health", "ok", null));
        checks.Add(new("package", read.PackageName == settings.PackageName ? "ok" : "ng", read.PackageName));
        checks.Add(active is not null && read.VersionCode <= active.VersionCode
            ? new("version", "warn", $"{read.VersionName} ({read.VersionCode}) <= {active.VersionName} ({active.VersionCode})")
            : new("version", "ok", $"{read.VersionName} ({read.VersionCode})"));
        checks.Add(read.SignerFingerprint is null
            ? new("signature", "ng", "unsigned")
            : active is not null && active.SignerFingerprint != read.SignerFingerprint
                ? new("signature", "ng", read.SignerFingerprint)
                : new("signature", "ok", read.SignerFingerprint));
        checks.Add(new("hash", "ok", sha));
        if (checks.Any(c => c.Status == "ng"))
            return Outcome.Ok(new ApiSuccess<PackageUploadResultDto>(new(false, checks, null)));

        var existing = await db.DistPackages.FirstOrDefaultAsync(p => !p.IsDeleted && p.Sha256 == sha, ct);
        if (existing is null)
        {
            existing = new DistPackage
            {
                PackageName = read.PackageName!, VersionCode = read.VersionCode, VersionName = Clip(read.VersionName!, 100),
                FileName = StoredName(read.PackageName!, read.VersionName!, sha),
                OriginalFileName = Clip(Path.GetFileName(fileName), 260), SizeBytes = bytes.Length, Sha256 = sha,
                SignerFingerprint = read.SignerFingerprint!, CreatedBy = Who(user), CreatedAt = Now,
            };
            await storage.SaveAsync(existing.FileName, bytes, ct);
            db.DistPackages.Add(existing);
            Audit(Who(user), "PACKAGE_ADD", existing.FileName);
            await db.SaveChangesAsync(ct);
        }
        return Outcome.Ok(new ApiSuccess<PackageUploadResultDto>(new(true, checks, MapPackage(existing, false))));
    }

    private static string Clip(string s, int max) => s.Length > max ? s[..max] : s;

    private static string StoredName(string package, string versionName, string sha)
    {
        static string Safe(string s) => new(s.Select(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '_').ToArray());
        var app = Safe(package.Split('.').Last());
        return Clip($"{app}-{Safe(versionName)}-{sha[..8]}", 190) + ".apk";
    }

    private static PackageDto MapPackage(DistPackage p, bool inUse) => new(
        p.Id, p.PackageName, p.VersionCode, p.VersionName, p.FileName, p.OriginalFileName, p.SizeBytes,
        p.Sha256, p.SignerFingerprint, p.CreatedBy, p.CreatedAt, inUse);

    public async Task<ServiceResult> ListPackagesAsync(CancellationToken ct)
    {
        var inUse = await db.DistRules.Where(r => !r.IsDeleted && Live.Contains(r.State))
            .Select(r => r.PackageId).Distinct().ToListAsync(ct);
        var rows = (await db.DistPackages.AsNoTracking().Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.VersionCode).ThenByDescending(p => p.Id).ToListAsync(ct))
            .Select(p => MapPackage(p, inUse.Contains(p.Id))).ToList();
        return Outcome.Ok(new ApiListSuccess<PackageDto>(rows, new PageMeta(rows.Count, 1, Math.Max(1, rows.Count))));
    }

    public async Task<ServiceResult> DeletePackageAsync(int id, ClaimsPrincipal user, CancellationToken ct)
    {
        var p = await db.DistPackages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (p is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("APK")));
        if (await db.DistRules.AnyAsync(r => r.PackageId == id && !r.IsDeleted && Live.Contains(r.State), ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Del001()));
        p.IsDeleted = true;
        storage.Delete(p.FileName);
        Audit(Who(user), "PACKAGE_DELETE", p.FileName);
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> AttributesAsync(CancellationToken ct)
    {
        var snap = await SnapshotAsync(ct);
        var live = snap.Handhelds.Where(d => d.IsActive).ToList();
        string[] Distinct(IEnumerable<string?> v) =>
            v.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).Distinct().Order(StringComparer.Ordinal).ToArray();
        return Outcome.Ok(new ApiSuccess<DeviceAttributesDto>(
            new(Distinct(live.Select(d => d.Model)), Distinct(live.Select(d => d.LocationCode)))));
    }

    public async Task<ServiceResult> PreviewTargetsAsync(TargetQueryDto body, CancellationToken ct)
    {
        var snap = await SnapshotAsync(ct);
        var rows = snap.Resolve(body.Models ?? [], body.Locations ?? [], body.Include ?? [], body.Exclude ?? [])
            .Select(d => MapDevice(snap, d.DeviceCode)).ToList();
        return Outcome.Ok(new ApiListSuccess<DistDeviceDto>(rows, new PageMeta(rows.Count, 1, Math.Max(1, rows.Count))));
    }

    public async Task<ServiceResult> CreateRuleAsync(RuleWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var p = await db.DistPackages.FirstOrDefaultAsync(x => x.Id == body.PackageId && !x.IsDeleted, ct);
        if (p is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("APK")));
        if (body.MinVersionCode is < 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));
        if (new[] { body.NoteJa, body.NoteEn, body.NoteVi }.Any(n => n is { Length: > NoteMax }))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005FieldTooLong()));
        var now = Now;
        var deliver = body.DeliverAt is { } at && at > now ? at : now;
        if (body.EndAt is { } end && end <= deliver) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val006()));

        var snap = await SnapshotAsync(ct);
        var targets = snap.Resolve(body.Models ?? [], body.Locations ?? [], body.Include ?? [], body.Exclude ?? []);
        if (targets.Count == 0)
            return Outcome.BadRequest(new ApiError(new ApiErrorBody("VAL-001", 400, "Error", "配信対象が0台です。対象を選択してください。")));

        var rule = new DistRule
        {
            PackageId = p.Id,
            Models = DistributionSnapshot.WriteList(body.Models),
            Locations = DistributionSnapshot.WriteList(body.Locations),
            IncludeDevices = DistributionSnapshot.WriteList(body.Include),
            ExcludeDevices = DistributionSnapshot.WriteList(body.Exclude),
            AutoAdd = snap.Settings.AutoAdd, DeliverAt = deliver, EndAt = body.EndAt, State = DistRuleState.Scheduled,
            Force = body.Force, MinVersionCode = body.MinVersionCode is > 0 ? body.MinVersionCode : null,
            NoteJa = NullIfBlank(body.NoteJa), NoteEn = NullIfBlank(body.NoteEn), NoteVi = NullIfBlank(body.NoteVi),
            CreatedBy = Who(user), CreatedAt = now,
        };
        db.DistRules.Add(rule);
        await db.SaveChangesAsync(ct);
        Audit(Who(user), "RULE_CREATE", $"R-{rule.Id:0000} ({p.VersionName})");
        await db.SaveChangesAsync(ct);
        await TickAsync(ct);
        return Outcome.Created($"/distribution/rules/{rule.Id}", new ApiSuccess<RuleDto>(await RuleDtoAsync(rule.Id, ct)));
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async Task<RuleDto> RuleDtoAsync(int id, CancellationToken ct)
    {
        var snap = await SnapshotAsync(ct);
        var rule = await db.DistRules.AsNoTracking().Include(r => r.Package).FirstAsync(r => r.Id == id, ct);
        return MapRule(snap, rule);
    }

    private static RuleDto MapRule(DistributionSnapshot snap, DistRule r)
    {
        var targets = r.State == DistRuleState.Active || r.State == DistRuleState.Scheduled
            ? snap.TargetsOf(r).Select(d => d.DeviceCode).ToList()
            : DistributionSnapshot.ReadList(r.StaticTargets).ToList();
        var counts = r.State == DistRuleState.Active && snap.ActiveRule?.Id == r.Id
            ? targets.Select(snap.StatusOf).GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count())
            : new Dictionary<string, int>();
        int C(string k) => counts.GetValueOrDefault(k);
        return new RuleDto(r.Id, r.PackageId, r.Package.VersionName, r.Package.VersionCode, r.Package.FileName, r.Package.Sha256,
            DistributionSnapshot.ReadList(r.Models), DistributionSnapshot.ReadList(r.Locations),
            DistributionSnapshot.ReadList(r.IncludeDevices), DistributionSnapshot.ReadList(r.ExcludeDevices), r.AutoAdd,
            r.DeliverAt, r.EndAt, r.State.ToString(), r.Force, r.MinVersionCode, r.NoteJa, r.NoteEn, r.NoteVi,
            r.PublishedAt, r.ActivatedAt, r.Error, r.StopReason,
            string.IsNullOrEmpty(r.DeploySteps) ? [] : r.DeploySteps.Split(','), r.CreatedBy, r.CreatedAt,
            new RuleSummaryDto(targets.Count, C(DistributionSnapshot.Latest), C(DistributionSnapshot.Downloaded),
                C(DistributionSnapshot.Waiting), C(DistributionSnapshot.Attention), C(DistributionSnapshot.Unconfirmed)));
    }

    public async Task<ServiceResult> ListRulesAsync(CancellationToken ct)
    {
        await TickAsync(ct);
        var snap = await SnapshotAsync(ct);
        var rules = await db.DistRules.AsNoTracking().Include(r => r.Package).Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.DeliverAt).ThenByDescending(r => r.Id).ToListAsync(ct);
        var rows = rules.Select(r => MapRule(snap, r)).ToList();
        return Outcome.Ok(new ApiListSuccess<RuleDto>(rows, new PageMeta(rows.Count, 1, Math.Max(1, rows.Count))));
    }

    public async Task<ServiceResult> GetRuleAsync(int id, CancellationToken ct)
    {
        await TickAsync(ct);
        var rule = await db.DistRules.AsNoTracking().Include(r => r.Package).FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
        if (rule is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Rule")));
        var snap = await SnapshotAsync(ct);
        var dto = MapRule(snap, rule);
        var codes = rule.State is DistRuleState.Active or DistRuleState.Scheduled
            ? snap.TargetsOf(rule).Select(d => d.DeviceCode).ToList()
            : DistributionSnapshot.ReadList(rule.StaticTargets).ToList();
        var targets = codes.Where(c => snap.Handhelds.Any(d => d.DeviceCode == c))
            .Select(c => MapDevice(snap, c, rule.State == DistRuleState.Active)).ToList();
        var json = new VersionJsonDto([DistributionDeliveryService.Entry(rule, clock)]);
        return Outcome.Ok(new ApiSuccess<RuleDetailDto>(new(dto, targets, json)));
    }

    private async Task<ServiceResult> TransitionAsync(int id, ClaimsPrincipal user, CancellationToken ct,
        DistRuleState from, Action<DistRule> apply, string action)
    {
        await TickAsync(ct);
        var rule = await db.DistRules.Include(r => r.Package).FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
        if (rule is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Rule")));
        if (rule.State != from) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts002()));
        apply(rule);
        Audit(Who(user), action, $"R-{rule.Id:0000}");
        await db.SaveChangesAsync(ct);
        await TickAsync(ct);
        return Outcome.Ok(new ApiSuccess<RuleDto>(await RuleDtoAsync(rule.Id, ct)));
    }

    public Task<ServiceResult> StopRuleAsync(int id, ClaimsPrincipal user, CancellationToken ct) =>
        TransitionAsync(id, user, ct, DistRuleState.Active, r => { r.State = DistRuleState.Stopped; r.StopReason = "admin"; }, "RULE_STOP");

    public async Task<ServiceResult> ResumeRuleAsync(int id, ClaimsPrincipal user, CancellationToken ct)
    {
        var published = await LastPublishedAsync(ct);
        return await TransitionAsync(id, user, ct, DistRuleState.Stopped, r =>
        {
            SupersedeOthers(r);
            r.State = DistRuleState.Active;
            r.StopReason = null;
            r.EndAt = null;
            r.PublishedAt = NextPublished(Now, published);
        }, "RULE_RESUME");
    }

    public Task<ServiceResult> RetryRuleAsync(int id, ClaimsPrincipal user, CancellationToken ct) =>
        TransitionAsync(id, user, ct, DistRuleState.Error, r =>
        {
            r.State = DistRuleState.Scheduled;
            r.Error = null;
            if (r.DeliverAt > Now) r.DeliverAt = Now;
        }, "RULE_RETRY");

    public async Task<ServiceResult> DeleteRuleAsync(int id, ClaimsPrincipal user, CancellationToken ct)
    {
        var rule = await db.DistRules.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
        if (rule is null) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Rule")));
        rule.IsDeleted = true;
        Audit(Who(user), "RULE_DELETE", $"R-{rule.Id:0000}");
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    private DistDeviceDto MapDevice(DistributionSnapshot snap, string code, bool withStatus = true)
    {
        var d = snap.Handhelds.First(x => x.DeviceCode == code);
        snap.States.TryGetValue(code, out var s);
        var status = withStatus ? snap.StatusOf(code) : DistributionSnapshot.NotTarget;
        var target = status != DistributionSnapshot.NotTarget ? snap.ActiveRule?.Package : null;
        return new DistDeviceDto(d.DeviceCode, d.DeviceName, d.Model, d.LocationCode, d.IsActive,
            s?.VersionCode, snap.VersionNameOf(s?.VersionCode), s?.LastCheckAt, s?.LastUpdateAt,
            status != DistributionSnapshot.NotTarget && snap.ActiveDownloads.TryGetValue(code, out var dl) ? dl : null,
            status, target?.VersionCode, target?.VersionName);
    }

    public async Task<ServiceResult> ListDevicesAsync(string? status, CancellationToken ct)
    {
        await TickAsync(ct);
        var snap = await SnapshotAsync(ct);
        var rows = snap.Handhelds.Select(d => MapDevice(snap, d.DeviceCode))
            .Where(d => string.IsNullOrEmpty(status) || status == "all" || d.Status == status).ToList();
        return Outcome.Ok(new ApiListSuccess<DistDeviceDto>(rows, new PageMeta(rows.Count, 1, Math.Max(1, rows.Count))));
    }

    public async Task<IReadOnlyCollection<string>> DeviceCodesWithStatusAsync(string status, CancellationToken ct)
    {
        var snap = await SnapshotAsync(ct);
        return snap.Handhelds.Select(d => d.DeviceCode).Where(c => snap.StatusOf(c) == status).ToList();
    }

    public async Task<ServiceResult> DeviceHistoryAsync(string code, CancellationToken ct)
    {
        var rows = await db.DistChecks.AsNoTracking().Where(c => c.DeviceCode == code)
            .OrderByDescending(c => c.ReceivedAt).ThenByDescending(c => c.Id).Take(200)
            .Select(c => new DeviceCheckDto(c.ReceivedAt, c.AppVersion)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<DeviceCheckDto>(rows, new PageMeta(rows.Count, 1, Math.Max(1, rows.Count))));
    }

    public async Task<ServiceResult> ListUnknownAsync(CancellationToken ct)
    {
        var rows = await db.DistUnknownDevices.AsNoTracking().OrderByDescending(u => u.LastAt)
            .Select(u => new UnknownDeviceDto(u.DeviceCode, u.FirstAt, u.LastAt, u.RequestCount)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<UnknownDeviceDto>(rows, new PageMeta(rows.Count, 1, Math.Max(1, rows.Count))));
    }

    public async Task<ServiceResult> ListAuditAsync(CancellationToken ct)
    {
        var rows = await db.DistAudits.AsNoTracking().OrderByDescending(a => a.At).ThenByDescending(a => a.Id).Take(300)
            .Select(a => new AuditDto(a.At, a.Who, a.Action, a.Target)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<AuditDto>(rows, new PageMeta(rows.Count, 1, Math.Max(1, rows.Count))));
    }

    public async Task<ServiceResult> GetSettingsAsync(CancellationToken ct) =>
        Outcome.Ok(new ApiSuccess<DistSettingsDto>(await DistSettingsStore.ReadAsync(db, ct)));

    public async Task<ServiceResult> SaveSettingsAsync(DistSettingsDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.PackageName)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        if (body.PackageName.Trim().Length > 200) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005FieldTooLong()));
        if (body.UploadMb <= 0 || body.Retention <= 0 || body.NDays <= 0 || body.MDays <= 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty003()));
        await DistSettingsStore.WriteAsync(db, body, Who(user), Now, ct);
        Audit(Who(user), "SETTINGS_SAVE", null);
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<DistSettingsDto>(await DistSettingsStore.ReadAsync(db, ct)));
    }

    public async Task<ServiceResult> VersionJsonPreviewAsync(CancellationToken ct)
    {
        await TickAsync(ct);
        var active = await db.DistRules.AsNoTracking().Include(r => r.Package)
            .Where(r => !r.IsDeleted && r.State == DistRuleState.Active)
            .OrderByDescending(r => r.ActivatedAt).FirstOrDefaultAsync(ct);
        var body = new VersionJsonDto(active is null ? [] : [DistributionDeliveryService.Entry(active, clock)]);
        return Outcome.Ok(new ApiSuccess<VersionJsonDto>(body));
    }

    private async Task<DateTime?> LastPublishedAsync(CancellationToken ct) =>
        await db.DistRules.MaxAsync(r => r.PublishedAt, ct);

    private static DateTime NextPublished(DateTime at, DateTime? last)
    {
        var t = new DateTime(at.Ticks - at.Ticks % TimeSpan.TicksPerSecond, at.Kind);
        return last is { } l && t <= l ? l.AddSeconds(1) : t;
    }

    private void SupersedeOthers(DistRule rule)
    {
        foreach (var other in db.DistRules.Local.Where(r => r.Id != rule.Id && r.State == DistRuleState.Active && !r.IsDeleted))
        {
            other.State = DistRuleState.Superseded;
            Audit("system", "RULE_SUPERSEDED", $"R-{other.Id:0000}");
        }
    }

    public async Task<bool> TickAsync(CancellationToken ct)
    {
        var now = Now;
        var due = await db.DistRules.Include(r => r.Package)
            .Where(r => !r.IsDeleted && (r.State == DistRuleState.Scheduled && r.DeliverAt <= now
                                         || r.State == DistRuleState.Active && r.EndAt != null && r.EndAt <= now))
            .OrderBy(r => r.DeliverAt).ThenBy(r => r.Id).ToListAsync(ct);
        if (due.Count == 0) return false;
        await db.DistRules.Where(r => !r.IsDeleted && r.State == DistRuleState.Active).LoadAsync(ct);
        foreach (var rule in due)
        {
            if (rule.State == DistRuleState.Scheduled) await DeployAsync(rule, now, ct);
            if (rule.State == DistRuleState.Active && rule.EndAt is { } end && end <= now)
            {
                rule.State = DistRuleState.Stopped;
                rule.StopReason = "ended";
                Audit("system", "RULE_ENDED", $"R-{rule.Id:0000}");
            }
        }
        await db.SaveChangesAsync(ct);
        await PurgeOldPackagesAsync(ct);
        return true;
    }

    private async Task DeployAsync(DistRule rule, DateTime now, CancellationToken ct)
    {
        var steps = new List<string>();
        string? error = null;
        await using (var s = storage.OpenRead(rule.Package.FileName))
        {
            if (s is null) error = "missing";
            else
            {
                steps.Add("stored");
                var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(s, ct));
                if (hash != rule.Package.Sha256) error = "verify";
                else steps.Add("verify");
            }
        }
        if (error is not null)
        {
            rule.State = DistRuleState.Error;
            rule.Error = error;
            rule.DeploySteps = string.Join(',', steps);
            Audit("system", "RULE_ERROR", $"R-{rule.Id:0000}");
            return;
        }
        var snap = await SnapshotAsync(ct);
        SupersedeOthers(rule);
        steps.Add("publish");
        rule.State = DistRuleState.Active;
        rule.Error = null;
        rule.ActivatedAt = now;
        rule.PublishedAt = NextPublished(rule.DeliverAt, await LastPublishedAsync(ct));
        rule.StaticTargets = DistributionSnapshot.WriteList(snap.TargetsOf(rule).Select(d => d.DeviceCode));
        rule.DeploySteps = string.Join(',', steps);
        Audit("system", "RULE_ACTIVE", $"R-{rule.Id:0000}");
        await db.SaveChangesAsync(ct);
    }

    private async Task PurgeOldPackagesAsync(CancellationToken ct)
    {
        var keep = (await DistSettingsStore.ReadAsync(db, ct)).Retention;
        var used = await db.DistRules.Where(r => !r.IsDeleted && r.State != DistRuleState.Superseded)
            .Select(r => r.PackageId).Distinct().ToListAsync(ct);
        var spare = await db.DistPackages.Where(p => !p.IsDeleted && !used.Contains(p.Id))
            .OrderByDescending(p => p.VersionCode).ThenByDescending(p => p.Id).ToListAsync(ct);
        var distributed = await db.DistRules.Select(r => r.PackageId).Distinct().ToListAsync(ct);
        var purge = spare.Where(p => distributed.Contains(p.Id)).Skip(keep).ToList();
        foreach (var p in purge)
        {
            p.IsDeleted = true;
            storage.Delete(p.FileName);
            Audit("system", "PACKAGE_PURGE", p.FileName);
        }
        if (purge.Count > 0) await db.SaveChangesAsync(ct);
    }
}
