using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Settings;

internal sealed class SettingsService(ImsDbContext db) : ISettingsService
{
    private const string Category = "PcSettings";
    private const string KeyInvAgg    = "inv_agg_default";
    private const string KeyMasterPrn = "master_printer_default";
    private const string KeySchedPrn  = "schedule_printer_default";

    private static readonly string[] InvAggValues = { "lot", "item", "location" };

    public async Task<ServiceResult> GetAsync(CancellationToken ct)
    {
        var map = await db.GeneralSettings.AsNoTracking()
            .Where(s => s.Category == Category)
            .ToDictionaryAsync(s => s.ConfigCode, s => s.ConfigValue, ct);

        return Outcome.Ok(new ApiSuccess<SettingsDto>(new SettingsDto(
            map.GetValueOrDefault(KeyInvAgg) ?? "lot",
            map.GetValueOrDefault(KeyMasterPrn),
            map.GetValueOrDefault(KeySchedPrn))));
    }

    public async Task<ServiceResult> SaveAsync(SettingsDto body, CancellationToken ct)
    {
        var agg = (body.InvAggDefault ?? "").Trim().ToLowerInvariant();
        if (!InvAggValues.Contains(agg))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));

        await UpsertAsync(db, KeyInvAgg, agg, ct);
        await UpsertAsync(db, KeyMasterPrn, body.MasterPrinterDefault?.Trim(), ct);
        await UpsertAsync(db, KeySchedPrn, body.SchedulePrinterDefault?.Trim(), ct);
        await db.SaveChangesAsync(ct);

        return await GetAsync(ct);
    }

    private static async Task UpsertAsync(ImsDbContext db, string code, string? value, CancellationToken ct)
    {
        var row = await db.GeneralSettings.FirstOrDefaultAsync(s => s.ConfigCode == code, ct);
        if (row is null)
        {
            db.GeneralSettings.Add(new GeneralSetting
            {
                ConfigCode = code, ConfigValue = value, Category = Category,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            row.ConfigValue = value;
            row.Category = Category;
            row.UpdatedAt = DateTime.UtcNow;
        }
    }
}
