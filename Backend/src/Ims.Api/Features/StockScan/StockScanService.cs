using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;
using Scan = Ims.Domain.Stocktaking.StockScan;

namespace Ims.Api.Features.StockScan;

internal sealed class StockScanService(ImsDbContext db) : IStockScanService
{
    private const int ItemCodeMax = 50;
    private const int ItemNameMax = 255;

    private IQueryable<Scan> Targets => db.StockScans.Where(x => x.IsTarget);

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, string? itemCode = null, string? itemName = null, string? scanned = null)
    {
        IQueryable<Scan> q = Targets.AsNoTracking();

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
        var items = await Targets.AsNoTracking()
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
            var rows = await Targets.Where(x => epcs.Contains(x.Epc)).ToListAsync(ct);
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

    public async Task<ServiceResult> PartsAsync(CancellationToken ct) =>
        Outcome.Ok(new ApiSuccess<IReadOnlyList<StockScanPartDto>>(await PartListAsync(ct)));

    public async Task<ServiceResult> SetTargetsAsync(StockScanTargetsDto body, CancellationToken ct)
    {
        var codes = (body.ItemCodes ?? [])
            .Select(c => c?.Trim() ?? "")
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (codes.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));

        var known = await db.StockScans.AnyAsync(x => codes.Contains(x.ItemCode ?? ""), ct);
        if (!known) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));

        await db.StockScans.ExecuteUpdateAsync(s => s
            .SetProperty(x => x.IsTarget, x => codes.Contains(x.ItemCode ?? "")), ct);

        return await PartsAsync(ct);
    }

    public async Task<ServiceResult> LookupAsync(StockScanLookupDto body, CancellationToken ct)
    {
        var epcs = (body.Epcs ?? [])
            .Select(EpcRules.Normalize)
            .OfType<string>()
            .Where(EpcRules.IsValid)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var registered = epcs.Count == 0
            ? []
            : await db.StockScans.AsNoTracking()
                .Where(x => epcs.Contains(x.Epc))
                .OrderBy(x => x.Epc)
                .Select(x => new StockScanRegisteredEpcDto(x.Epc, x.ItemCode, x.ItemName))
                .ToListAsync(ct);

        var prefixes = new List<StockScanPrefixDto>();
        foreach (var prefix in epcs.Select(EpcRules.PrefixOf).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            var owners = await db.StockScans.AsNoTracking()
                .Where(x => x.Epc.StartsWith(prefix) && x.ItemCode != null)
                .GroupBy(x => x.ItemCode)
                .Select(g => new { Code = g.Key, Name = g.Max(x => x.ItemName), Count = g.Count() })
                .ToListAsync(ct);

            var best = owners
                .OrderByDescending(o => o.Count)
                .ThenBy(o => o.Code, StringComparer.Ordinal)
                .FirstOrDefault();
            if (best is not null) prefixes.Add(new StockScanPrefixDto(prefix, best.Code, best.Name));
        }

        return Outcome.Ok(new ApiSuccess<StockScanLookupResultDto>(new StockScanLookupResultDto(registered, prefixes)));
    }

    public async Task<ServiceResult> RegisterAsync(StockScanRegisterDto body, CancellationToken ct)
    {
        var groups = new List<(string Code, string Name, List<string> Epcs)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var g in body.Groups ?? [])
        {
            var epcs = (g.Epcs ?? []).Select(EpcRules.Normalize).OfType<string>().ToList();
            if (epcs.Count == 0) continue;

            var code = NullIfBlank(g.ItemCode);
            if (code is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("rsdPartNo", "部品番号")));
            if (code.Length > ItemCodeMax) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005TooLong(code, ItemCodeMax)));

            var name = NullIfBlank(g.ItemName);
            if (name is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("rsdPartName", "部品名")));
            if (name.Length > ItemNameMax) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005FieldTooLong()));

            foreach (var epc in epcs)
            {
                if (!EpcRules.IsValid(epc)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005Epc()));
                if (!seen.Add(epc)) return Outcome.Conflict(new ApiError(ErrorCatalog.Dup002()));
            }
            groups.Add((code, name, epcs));
        }

        if (groups.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));

        var all = seen.ToList();
        if (await db.StockScans.AnyAsync(x => all.Contains(x.Epc), ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup002()));

        var codes = groups.Select(g => g.Code).Distinct(StringComparer.Ordinal).ToList();
        var targetOf = await db.StockScans.AsNoTracking()
            .Where(x => x.ItemCode != null && codes.Contains(x.ItemCode))
            .GroupBy(x => x.ItemCode!)
            .Select(g => new { Code = g.Key, Target = g.Any(x => x.IsTarget) })
            .ToDictionaryAsync(x => x.Code, x => x.Target, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var (code, name, epcs) in groups)
        {
            var target = targetOf.GetValueOrDefault(code, true);
            db.StockScans.AddRange(epcs.Select(epc => new Scan
            {
                Epc = epc, ItemCode = code, ItemName = name, IsScanned = false, IsTarget = target,
            }));
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await db.StockScans.AnyAsync(x => all.Contains(x.Epc), ct))
                return Outcome.Conflict(new ApiError(ErrorCatalog.Dup002()));
            throw;
        }

        return Outcome.Created("/stock-scan", new ApiSuccess<StockScanRegisterResultDto>(
            new StockScanRegisterResultDto(all.Count, codes.Count)));
    }

    public async Task<ServiceResult> DeletePartAsync(string? itemCode, CancellationToken ct)
    {
        var code = itemCode?.Trim() ?? "";
        var deleted = await db.StockScans.Where(x => (x.ItemCode ?? "") == code).ExecuteDeleteAsync(ct);
        if (deleted == 0) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Part")));

        return Outcome.Ok(new ApiSuccess<StockScanDeletePartResultDto>(
            new StockScanDeletePartResultDto(deleted, await PartListAsync(ct))));
    }

    private async Task<IReadOnlyList<StockScanPartDto>> PartListAsync(CancellationToken ct)
    {
        var parts = await db.StockScans.AsNoTracking()
            .GroupBy(x => x.ItemCode)
            .Select(g => new StockScanPartDto(
                g.Key,
                g.Max(x => x.ItemName),
                g.Count(),
                g.Count(x => x.IsScanned),
                g.Any(x => x.IsTarget)))
            .ToListAsync(ct);

        return parts.OrderBy(p => p.ItemCode ?? "", StringComparer.Ordinal).ToList();
    }

    private async Task<(int Scanned, int Total)> CountsAsync(CancellationToken ct)
    {
        var total = await Targets.CountAsync(ct);
        var scanned = await Targets.CountAsync(x => x.IsScanned, ct);
        return (scanned, total);
    }
}
