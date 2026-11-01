using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.LanguageData;

internal sealed class LanguageDataService(ImsDbContext db) : ILanguageDataService
{
    private IQueryable<Domain.System.Language> Offered() =>
        db.Languages.AsNoTracking().Where(l => !l.IsDeleted && l.IsActive);

    private async Task<HashSet<string>> OfferedCodesAsync(CancellationToken ct) =>
        (await Offered().Select(l => l.LanguageCode).ToListAsync(ct))
            .Select(c => c.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

    private static IQueryable<Domain.System.LanguageData> Filtered(
        ImsDbContext db, string? q, string? labelKey, string? deviceScope)
    {
        var query = db.LanguageData.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(labelKey))
            query = query.Where(d => EF.Functions.Like(d.LabelKey, LikeContains(labelKey.Trim()), LikeEscape));
        if (!string.IsNullOrWhiteSpace(deviceScope))
            query = query.Where(d => d.DeviceScope == deviceScope);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(d => EF.Functions.Like(d.LabelKey, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(d.LabelText, LikeContains(s), LikeEscape));
        }
        return query;
    }

    private static async Task<List<LanguageRowDto>> PivotAsync(
        ImsDbContext db, string? q, string? labelKey, string? deviceScope, string? sort, CancellationToken ct)
    {
        var keys = await Filtered(db, q, labelKey, deviceScope)
            .Select(d => new { d.LabelKey, d.DeviceScope })
            .Distinct().ToListAsync(ct);
        if (keys.Count == 0) return new List<LanguageRowDto>();

        var keyNames = keys.Select(k => k.LabelKey).Distinct().ToList();
        var all = await db.LanguageData.AsNoTracking()
            .Where(d => keyNames.Contains(d.LabelKey))
            .ToListAsync(ct);

        var rows = keys.Select(k =>
        {
            var group = all.Where(d => d.LabelKey == k.LabelKey && d.DeviceScope == k.DeviceScope).ToList();
            string? Pick(string lang) => group.FirstOrDefault(d =>
                string.Equals(d.LanguageCode, lang, StringComparison.OrdinalIgnoreCase))?.LabelText;
            return new LanguageRowDto(k.LabelKey, k.DeviceScope, Pick("en"), Pick("ja"), Pick("vi"));
        });

        var desc = sort?.StartsWith('-') == true;
        var col = (desc ? sort![1..] : sort ?? "").Trim().ToLowerInvariant();
        Func<LanguageRowDto, string?> key = col switch
        {
            "device_scope" => r => r.DeviceScope,
            "english" or "en" => r => r.En,
            "japanese" or "ja" => r => r.Ja,
            "vietnamese" or "vi" => r => r.Vi,
            _ => r => r.LabelKey,
        };
        var ordered = desc
            ? rows.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
            : rows.OrderBy(key, StringComparer.OrdinalIgnoreCase);
        return ordered.ThenBy(r => r.LabelKey, StringComparer.OrdinalIgnoreCase)
                      .ThenBy(r => r.DeviceScope, StringComparer.OrdinalIgnoreCase)
                      .ToList();
    }

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, string? q = null, string? labelKey = null, string? deviceScope = null,
        string? sort = null, int? page = null, int? pageSize = null)
    {
        var rows = await PivotAsync(db, q, labelKey, deviceScope, sort, ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var slice = rows.Skip((pg - 1) * sz).Take(sz).ToList();
        return Outcome.Ok(new ApiListSuccess<LanguageRowDto>(slice, new PageMeta(rows.Count, pg, sz)));
    }

    public async Task<ServiceResult> SaveAsync(LanguageSaveDto body, CancellationToken ct)
    {
        var items = body?.Items ?? new List<LanguageItemDto>();
        if (items.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));
        if (items.Any(i => string.IsNullOrWhiteSpace(i.LabelKey)))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        var offered = await OfferedCodesAsync(ct);
        if (items.Any(i => !offered.Contains((i.LanguageCode ?? "").Trim().ToLowerInvariant())))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));

        var keys = items.Select(i => i.LabelKey).Distinct().ToList();
        var existing = await db.LanguageData.Where(d => keys.Contains(d.LabelKey)).ToListAsync(ct);

        foreach (var i in items)
        {
            var lang = i.LanguageCode.Trim().ToLowerInvariant();
            var row = existing.FirstOrDefault(d => d.LabelKey == i.LabelKey
                                                && d.LanguageCode == lang
                                                && d.DeviceScope == i.DeviceScope);
            if (row is null)
            {
                var added = new Domain.System.LanguageData
                {
                    LabelKey = i.LabelKey.Trim(), LanguageCode = lang,
                    LabelText = i.LabelText ?? "", DeviceScope = i.DeviceScope,
                    CreatedAt = DateTime.Now,
                };
                db.LanguageData.Add(added);
                existing.Add(added);
            }
            else
            {
                row.LabelText = i.LabelText ?? "";
                row.UpdatedAt = DateTime.Now;
            }
        }
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<int>(items.Count));
    }

    public async Task<ServiceResult> LabelsAsync(CancellationToken ct, string? lang = null, string deviceScope = "PC")
    {
        var code = (lang ?? "en").Trim().ToLowerInvariant();
        if (!(await OfferedCodesAsync(ct)).Contains(code))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));

        var rows = await db.LanguageData.AsNoTracking()
            .Where(d => d.LanguageCode == code
                     && (d.DeviceScope == null || d.DeviceScope == deviceScope))
            .Select(d => new { d.LabelKey, d.LabelText, d.DeviceScope })
            .ToListAsync(ct);

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var r in rows.Where(r => r.DeviceScope == null)) map[r.LabelKey] = r.LabelText;
        foreach (var r in rows.Where(r => r.DeviceScope != null)) map[r.LabelKey] = r.LabelText;

        return Outcome.Ok(new ApiSuccess<Dictionary<string, string>>(map));
    }

    public async Task<ServiceResult> LanguagesAsync(CancellationToken ct)
    {
        var list = await Offered()
            .OrderBy(l => l.CreatedAt).ThenBy(l => l.LanguageCode)
            .Select(l => new LanguageOptionDto(l.LanguageCode, l.LanguageName, l.IconImage))
            .ToListAsync(ct);
        return Outcome.Ok(new ApiSuccess<IReadOnlyList<LanguageOptionDto>>(list));
    }

    public async Task<ServiceResult> ExportCsvAsync(
        CancellationToken ct, string? q = null, string? labelKey = null, string? deviceScope = null,
        string? sort = null)
    {
        var rows = await PivotAsync(db, q, labelKey, deviceScope, sort, ct);
        var sb = new StringBuilder();
        sb.AppendLine("device_scope,label_key,english,japanese,vietnamese");
        foreach (var r in rows)
            sb.Append(Csv(r.DeviceScope)).Append(',').Append(Csv(r.LabelKey)).Append(',')
              .Append(Csv(r.En)).Append(',').Append(Csv(r.Ja)).Append(',')
              .Append(Csv(r.Vi)).AppendLine();
        return CsvFile(sb, "language_data.csv");
    }
}
