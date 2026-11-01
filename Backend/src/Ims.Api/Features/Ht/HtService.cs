using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Ht;

internal sealed class HtService(ImsDbContext db) : IHtService
{
    private const string DefaultLabelMode = "printNow";
    private const string DefaultReasonScope = "byFunction";

    public async Task<ServiceResult> GetConfigAsync(CancellationToken ct)
    {
        var rows = await db.GeneralSettings.AsNoTracking()
            .Where(s => s.Category == "HT")
            .ToDictionaryAsync(s => s.ConfigCode, s => s.ConfigValue, ct);

        string? V(string key) => rows.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

        var drivers = (V("ht_printer_drivers") ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        return Outcome.Ok(new ApiSuccess<HtConfigDto>(new HtConfigDto(
            V("ht_label_mode_rcv") ?? DefaultLabelMode,
            V("ht_label_mode_shp") ?? DefaultLabelMode,
            V("ht_label_mode_rtn") ?? DefaultLabelMode,
            V("ht_reason_scope") ?? DefaultReasonScope,
            V("ht_apk_version"),
            V("ht_apk_download_url"),
            drivers)));
    }

    public async Task<ServiceResult> ListPrintersAsync(CancellationToken ct)
    {
        var rows = await db.Devices.AsNoTracking()
            .Where(d => d.DeviceType == Domain.Masters.DeviceType.Printer && d.IsActive && !d.IsDeleted
                        && d.IpAddress != null && d.IpAddress.Trim() != "")
            .OrderBy(d => d.DeviceCode)
            .Select(d => new { d.DeviceCode, d.DeviceName, d.IpAddress })
            .Take(200)
            .ToListAsync(ct);

        var dtos = rows.Select(r => new HtPrinterDto(r.DeviceCode, r.DeviceName, r.IpAddress!.Trim())).ToList();
        return Outcome.Ok(new ApiListSuccess<HtPrinterDto>(dtos, new PageMeta(dtos.Count, 1, 200)));
    }

    public async Task<ServiceResult> ListLotsAsync(CancellationToken ct, string? q = null, string? itemCode = null)
    {
        var query = db.ReceivingPlans.AsNoTracking().Where(p => p.LotNo != null && p.LotNo != "");
        if (!string.IsNullOrWhiteSpace(itemCode))
            query = query.Where(p => p.ItemCode == itemCode.Trim());
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => EF.Functions.Like(p.LotNo!, LikeContains(q.Trim()), LikeEscape));

        var rows = await query
            .Select(p => new { Lot = p.LotNo!, p.ItemCode })
            .Distinct()
            .OrderBy(x => x.Lot)
            .Take(200)
            .ToListAsync(ct);

        var dtos = rows.Select(r => new HtLotDto(r.Lot, r.ItemCode)).ToList();
        return Outcome.Ok(new ApiListSuccess<HtLotDto>(dtos, new PageMeta(dtos.Count, 1, 200)));
    }

}
