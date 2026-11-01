using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Distribution;

internal static class DistSettingsStore
{
    public static readonly DistSettingsDto Defaults = new("com.imssolutions.ht", 150, 3, 14, 3, true);

    private const string Category = "Distribution";
    private const string KeyPackage = "dist_package_name";
    private const string KeyUpload = "dist_upload_mb";
    private const string KeyRetention = "dist_retention";
    private const string KeyN = "dist_n_days";
    private const string KeyM = "dist_m_days";
    private const string KeyAutoAdd = "dist_auto_add";

    public static async Task<DistSettingsDto> ReadAsync(ImsDbContext db, CancellationToken ct)
    {
        var map = await db.GeneralSettings.AsNoTracking()
            .Where(g => g.ConfigCode.StartsWith("dist_"))
            .ToDictionaryAsync(g => g.ConfigCode, g => g.ConfigValue, ct);
        int Int(string key, int fallback) =>
            map.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0
                ? n : fallback;
        return new DistSettingsDto(
            map.TryGetValue(KeyPackage, out var p) && !string.IsNullOrWhiteSpace(p) ? p.Trim() : Defaults.PackageName,
            Int(KeyUpload, Defaults.UploadMb), Int(KeyRetention, Defaults.Retention),
            Int(KeyN, Defaults.NDays), Int(KeyM, Defaults.MDays),
            map.TryGetValue(KeyAutoAdd, out var a) ? a != "0" : Defaults.AutoAdd);
    }

    public static async Task WriteAsync(ImsDbContext db, DistSettingsDto s, string who, DateTime now, CancellationToken ct)
    {
        var values = new Dictionary<string, string>
        {
            [KeyPackage] = s.PackageName.Trim(),
            [KeyUpload] = s.UploadMb.ToString(CultureInfo.InvariantCulture),
            [KeyRetention] = s.Retention.ToString(CultureInfo.InvariantCulture),
            [KeyN] = s.NDays.ToString(CultureInfo.InvariantCulture),
            [KeyM] = s.MDays.ToString(CultureInfo.InvariantCulture),
            [KeyAutoAdd] = s.AutoAdd ? "1" : "0",
        };
        var rows = await db.GeneralSettings.Where(g => values.Keys.Contains(g.ConfigCode)).ToListAsync(ct);
        foreach (var (key, value) in values)
        {
            var row = rows.FirstOrDefault(r => r.ConfigCode == key);
            if (row is null)
                db.GeneralSettings.Add(new GeneralSetting { ConfigCode = key, ConfigValue = value, Category = Category,
                    CreatedAt = now, CreatedBy = who });
            else { row.ConfigValue = value; row.UpdatedAt = now; row.UpdatedBy = who; }
        }
    }
}
