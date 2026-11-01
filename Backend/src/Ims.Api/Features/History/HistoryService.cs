using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.History;

internal sealed class HistoryService(ImsDbContext db, Print.IMlEngineClient engine, Print.IPrintTargetResolver targets) : IHistoryService
{
    private readonly SortMap<Domain.History.PrintHistory> PrintSort =
        new SortMap<Domain.History.PrintHistory>()
            .Add("no", p => p.No)
            .Add("worked_date", p => p.CreatedAt)
            .Add("created_at", p => p.CreatedAt)
            .Add("category", p => p.Category)
            .Add("doc_no", p => p.DocNo)
            .Add("scheduled_date", p => p.ScheduledDate)
            .Add("item_code", p => p.ItemCode)
            .Add("item_name", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.ItemName).FirstOrDefault())
            .Add("qty", p => p.Qty)
            .Add("uom_code", p => p.UomCode)
            .Add("package_count", p => p.PackageCount)
            .Add("spec", p => p.Spec)
            .Add("status", p => p.Status)
            .Add("created_by", p => p.CreatedBy)
            .Default("worked_date")
            .ThenAlwaysBy(p => p.No);

    private readonly SortMap<Domain.History.RfidGateHistory> RfidSort =
        new SortMap<Domain.History.RfidGateHistory>()
            .Add("no", r => r.No)
            .Add("worked_date", r => r.CreatedAt)
            .Add("created_at", r => r.CreatedAt)
            .Add("item_code", r => r.ItemCode)
            .Add("item_name", r => db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.ItemName).FirstOrDefault())
            .Add("gate_code", r => r.GateCode)
            .Add("serial_no", r => r.SerialNo)
            .Add("lot_no", r => r.LotNo)
            .Add("direction_type", r => r.DirectionType)
            .Add("status", r => r.Status)
            .Default("worked_date")
            .ThenAlwaysBy(r => r.No);

    private const int MaxExportDays = 92;

    private static ServiceResult? ValidateExportRange(DateOnly? from, DateOnly? to)
    {
        if (from is null || to is null)
            return Outcome.BadRequest(new ApiError(new ApiErrorBody(
                "VAL-001", 400, "Error",
                "エクスポートには作業日時の範囲（開始日・終了日）が必要です。")));
        if (to < from)
            return Outcome.BadRequest(new ApiError(new ApiErrorBody(
                "VAL-005", 400, "Error", "終了日は開始日以降を指定してください。")));
        var days = to.Value.DayNumber - from.Value.DayNumber + 1;
        if (days > MaxExportDays)
            return Outcome.BadRequest(new ApiError(new ApiErrorBody(
                "VAL-005", 400, "Error",
                $"エクスポートできる期間は最大 {MaxExportDays} 日です（指定: {days} 日）。")));
        return null;
    }

    private static DateTime? EndOfDay(DateOnly? d) => d?.ToDateTime(TimeOnly.MinValue).AddDays(1);

    private static IQueryable<Domain.History.PrintHistory> PrintQuery(
        ImsDbContext db, DateOnly? dateFrom, DateOnly? dateTo, string? category, int? status, string? q,
        string? item = null)
    {
        var query = db.PrintHistories.AsNoTracking();
        if (dateFrom is { } f) query = query.Where(x => x.CreatedAt >= f.ToDateTime(TimeOnly.MinValue));
        if (EndOfDay(dateTo) is { } t) query = query.Where(x => x.CreatedAt < t);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
        if (status is { } st) query = query.Where(x => x.Status == st);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(x => EF.Functions.Like(x.DocNo!, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(x.ItemCode!, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(x.Spec!, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(x.CreatedBy!, LikeContains(s), LikeEscape));
        }
        if (!string.IsNullOrWhiteSpace(item))
        {
            var s = LikeContains(item.Trim());
            query = query.Where(x => EF.Functions.Like(x.ItemCode!, s, LikeEscape)
                                  || db.Items.Any(i => i.ItemCode == x.ItemCode && EF.Functions.Like(i.ItemName, s, LikeEscape)));
        }
        return query;
    }

    private static IQueryable<PrintHistoryDto> PrintProjected(
        ImsDbContext db, IQueryable<Domain.History.PrintHistory> query)
        => from h in query
           join i in db.Items.AsNoTracking() on h.ItemCode equals i.ItemCode into ij
           from i in ij.DefaultIfEmpty()
           join s in db.Statuses.AsNoTracking() on h.Status equals s.StatusCode into sj
           from s in sj.DefaultIfEmpty()
           join u in db.AppUsers.AsNoTracking() on h.CreatedBy equals u.UserCode into uj
           from u in uj.DefaultIfEmpty()
           select new PrintHistoryDto(
               h.No, h.CreatedAt, h.Category, h.DocNo, h.ScheduledDate,
               h.ItemCode, i != null ? i.ItemName : null, h.Qty, h.UomCode, h.PackageCount,
               h.Spec, h.Status, s != null ? s.StatusNameLang1 : null, h.CreatedBy,
               u != null ? u.UserName : null);

    public async Task<ServiceResult> ListPrintAsync(
        CancellationToken ct, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? category = null,
        int? status = null, string? q = null, string? sort = null, int? page = null, int? pageSize = null,
        string? item = null)
    {
        var query = PrintSort.Apply(PrintQuery(db, dateFrom, dateTo, category, status, q, item), sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await PrintProjected(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<PrintHistoryDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> CreatePrintAsync(
        PrintHistoryWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var category = NullIfBlank(body.Category)?.Trim();
        if (category is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));
        if (body.Qty is not { } qty || qty <= 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var docNo = NullIfBlank(body.DocNo)?.Trim();
        var itemCode = NullIfBlank(body.ItemCode)?.Trim();
        if (!body.Reprint && docNo is not null && itemCode is not null)
        {
            var already = await db.PrintHistories.AnyAsync(
                h => h.Category == category && h.DocNo == docNo && h.ItemCode == itemCode, ct);
            if (already) return Outcome.Conflict(new ApiError(ErrorCatalog.Dup002()));
        }

        var row = new Domain.History.PrintHistory
        {
            Category = body.Reprint ? category + ReprintSuffix : category,
            CreatedAt = DateTime.Now,
            DocNo = docNo,
            ScheduledDate = body.ScheduledDate,
            ItemCode = itemCode,
            Qty = qty,
            UomCode = NullIfBlank(body.UomCode)?.Trim(),
            PackageCount = Math.Max(1, body.PackageCount),
            Spec = NullIfBlank(body.Spec)?.Trim(),
            Status = body.Reprint ? ReprintedStatus : PrintedStatus,
            CreatedBy = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        };
        db.PrintHistories.Add(row);
        await db.SaveChangesAsync(ct);

        var dto = await PrintProjected(db, db.PrintHistories.AsNoTracking().Where(h => h.No == row.No))
            .FirstOrDefaultAsync(ct);
        return Outcome.Created($"/print-history?q={row.DocNo}", new ApiSuccess<PrintHistoryDto>(dto!));
    }

    public async Task<ServiceResult> ReprintAsync(
        long no, PrintReprintRequest body, ClaimsPrincipal user, CancellationToken ct)
    {
        var source = await db.PrintHistories.AsNoTracking().FirstOrDefaultAsync(h => h.No == no, ct);
        if (source is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Print history")));
        if (body.Count < 1) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var baseCategory = source.Category.EndsWith(ReprintSuffix, StringComparison.Ordinal)
            ? source.Category[..^ReprintSuffix.Length]
            : source.Category;
        var (target, refused) = await targets.ResolveAsync(
            targets.LayoutKeyFor(baseCategory), body.DeviceCode, ct);
        if (refused is not null) return refused;

        try
        {
            await engine.PrintAsync(target!.Value.LayoutFile, target.Value.Setting, body.Fields, body.Count, ct);
        }
        catch (Exception e) when (e is Print.MlEngineException or HttpRequestException or TaskCanceledException)
        {
            return Outcome.Json(new ApiError(ErrorCatalog.Prn002()), statusCode: 500);
        }

        var row = new Domain.History.PrintHistory
        {
            Category = baseCategory + ReprintSuffix,
            CreatedAt = DateTime.Now,
            DocNo = source.DocNo,
            ScheduledDate = source.ScheduledDate,
            ItemCode = source.ItemCode,
            Qty = source.Qty,
            UomCode = source.UomCode,
            PackageCount = body.Count,
            Spec = body.DeviceCode.Trim(),
            Status = ReprintedStatus,
            CreatedBy = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        };
        db.PrintHistories.Add(row);
        await db.SaveChangesAsync(ct);

        var dto = await PrintProjected(db, db.PrintHistories.AsNoTracking().Where(h => h.No == row.No))
            .FirstOrDefaultAsync(ct);
        return Outcome.Created($"/print-history?q={row.DocNo}", new ApiSuccess<PrintHistoryDto>(dto!));
    }

    private const string ReprintSuffix = "Reprint";

    private const int ReprintedStatus = 20;

    private const int PrintedStatus = 19;

    public async Task<ServiceResult> ExportPrintCsvAsync(
        CancellationToken ct, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? category = null,
        int? status = null, string? q = null, string? sort = null, string? item = null)
    {
        if (ValidateExportRange(dateFrom, dateTo) is { } bad) return bad;
        var query = PrintSort.Apply(PrintQuery(db, dateFrom, dateTo, category, status, q, item), sort);
        return CsvStream(
            "print_history.csv",
            "no,worked_date,category,doc_no,scheduled_date,item_code,item_name,qty,uom_code,package_count,spec,status,created_by,worker_name",
            PrintProjected(db, query).AsAsyncEnumerable(),
            r => new StringBuilder()
                .Append(r.No).Append(',')
                .Append(Csv(r.WorkedDate.ToString("yyyy-MM-dd HH:mm:ss"))).Append(',')
                .Append(Csv(r.Category)).Append(',').Append(Csv(r.DocNo)).Append(',')
                .Append(Csv(r.ScheduledDate?.ToString("yyyy-MM-dd"))).Append(',')
                .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
                .Append(Num(r.Qty)).Append(',').Append(Csv(r.UomCode)).Append(',')
                .Append(r.PackageCount).Append(',').Append(Csv(r.Spec)).Append(',')
                .Append(Csv(r.StatusName)).Append(',').Append(Csv(r.CreatedBy)).Append(',')
                .Append(Csv(r.WorkerName))
                .ToString());
    }

    private static IQueryable<Domain.History.RfidGateHistory> RfidQuery(
        ImsDbContext db, DateOnly? dateFrom, DateOnly? dateTo, string? q)
    {
        var query = db.RfidGateHistories.AsNoTracking();
        if (dateFrom is { } f) query = query.Where(x => x.CreatedAt >= f.ToDateTime(TimeOnly.MinValue));
        if (EndOfDay(dateTo) is { } t) query = query.Where(x => x.CreatedAt < t);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(x => EF.Functions.Like(x.ItemCode!, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(x.SerialNo!, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(x.GateCode, LikeContains(s), LikeEscape));
        }
        return query;
    }

    private static IQueryable<RfidGateHistoryDto> RfidProjected(
        ImsDbContext db, IQueryable<Domain.History.RfidGateHistory> query)
        => from h in query
           join i in db.Items.AsNoTracking() on h.ItemCode equals i.ItemCode into ij
           from i in ij.DefaultIfEmpty()
           select new RfidGateHistoryDto(
               h.No, h.CreatedAt, h.ItemCode, i != null ? i.ItemName : null,
               h.GateCode, h.SerialNo, h.LotNo, h.DirectionType, h.Status);

    public async Task<ServiceResult> ListRfidAsync(
        CancellationToken ct, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? q = null,
        string? sort = null, int? page = null, int? pageSize = null)
    {
        var query = RfidSort.Apply(RfidQuery(db, dateFrom, dateTo, q), sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await RfidProjected(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<RfidGateHistoryDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> ExportRfidCsvAsync(
        CancellationToken ct, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? q = null,
        string? sort = null)
    {
        if (ValidateExportRange(dateFrom, dateTo) is { } bad) return bad;
        var query = RfidSort.Apply(RfidQuery(db, dateFrom, dateTo, q), sort);
        return CsvStream(
            "rfid_gate_history.csv",
            "no,worked_date,item_code,item_name,gate_code,epc,lot_no,direction_type",
            RfidProjected(db, query).AsAsyncEnumerable(),
            r => new StringBuilder()
                .Append(r.No).Append(',')
                .Append(Csv(r.WorkedDate.ToString("yyyy-MM-dd HH:mm:ss"))).Append(',')
                .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
                .Append(Csv(r.GateCode)).Append(',').Append(Csv(r.SerialNo)).Append(',')
                .Append(Csv(r.LotNo)).Append(',').Append(Csv(r.DirectionType))
                .ToString());
    }
}
