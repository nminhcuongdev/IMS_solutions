using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.LanguageData;

internal sealed class LanguageDataService(ImsDbContext db) : ILanguageDataService
{
    private static readonly string[] Langs = { "en", "ja", "vi" };

    private static IQueryable<Domain.System.LanguageData> Filtered(
        ImsDbContext db, string? q, string? labelKey, string? deviceScope)
    {
        var query = db.LanguageData.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(labelKey))
            query = query.Where(d => EF.Functions.Like(d.LabelKey, "%" + labelKey.Trim() + "%"));
        if (!string.IsNullOrWhiteSpace(deviceScope))
            query = query.Where(d => d.DeviceScope == deviceScope);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(d => EF.Functions.Like(d.LabelKey, "%" + s + "%")
                                  || EF.Functions.Like(d.LabelText, "%" + s + "%"));
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
        if (items.Any(i => !Langs.Contains((i.LanguageCode ?? "").ToLowerInvariant())))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));

        var keys = items.Select(i => i.LabelKey).Distinct().ToList();
        var existing = await db.LanguageData.Where(d => keys.Contains(d.LabelKey)).ToListAsync(ct);

        foreach (var i in items)
        {
            var lang = i.LanguageCode.ToLowerInvariant();
            var row = existing.FirstOrDefault(d => d.LabelKey == i.LabelKey
                                                && d.LanguageCode == lang
                                                && d.DeviceScope == i.DeviceScope);
            if (row is null)
            {
                var added = new Domain.System.LanguageData
                {
                    LabelKey = i.LabelKey.Trim(), LanguageCode = lang,
                    LabelText = i.LabelText ?? "", DeviceScope = i.DeviceScope,
                    CreatedAt = DateTime.UtcNow,
                };
                db.LanguageData.Add(added);
                existing.Add(added);
            }
            else
            {
                row.LabelText = i.LabelText ?? "";
                row.UpdatedAt = DateTime.UtcNow;
            }
        }
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<int>(items.Count));
    }

    public async Task<ServiceResult> LabelsAsync(CancellationToken ct, string? lang = null, string deviceScope = "PC")
    {
        var code = (lang ?? "en").Trim().ToLowerInvariant();
        if (!Langs.Contains(code)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));

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

    public async Task<ServiceResult> SlotsAsync(CancellationToken ct)
    {
        var cfg = await db.GeneralSettings.AsNoTracking()
            .Where(g => g.Category == "Language")
            .ToDictionaryAsync(g => g.ConfigCode, g => g.ConfigValue, ct);
        var names = await db.Languages.AsNoTracking()
            .Where(l => !l.IsDeleted && l.IsActive)
            .ToDictionaryAsync(l => l.LanguageCode, l => l.LanguageName, ct);

        var slots = new List<LanguageSlotDto>();
        for (var i = 1; i <= 3; i++)
        {
            var code = cfg.GetValueOrDefault($"language_slot_{i}");
            if (string.IsNullOrWhiteSpace(code)) continue;
            slots.Add(new LanguageSlotDto(
                i, code,
                names.GetValueOrDefault(code) ?? code,
                cfg.GetValueOrDefault($"language_slot_{i}_flag_icon")));
        }
        return Outcome.Ok(new ApiSuccess<IReadOnlyList<LanguageSlotDto>>(slots));
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
