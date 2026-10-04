using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Stocktaking;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Stocktaking;

internal sealed class StocktakingService(ImsDbContext db) : IStocktakingService
{
    private readonly SortMap<StocktakingLabel> labelSort = new SortMap<StocktakingLabel>()
        .Add("no", x => x.ItemCode, x => x.SerialNo)
        .Add("serial_no", x => x.SerialNo)
        .Add("item_code", x => x.ItemCode)
        .Add("item_name", x => db.Items.Where(i => i.ItemCode == x.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("qty", x => db.Stocks.Where(s => s.ItemCode == x.ItemCode && s.SerialNo == x.SerialNo)
            .Select(s => (decimal?)s.StockQty).FirstOrDefault())
        .Add("unit", x => db.Items.Where(i => i.ItemCode == x.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("location_code", x => x.LocationCode)
        .Add("lot_no", x => x.LotNo)
        .Add("status", x => x.Status)
        .Add("is_scanned", x => x.IsScanned)
        .Add("updated_at", x => x.UpdatedAt)
        .Default("no")
        .ThenAlwaysBy(x => x.SerialNo);

    private readonly SortMap<StocktakingHistory> historySort = new SortMap<StocktakingHistory>()
        .Add("no", h => h.ItemCode, h => h.LotNo)
        .Add("stocktaking_name", h => h.StocktakingName)
        .Add("stocktaking_date", h => h.StocktakingDate)
        .Add("item_code", h => h.ItemCode)
        .Add("item_name", h => db.Items.Where(i => i.ItemCode == h.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("lot_no", h => h.LotNo)
        .Add("unit", h => db.Items.Where(i => i.ItemCode == h.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("planned_qty", h => h.PlannedQty)
        .Add("result_qty", h => h.ResultQty)
        .Add("missing_qty", h => h.MissingQty)
        .Add("status", h => h.PlannedQty == h.ResultQty && h.MissingQty == 0m ? 1 : 0)
        .Default("stocktaking_date")
        .ThenAlwaysBy(h => h.ItemCode)
        .ThenAlwaysBy(h => h.LotNo);

    private static readonly Dictionary<string, Func<StocktakingGroupDto, object?>> GroupSort =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["item_code"] = g => g.ItemCode,
            ["item_name"] = g => g.ItemName,
            ["location_code"] = g => g.LocationText,
            ["lot_no"] = g => g.LotNo,
            ["planned_labels"] = g => g.PlannedLabels,
            ["scanned_labels"] = g => g.ScannedLabels,
            ["planned_qty"] = g => g.PlannedQty,
            ["result_qty"] = g => g.ResultQty,
            ["unit"] = g => g.Unit,
            ["updated_at"] = g => g.UpdatedAt,
        };

    private static List<StocktakingGroupDto> SortGroups(List<StocktakingGroupDto> groups, string? sort)
    {
        var desc = sort?.StartsWith('-') == true;
        var key = (desc ? sort![1..] : sort ?? "").Trim();
        if (!GroupSort.TryGetValue(key, out var by)) return groups;
        var ordered = desc
            ? groups.OrderByDescending(by, Comparer<object?>.Default)
            : groups.OrderBy(by, Comparer<object?>.Default);
        return ordered.ThenBy(g => g.ItemCode, StringComparer.Ordinal)
            .ThenBy(g => g.LotNo, StringComparer.Ordinal).ToList();
    }

    public async Task<ServiceResult> GetSessionAsync(CancellationToken ct)
    {
        var s = await CurrentAsync(db, ct);
        return Outcome.Ok(new ApiSuccess<StocktakingSessionDto?>(
            s is null ? null : await MapSessionAsync(db, s, ct)));
    }

    public async Task<ServiceResult> StartAsync(StartStocktakingDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var name = Trimmed(body.SessionName);
        if (name is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));

        if (await CurrentAsync(db, ct) is not null)
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));

        var who = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var now = DateTime.Now;

        var session = new StocktakingSession
        {
            SessionName = name,
            Status = StocktakingSessionStatus.InProgress,
            StartedAt = now,
            CreatedAt = now,
            CreatedBy = who,
        };
        db.StocktakingSessions.Add(session);
        await db.SaveChangesAsync(ct);

        db.StocktakingLabels.RemoveRange(await db.StocktakingLabels.ToListAsync(ct));
        await db.SaveChangesAsync(ct);

        var rows = await db.Stocks.AsNoTracking()
            .Where(s => s.StockQty > 0 && s.LocationCode != null)
            .Select(s => new { s.ItemCode, s.SerialNo, s.LocationCode, s.LotNo, s.ExpiryDate })
            .ToListAsync(ct);

        foreach (var r in rows)
            db.StocktakingLabels.Add(new StocktakingLabel
            {
                SerialNo = r.SerialNo,
                StocktakingSessionNo = session.No,
                ItemCode = r.ItemCode,
                LocationCode = r.LocationCode!,
                LotNo = r.LotNo,
                ExpiryDate = r.ExpiryDate,
                IsScanned = false,
                Status = StocktakingLabelStatus.NotStarted,
            });
        await db.SaveChangesAsync(ct);

        return Outcome.Ok(new ApiSuccess<StocktakingSessionDto>(await MapSessionAsync(db, session, ct)));
    }

    public async Task<ServiceResult> CompleteAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var s = await CurrentAsync(db, ct);
        if (s is null) return Outcome.Json(new ApiError(ErrorCatalog.Stk001()), statusCode: 409);
        if (s.Status != StocktakingSessionStatus.InProgress)
            return Outcome.Json(new ApiError(ErrorCatalog.Stk004()), statusCode: 409);

        s.Status = StocktakingSessionStatus.Completed;
        s.CompletedAt = DateTime.Now;
        Touch(s, user);
        await db.SaveChangesAsync(ct);

        return Outcome.Ok(new ApiSuccess<StocktakingSessionDto>(await MapSessionAsync(db, s, ct)));
    }

    public async Task<ServiceResult> FinalizeAsync(
        FinalizeStocktakingDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var s = await CurrentAsync(db, ct);
        if (s is null || s.Status != StocktakingSessionStatus.Completed)
            return Outcome.Json(new ApiError(ErrorCatalog.Stk001()), statusCode: 409);

        var labels = await db.StocktakingLabels
            .Where(x => x.StocktakingSessionNo == s.No).ToListAsync(ct);

        var wanted = (body.Serials ?? Array.Empty<string>())
            .Select(x => x?.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToHashSet();

        var now = DateTime.Now;
        foreach (var l in labels)
        {
            if (l.IsScanned || !wanted.Contains(l.SerialNo)) continue;
            l.Status = StocktakingLabelStatus.Missing;
            l.UpdatedAt = now;
        }

        await WriteHistoryAsync(db, s, labels, now, ct);

        s.Status = StocktakingSessionStatus.Finalized;
        Touch(s, user);
        await db.SaveChangesAsync(ct);

        return Outcome.Ok(new ApiSuccess<StocktakingSessionDto>(await MapSessionAsync(db, s, ct)));
    }

    private static async Task WriteHistoryAsync(
        ImsDbContext db, StocktakingSession s, List<StocktakingLabel> labels, DateTime now, CancellationToken ct)
    {
        var serials = labels.Select(l => l.SerialNo).ToList();
        var qtyBySerial = await db.Stocks.AsNoTracking()
            .Where(x => serials.Contains(x.SerialNo))
            .Select(x => new { x.ItemCode, x.SerialNo, x.StockQty })
            .ToListAsync(ct);

        var itemCodes = labels.Select(l => l.ItemCode).Distinct().ToList();
        var packQty = await db.Items.AsNoTracking()
            .Where(i => itemCodes.Contains(i.ItemCode))
            .Select(i => new { i.ItemCode, i.PackageQty })
            .ToDictionaryAsync(i => i.ItemCode, i => i.PackageQty, ct);

        decimal QtyOf(StocktakingLabel l) => l.Status == StocktakingLabelStatus.Over
            ? packQty.GetValueOrDefault(l.ItemCode)
            : qtyBySerial.FirstOrDefault(q => q.SerialNo == l.SerialNo && q.ItemCode == l.ItemCode)?.StockQty ?? 0m;

        var groups = labels
            .GroupBy(l => (l.ItemCode, Lot: l.LotNo ?? ""))
            .Select(g => new
            {
                g.Key.ItemCode,
                g.Key.Lot,
                Planned = g.Where(l => l.Status != StocktakingLabelStatus.Over).Sum(QtyOf),
                Result = g.Where(l => l.IsScanned).Sum(QtyOf),
                Missing = g.Where(l => l.Status == StocktakingLabelStatus.Missing).Sum(QtyOf),
            })
            .ToList();

        var keys = groups.Select(g => g.ItemCode).Distinct().ToList();
        var existing = await db.StocktakingHistories
            .Where(h => keys.Contains(h.ItemCode)).ToListAsync(ct);

        foreach (var g in groups)
        {
            var row = existing.FirstOrDefault(h => h.ItemCode == g.ItemCode && h.LotNo == g.Lot);
            if (row is null)
            {
                row = new StocktakingHistory { ItemCode = g.ItemCode, LotNo = g.Lot };
                db.StocktakingHistories.Add(row);
            }
            row.StocktakingName = s.SessionName;
            row.StocktakingDate = s.CompletedAt ?? now;
            row.PlannedQty = g.Planned;
            row.ResultQty = g.Result;
            row.MissingQty = g.Missing;
            row.UpdatedAt = now;
        }
    }

    public async Task<ServiceResult> ListItemsAtLocationAsync(CancellationToken ct, string? locationCode = null)
    {
        var s = await CurrentAsync(db, ct);
        if (s is null) return Outcome.Json(new ApiError(ErrorCatalog.Stk001()), statusCode: 409);

        var loc = Trimmed(locationCode);
        if (loc is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Loc001()));

        var rows = await GroupsAsync(db, db.StocktakingLabels.AsNoTracking()
            .Where(x => x.StocktakingSessionNo == s.No && x.LocationCode == loc), ct);

        if (rows.Count == 0) return Outcome.Json(new ApiError(ErrorCatalog.Loc003()), statusCode: 404);
        return Outcome.Ok(new ApiSuccess<List<StocktakingGroupDto>>(rows));
    }

    public async Task<ServiceResult> SaveResultAsync(
        StocktakingResultDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var s = await CurrentAsync(db, ct);
        if (s is null) return Outcome.Json(new ApiError(ErrorCatalog.Stk001()), statusCode: 409);
        if (!s.AcceptsResults) return Outcome.Json(new ApiError(ErrorCatalog.Stk004()), statusCode: 409);

        if (body.CountedLabels < 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var loc = Trimmed(body.LocationCode);
        var item = Trimmed(body.ItemCode);
        if (loc is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Loc001()));
        if (item is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        var lot = Trimmed(body.LotNo);

        if (!await db.Locations.AnyAsync(l => l.LocationCode == loc, ct))
            return Outcome.Json(new ApiError(ErrorCatalog.Loc003()), statusCode: 404);

        var group = await db.StocktakingLabels
            .Where(x => x.StocktakingSessionNo == s.No && x.LocationCode == loc && x.ItemCode == item
                     && (lot == null ? x.LotNo == null : x.LotNo == lot))
            .ToListAsync(ct);

        if (group.Count == 0)
        {
            var known = await db.Items.AsNoTracking().AnyAsync(i => i.ItemCode == item, ct);
            if (!known)
                return Outcome.Json(new ApiError(ErrorCatalog.NotFound(item)), statusCode: 404);

            return Outcome.Json(new ApiError(ErrorCatalog.Loc003()), statusCode: 404);
        }

        var now = DateTime.Now;
        var planned = group.Where(l => l.Status != StocktakingLabelStatus.Over)
            .OrderBy(l => l.SerialNo).ToList();
        var surplus = group.Where(l => l.Status == StocktakingLabelStatus.Over)
            .OrderBy(l => l.SerialNo).ToList();

        if (body.CountedLabels > planned.Count)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty002()));

        for (var i = 0; i < planned.Count; i++)
        {
            var scanned = i < body.CountedLabels;
            if (planned[i].IsScanned == scanned) continue;
            planned[i].IsScanned = scanned;
            planned[i].Status = scanned ? StocktakingLabelStatus.Normal : StocktakingLabelStatus.NotStarted;
            planned[i].UpdatedAt = now;
        }

        foreach (var extra in surplus) db.StocktakingLabels.Remove(extra);

        await db.SaveChangesAsync(ct);

        var rows = await GroupsAsync(db, db.StocktakingLabels.AsNoTracking()
            .Where(x => x.StocktakingSessionNo == s.No && x.LocationCode == loc && x.ItemCode == item
                     && (lot == null ? x.LotNo == null : x.LotNo == lot)), ct);
        if (rows.Count > 0) return Outcome.Ok(new ApiSuccess<StocktakingGroupDto>(rows[0]));

        var meta = await db.Items.AsNoTracking().Where(i => i.ItemCode == item)
            .Select(i => new { i.ItemName, i.UomCode }).FirstOrDefaultAsync(ct);
        return Outcome.Ok(new ApiSuccess<StocktakingGroupDto>(new StocktakingGroupDto(
            item, meta?.ItemName, loc, lot, 0, 0, 0m, 0m, meta?.UomCode, now)));
    }

    public async Task<ServiceResult> ListLabelsAsync(
        CancellationToken ct, string? view = null, string? locationCode = null, string? itemCode = null,
        string? itemName = null, bool notScanned = false, bool statusDiff = false, int page = 1, int pageSize = 50,
        string? sort = null)
    {
        var p = NormalizePage(page);
        var ps = NormalizePageSize(pageSize);

        var s = await CurrentAsync(db, ct);
        if (s is null)
            return view == "model"
                ? Outcome.Ok(new ApiListSuccess<StocktakingGroupDto>([], new PageMeta(0, p, ps)))
                : Outcome.Ok(new ApiListSuccess<StocktakingLabelDto>([], new PageMeta(0, p, ps)));

        var q = Filter(db, s.No, locationCode, itemCode, itemName);

        if (view == "model")
        {
            var groups = await GroupsAsync(db, q, ct);
            if (notScanned) groups = groups.Where(g => g.ScannedLabels == 0).ToList();
            if (statusDiff) groups = groups.Where(g => g.ScannedLabels != g.PlannedLabels).ToList();
            groups = SortGroups(groups, sort);

            var total = groups.Count;
            var pageRows = groups.Skip((p - 1) * ps).Take(ps).ToList();
            return Outcome.Ok(new ApiListSuccess<StocktakingGroupDto>(pageRows, new PageMeta(total, p, ps)));
        }

        if (notScanned) q = q.Where(x => !x.IsScanned);
        if (statusDiff) q = q.Where(x => x.Status != StocktakingLabelStatus.Normal);

        var labels = await LabelsAsync(db, labelSort.Apply(q, sort).Skip((p - 1) * ps).Take(ps), ct);
        var count = await q.CountAsync(ct);
        return Outcome.Ok(new ApiListSuccess<StocktakingLabelDto>(labels, new PageMeta(count, p, ps)));
    }

    public async Task<ServiceResult> ExportLabelsCsvAsync(
        CancellationToken ct, string? view = null, string? locationCode = null, string? itemCode = null,
        string? itemName = null, bool notScanned = false, bool statusDiff = false)
    {
        var s = await CurrentAsync(db, ct);
        var sb = new StringBuilder();

        if (view == "model")
        {
            sb.AppendLine("item_code,item_name,location_code,lot_no,planned_labels,scanned_labels," +
                          "planned_qty,result_qty,unit,updated_at");
            var groups = s is null ? [] : await GroupsAsync(db, Filter(db, s.No, locationCode, itemCode, itemName), ct);
            if (notScanned) groups = groups.Where(g => g.ScannedLabels == 0).ToList();
            if (statusDiff) groups = groups.Where(g => g.ScannedLabels != g.PlannedLabels).ToList();
            foreach (var g in groups)
                sb.Append(Csv(g.ItemCode)).Append(',').Append(Csv(g.ItemName)).Append(',')
                  .Append(Csv(g.LocationText)).Append(',').Append(Csv(g.LotNo)).Append(',')
                  .Append(g.PlannedLabels).Append(',').Append(g.ScannedLabels).Append(',')
                  .Append(Num(g.PlannedQty)).Append(',').Append(Num(g.ResultQty)).Append(',')
                  .Append(Csv(g.Unit)).Append(',')
                  .Append(g.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "").AppendLine();
            return CsvFile(sb, "label-stocktaking.csv");
        }

        sb.AppendLine("serial_no,item_code,item_name,qty,unit,location_code,lot_no,status," +
                      "is_scanned,expiry_date,updated_at");
        if (s is not null)
        {
            var q = Filter(db, s.No, locationCode, itemCode, itemName);
            if (notScanned) q = q.Where(x => !x.IsScanned);
            if (statusDiff) q = q.Where(x => x.Status != StocktakingLabelStatus.Normal);
            foreach (var r in await LabelsAsync(db, q.OrderBy(x => x.ItemCode).ThenBy(x => x.SerialNo), ct))
                sb.Append(Csv(r.SerialNo)).Append(',').Append(Csv(r.ItemCode)).Append(',')
                  .Append(Csv(r.ItemName)).Append(',').Append(Num(r.Qty)).Append(',')
                  .Append(Csv(r.Unit)).Append(',').Append(Csv(r.LocationCode)).Append(',')
                  .Append(Csv(r.LotNo)).Append(',').Append(Csv(r.StatusLabelKey)).Append(',')
                  .Append(r.IsScanned ? "1" : "0").Append(',')
                  .Append(r.ExpiryDate?.ToString("yyyy-MM-dd") ?? "").Append(',')
                  .Append(r.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "").AppendLine();
        }
        return CsvFile(sb, "label-stocktaking.csv");
    }

    public async Task<ServiceResult> ListHistoryAsync(
        CancellationToken ct, string? q = null, int page = 1, int pageSize = 50, string? sort = null)
    {
        var p = NormalizePage(page);
        var ps = NormalizePageSize(pageSize);
        var query = HistoryFilter(db, q);

        var total = await query.CountAsync(ct);
        var rows = await HistoryRowsAsync(db,
            historySort.Apply(query, sort ?? "-stocktaking_date").Skip((p - 1) * ps).Take(ps), ct);
        return Outcome.Ok(new ApiListSuccess<StocktakingHistoryDto>(rows, new PageMeta(total, p, ps)));
    }

    public async Task<ServiceResult> ExportHistoryCsvAsync(CancellationToken ct, string? q = null)
    {
        var rows = await HistoryRowsAsync(db,
            HistoryFilter(db, q).OrderByDescending(h => h.StocktakingDate).ThenBy(h => h.ItemCode), ct);

        var sb = new StringBuilder();
        sb.AppendLine("stocktaking_name,stocktaking_date,item_code,item_name,lot_no,unit," +
                      "planned_qty,result_qty,missing_qty,status");
        foreach (var r in rows)
            sb.Append(Csv(r.StocktakingName)).Append(',')
              .Append(r.StocktakingDate.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
              .Append(Csv(r.LotNo)).Append(',').Append(Csv(r.Unit)).Append(',')
              .Append(Num(r.PlannedQty)).Append(',').Append(Num(r.ResultQty)).Append(',')
              .Append(Num(r.MissingQty)).Append(',').Append(Csv(r.BadgeKey)).AppendLine();
        return CsvFile(sb, "stocktaking-history.csv");
    }

    private static Task<StocktakingSession?> CurrentAsync(ImsDbContext db, CancellationToken ct) =>
        db.StocktakingSessions
            .Where(x => x.Status == StocktakingSessionStatus.InProgress
                     || x.Status == StocktakingSessionStatus.Completed)
            .OrderByDescending(x => x.No)
            .FirstOrDefaultAsync(ct);

    private static IQueryable<StocktakingLabel> Filter(
        ImsDbContext db, long sessionNo, string? locationCode, string? itemCode, string? itemName)
    {
        var q = db.StocktakingLabels.AsNoTracking().Where(x => x.StocktakingSessionNo == sessionNo);
        if (Trimmed(locationCode) is { } loc) q = q.Where(x => x.LocationCode == loc);
        if (Trimmed(itemCode) is { } ic) q = q.Where(x => EF.Functions.Like(x.ItemCode, "%" + ic + "%"));
        if (Trimmed(itemName) is { } iname)
            q = q.Where(x => db.Items.Any(i => i.ItemCode == x.ItemCode
                && EF.Functions.Like(i.ItemName, "%" + iname + "%")));
        return q;
    }

    private static IQueryable<StocktakingHistory> HistoryFilter(ImsDbContext db, string? q)
    {
        var query = db.StocktakingHistories.AsNoTracking();
        if (Trimmed(q) is { } free)
            query = query.Where(h => EF.Functions.Like(h.StocktakingName, "%" + free + "%")
                                  || EF.Functions.Like(h.ItemCode, "%" + free + "%")
                                  || EF.Functions.Like(h.LotNo, "%" + free + "%")
                                  || db.Items.Any(i => i.ItemCode == h.ItemCode
                                        && EF.Functions.Like(i.ItemName, "%" + free + "%")));
        return query;
    }

    private static async Task<List<StocktakingHistoryDto>> HistoryRowsAsync(
        ImsDbContext db, IQueryable<StocktakingHistory> q, CancellationToken ct)
    {
        var rows = await q.Select(h => new
        {
            h.ItemCode, h.LotNo, h.StocktakingName, h.StocktakingDate,
            h.PlannedQty, h.ResultQty, h.MissingQty,
            ItemName = db.Items.Where(i => i.ItemCode == h.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            Unit = db.Items.Where(i => i.ItemCode == h.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
        }).ToListAsync(ct);

        return rows.Select(r => new StocktakingHistoryDto(
            r.ItemCode, r.ItemName, r.LotNo, r.StocktakingName, r.StocktakingDate,
            r.PlannedQty, r.ResultQty, r.MissingQty, r.Unit,
            r.PlannedQty == r.ResultQty && r.MissingQty == 0m ? "badgeMatch" : "badgeDiff")).ToList();
    }

    private static async Task<List<StocktakingLabelDto>> LabelsAsync(
        ImsDbContext db, IQueryable<StocktakingLabel> q, CancellationToken ct)
    {
        var rows = await q.Select(x => new
        {
            x.SerialNo, x.ItemCode, x.LocationCode, x.LotNo, x.IsScanned, x.Status,
            x.ExpiryDate, x.UpdatedAt,
            ItemName = db.Items.Where(i => i.ItemCode == x.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            Unit = db.Items.Where(i => i.ItemCode == x.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            Qty = db.Stocks.Where(s => s.ItemCode == x.ItemCode && s.SerialNo == x.SerialNo)
                    .Select(s => (decimal?)s.StockQty).FirstOrDefault(),
        }).ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Now);
        return rows.Select(r => new StocktakingLabelDto(
            r.SerialNo, r.ItemCode, r.ItemName, r.Qty ?? 0m, r.Unit, r.LocationCode, r.LotNo,
            r.IsScanned, r.Status, StocktakingLabelStatus.ToApiName(r.Status),
            r.ExpiryDate, r.ExpiryDate is { } e && e < today, r.UpdatedAt)).ToList();
    }

    private static async Task<List<StocktakingGroupDto>> GroupsAsync(
        ImsDbContext db, IQueryable<StocktakingLabel> q, CancellationToken ct)
    {
        var rows = await q.Select(x => new
        {
            x.SerialNo, x.ItemCode, x.LocationCode, x.LotNo, x.IsScanned, x.Status, x.UpdatedAt,
            ItemName = db.Items.Where(i => i.ItemCode == x.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            Unit = db.Items.Where(i => i.ItemCode == x.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            Qty = db.Stocks.Where(s => s.ItemCode == x.ItemCode && s.SerialNo == x.SerialNo)
                    .Select(s => (decimal?)s.StockQty).FirstOrDefault(),
            PackQty = db.Items.Where(i => i.ItemCode == x.ItemCode)
                    .Select(i => i.PackageQty).FirstOrDefault(),
        }).ToListAsync(ct);

        return rows
            .Select(x => new
            {
                x.ItemCode, x.LocationCode, x.LotNo, x.IsScanned, x.Status, x.UpdatedAt,
                x.ItemName, x.Unit,
                Qty = x.Qty ?? (x.Status == StocktakingLabelStatus.Over ? x.PackQty : 0m),
            })
            .GroupBy(x => (x.ItemCode, Lot: x.LotNo ?? ""))
            .Select(g => new StocktakingGroupDto(
                g.Key.ItemCode,
                g.First().ItemName,
                string.Join(", ", g.Select(x => x.LocationCode).Distinct().OrderBy(x => x)),
                g.Key.Lot.Length == 0 ? null : g.Key.Lot,
                g.Count(x => x.Status != StocktakingLabelStatus.Over),
                g.Count(x => x.IsScanned),
                g.Where(x => x.Status != StocktakingLabelStatus.Over).Sum(x => x.Qty),
                g.Where(x => x.IsScanned).Sum(x => x.Qty),
                g.First().Unit,
                g.Max(x => x.UpdatedAt)))
            .OrderBy(g => g.ItemCode).ThenBy(g => g.LotNo)
            .ToList();
    }

    private static async Task<StocktakingSessionDto> MapSessionAsync(
        ImsDbContext db, StocktakingSession s, CancellationToken ct)
    {
        var labels = db.StocktakingLabels.AsNoTracking().Where(x => x.StocktakingSessionNo == s.No);
        var total = await labels.CountAsync(ct);
        var scanned = await labels.CountAsync(x => x.IsScanned, ct);
        return new StocktakingSessionDto(
            s.No, s.SessionName, s.Status, StocktakingSessionStatus.ToApiName(s.Status),
            s.StartedAt, s.CompletedAt, s.AcceptsResults, total, scanned);
    }

    private static void Touch(StocktakingSession s, ClaimsPrincipal user)
    {
        s.UpdatedAt = DateTime.Now;
        s.UpdatedBy = user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
