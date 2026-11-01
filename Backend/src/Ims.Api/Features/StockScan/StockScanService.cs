using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;
using Scan = Ims.Domain.Stocktaking.StockScan;

namespace Ims.Api.Features.StockScan;

internal sealed class StockScanService(ImsDbContext db) : IStockScanService
{
    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, string? itemCode = null, string? itemName = null, string? scanned = null)
    {
        IQueryable<Scan> q = db.StockScans.AsNoTracking();

        var code = NullIfBlank(itemCode);
        if (code is not null) q = q.Where(x => x.ItemCode != null && EF.Functions.Like(x.ItemCode, LikeContains(code), LikeEscape));

        var name = NullIfBlank(itemName);
        if (name is not null) q = q.Where(x => x.ItemName != null && EF.Functions.Like(x.ItemName, LikeContains(name), LikeEscape));

        switch (scanned?.Trim().ToLowerInvariant())
        {
            case "yes": q = q.Where(x => x.IsScanned); break;
            case "no": q = q.Where(x => !x.IsScanned); break;
        }

        var rows = await q
            .OrderBy(x => x.ItemCode).ThenBy(x => x.Epc)
            .Select(x => new StockScanRowDto(
                x.Epc, x.ItemCode, x.ItemName, x.ItemCategory, x.IsScanned, x.WorkerCode, x.WorkerName))
            .ToListAsync(ct);

        var (done, total) = await CountsAsync(ct);
        return Outcome.Ok(new ApiSuccess<StockScanBoardDto>(new StockScanBoardDto(done, total, rows)));
    }

    public async Task<ServiceResult> SummaryAsync(CancellationToken ct)
    {
        var items = await db.StockScans.AsNoTracking()
            .GroupBy(x => x.ItemCode)
            .Select(g => new StockScanItemDto(
                g.Key,
                g.Max(x => x.ItemName),
                g.Max(x => x.ItemCategory),
                g.Count(),
                g.Count(x => x.IsScanned)))
            .ToListAsync(ct);

        items = items.OrderBy(i => i.ItemCode, StringComparer.Ordinal).ToList();
        var (done, total) = await CountsAsync(ct);
        return Outcome.Ok(new ApiSuccess<StockScanSummaryDto>(new StockScanSummaryDto(done, total, items)));
    }

    public async Task<ServiceResult> ReadAsync(StockScanReadDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var epcs = (body.Epcs ?? [])
            .Select(NullIfBlank)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        int accepted = 0, already = 0;
        if (epcs.Count > 0)
        {
            var rows = await db.StockScans.Where(x => epcs.Contains(x.Epc)).ToListAsync(ct);
            var workerCode = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var workerName = workerCode is null
                ? null
                : await db.AppUsers.AsNoTracking()
                    .Where(u => u.UserCode == workerCode)
                    .Select(u => u.UserName)
                    .FirstOrDefaultAsync(ct) ?? workerCode;

            foreach (var row in rows)
            {
                if (row.IsScanned) { already++; continue; }
                row.IsScanned = true;
                row.WorkerCode = workerCode;
                row.WorkerName = workerName;
                accepted++;
            }
            await db.SaveChangesAsync(ct);
        }

        var ignored = epcs.Count - accepted - already;
        var (done, total) = await CountsAsync(ct);
        return Outcome.Ok(new ApiSuccess<StockScanReadResultDto>(
            new StockScanReadResultDto(accepted, already, ignored, done, total)));
    }

    public async Task<ServiceResult> ResetAsync(CancellationToken ct)
    {
        await db.StockScans.ExecuteUpdateAsync(s => s
            .SetProperty(x => x.IsScanned, false)
            .SetProperty(x => x.WorkerCode, (string?)null)
            .SetProperty(x => x.WorkerName, (string?)null), ct);

        return await ListAsync(ct);
    }

    private async Task<(int Scanned, int Total)> CountsAsync(CancellationToken ct)
    {
        var total = await db.StockScans.CountAsync(ct);
        var scanned = await db.StockScans.CountAsync(x => x.IsScanned, ct);
        return (scanned, total);
    }
}
