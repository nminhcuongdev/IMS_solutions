using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Domain.Returns;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Returns;

internal sealed class ReturnsService(ImsDbContext db) : IReturnsService
{
    private const int MaxReturnLength = 30;

    internal const string PrintCategory = "Returns";

    private readonly SortMap<ReturnPlan> PlanSort = new SortMap<ReturnPlan>()
        .Add("return_no", p => p.ReturnNo)
        .Add("return_branch_no", p => p.ReturnBranchNo)
        .Add("partner_code", p => p.PartnerCode)
        .Add("item_code", p => p.ItemCode)
        .Add("qty", p => p.Qty)
        .Add("actual_qty", p => p.ActualQty)
        .Add("scheduled_date", p => p.ScheduledDate)
        .Add("status", p => p.Status)
        .Add("invoice_no", p => p.InvoiceNo)
        .Add("created_at", p => p.CreatedAt)
        .Add("no", p => p.No)
        .Add("item_name", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("unit", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("shortage_reason", p => p.ReasonCode)
        .Add("created_by", p => p.CreatedBy)
        .Default("scheduled_date")
        .ThenAlwaysBy(p => p.No);

    private static readonly SortMap<ReturnResult> ResultSort = new SortMap<ReturnResult>()
        .Add("created_at", r => r.CreatedAt)
        .Add("return_no", r => r.ReturnNo)
        .Add("item_code", r => r.ItemCode)
        .Add("qty", r => r.Qty)
        .Add("location_code", r => r.LocationCode)
        .Default("created_at")
        .ThenAlwaysBy(r => r.No);

    public async Task<ServiceResult> ListPlansAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? returnNo = null, string? status = null)
    {
        var query = db.ReturnPlans.AsNoTracking();

        if (dateFrom is { } from) query = query.Where(p => p.ScheduledDate >= from);
        if (dateTo is { } to) query = query.Where(p => p.ScheduledDate <= to);
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(p => EF.Functions.Like(p.ItemCode, LikeContains(ic), LikeEscape));
        if (Trimmed(returnNo) is { } rn)
            query = query.Where(p => EF.Functions.Like(p.ReturnNo, LikeContains(rn), LikeEscape));
        if (Trimmed(itemName) is { } iname)
            query = query.Where(p => db.Items.Any(i =>
                i.ItemCode == p.ItemCode && EF.Functions.Like(i.ItemName, LikeContains(iname), LikeEscape)));
        if (Trimmed(q) is { } free)
            query = query.Where(p => EF.Functions.Like(p.ReturnNo, LikeContains(free), LikeEscape)
                                  || EF.Functions.Like(p.ItemCode, LikeContains(free), LikeEscape));
        if (ReturnStatus.FromApiName(Trimmed(status)) is { } st)
            query = query.Where(p => p.Status == st);

        query = PlanSort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await ProjectPlans(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ReturnPlanDto>(
            rows.Select(MapPlan).ToList(), new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> CreatePlanAsync(ReturnPlanWriteDto body, CancellationToken ct, ClaimsPrincipal? user = null)
    {
        if (ValidateCode(body.ReturnNo, MaxReturnLength) is { } e1) return e1;
        if (ValidateCode(body.ItemCode) is { } e2) return e2;
        if (body.ExpectedQty < 1) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var itemCode = body.ItemCode.Trim();
        if (!await db.Items.AnyAsync(i => i.ItemCode == itemCode && !i.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));

        var partnerCode = NullIfBlank(body.ReturnedByCode)?.Trim();
        if (partnerCode is not null
            && !await db.Partners.AnyAsync(p => p.PartnerCode == partnerCode && !p.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Partner")));

        var factoryCode = NullIfBlank(body.FactoryCode)?.Trim();
        if (factoryCode is not null
            && !await db.Factories.AnyAsync(f => f.FactoryCode == factoryCode && !f.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")));

        var plan = new ReturnPlan
        {
            ReturnNo = body.ReturnNo.Trim(),
            ReturnBranchNo = NullIfBlank(body.SerialNo)?.Trim() ?? "01",
            PartnerCode = partnerCode,
            ItemCode = itemCode,
            Qty = body.ExpectedQty,
            ActualQty = 0,
            FactoryCode = factoryCode,
            ScheduledDate = body.ReturnDate ?? DateOnly.FromDateTime(DateTime.Now),
            Status = ReturnStatus.Pending,
            InvoiceNo = NullIfBlank(body.InvoiceNo)?.Trim(),
            LotNo = NullIfBlank(body.LotNo)?.Trim(),
            CreatedAt = DateTime.Now,
            CreatedBy = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
        };
        if (await db.ReturnPlans.AnyAsync(x => x.ReturnNo == plan.ReturnNo && x.ReturnBranchNo == plan.ReturnBranchNo, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        db.ReturnPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        var dto = await LoadPlanDtoAsync(db, plan.No, ct);
        return Outcome.Created($"/returns-schedule?returnNo={plan.ReturnNo}",
            new ApiSuccess<ReturnPlanDto>(dto!));
    }

    public async Task<ServiceResult> ChangeStatusAsync(ReturnStatusChangeDto body, CancellationToken ct)
    {
        var lines = (body.Lines ?? Array.Empty<ReturnPlanKeyDto>())
            .Select(l => (No: Trimmed(l.ReturnNo), Serial: Trimmed(l.SerialNo) ?? "01"))
            .Where(l => l.No is not null).Distinct().ToList();
        var returnNos = lines.Count > 0
            ? lines.Select(l => l.No).Distinct().ToList()
            : body.ReturnNos?.Select(x => x?.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList()
              ?? new List<string?>();
        if (returnNos.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));

        var target = ActionToStatus(Trimmed(body.Status));
        if (target is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel006()));

        var rows = await db.ReturnPlans.Where(p => returnNos.Contains(p.ReturnNo)).ToListAsync(ct);
        IEnumerable<string> missing;
        Func<ReturnPlan, string> label;
        if (lines.Count > 0)
        {
            rows = rows.Where(r => lines.Contains((r.ReturnNo, r.ReturnBranchNo))).ToList();
            missing = lines.Where(l => !rows.Any(r => r.ReturnNo == l.No && r.ReturnBranchNo == l.Serial))
                .Select(l => $"{l.No}/{l.Serial}");
            label = r => $"{r.ReturnNo}/{r.ReturnBranchNo}";
        }
        else
        {
            missing = returnNos.Where(r => rows.All(x => x.ReturnNo != r)).Select(r => r!);
            label = r => r.ReturnNo;
        }

        var skipped = new SortedSet<string>(StringComparer.Ordinal);
        var updated = 0;
        var now = DateTime.Now;
        foreach (var row in rows)
        {
            var allowed = target.Value switch
            {
                ReturnStatus.Pending => row.Status != ReturnStatus.Returned && row.ActualQty == 0,
                _ => row.Status is not (ReturnStatus.Returned or ReturnStatus.Cancelled),
            };
            if (!allowed)
            {
                skipped.Add(label(row));
                continue;
            }
            row.ActualQty = target.Value switch
            {
                ReturnStatus.Returned => row.Qty,
                ReturnStatus.Pending => 0,
                _ => row.ActualQty,
            };
            row.Status = target.Value;
            row.UpdatedAt = now;
            updated++;
        }
        foreach (var m in missing) skipped.Add(m);

        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<ReturnStatusChangeResult>(
            new ReturnStatusChangeResult(updated, skipped.ToList())));
    }

    private static int? ActionToStatus(string? action) => action switch
    {
        "done" => ReturnStatus.Returned,
        "notDone" => ReturnStatus.Pending,
        "cancelled" => ReturnStatus.Cancelled,
        _ => null,
    };

    public async Task<ServiceResult> RegisterShortageAsync(
        string returnNo, ReturnShortageDto body, CancellationToken ct, ClaimsPrincipal? user = null)
    {
        if (body.ShortageQty is not { } shortageQty || shortageQty < 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var reasonCode = NullIfBlank(body.ReasonCode)?.Trim();
        if (reasonCode is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));
        if (!await db.Reasons.AnyAsync(r => r.ReasonCode == reasonCode && !r.IsDeleted
                                         && r.ReasonCategoryValue == ReasonCategory.Shortage, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")));

        var code = returnNo.Trim();
        var serialNo = NullIfBlank(body.SerialNo)?.Trim();
        var candidates = db.ReturnPlans.Where(p =>
            p.ReturnNo == code
            && p.Status != ReturnStatus.Returned
            && p.Status != ReturnStatus.Cancelled);
        if (serialNo is not null) candidates = candidates.Where(p => p.ReturnBranchNo == serialNo);

        var open = await candidates.ToListAsync(ct);
        if (open.Count != 1) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Return plan")));

        var plan = open[0];
        if (shortageQty > plan.Qty) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));
        var returned = plan.Qty - shortageQty;
        if (returned < plan.ActualQty) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var now = DateTime.Now;
        var worker = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (returned > plan.ActualQty)
        {
            var preferred = await db.Items.Where(i => i.ItemCode == plan.ItemCode)
                .Select(i => i.DefaultLocationCode).FirstOrDefaultAsync(ct);
            var shelf = preferred is null
                ? null
                : await db.Locations.Where(l => l.LocationCode == preferred && !l.IsDeleted)
                    .Select(l => l.LocationCode).FirstOrDefaultAsync(ct);
            if (shelf is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Loc001()));

            var row = new ReturnResult
            {
                ReturnNo = plan.ReturnNo,
                ReturnBranchNo = plan.ReturnBranchNo,
                PartnerCode = plan.PartnerCode,
                ItemCode = plan.ItemCode,
                Qty = returned,
                LocationCode = shelf,
                LotNo = plan.LotNo,
                ReasonCode = reasonCode,
                CreatedAt = now,
                CreatedBy = worker,
            };
            db.ReturnResults.Add(row);
            await db.SaveChangesAsync(ct);
            await PutBackAsync(row, returned - plan.ActualQty, plan.FactoryCode, worker, now, ct);
        }

        plan.ActualQty = returned;
        plan.Status = ReturnStatus.Partial;
        plan.ReasonCode = reasonCode;
        plan.UpdatedAt = now;
        if (worker is not null) plan.UpdatedBy = worker;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var dto = await LoadPlanDtoAsync(db, plan.No, ct);
        return Outcome.Ok(new ApiSuccess<ReturnPlanDto>(dto!));
    }

    public async Task<ServiceResult> ListResultsAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, DateOnly? dateFrom = null,
        DateOnly? dateTo = null, string? itemCode = null, string? returnNo = null)
    {
        var query = db.ReturnResults.AsNoTracking();

        if (dateFrom is { } from)
            query = query.Where(r => r.CreatedAt >= from.ToDateTime(TimeOnly.MinValue));
        if (dateTo is { } to)
            query = query.Where(r => r.CreatedAt < to.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(r => EF.Functions.Like(r.ItemCode, LikeContains(ic), LikeEscape));
        if (Trimmed(returnNo) is { } rn)
            query = query.Where(r => r.ReturnNo != null && EF.Functions.Like(r.ReturnNo, LikeContains(rn), LikeEscape));

        query = ResultSort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await ProjectResults(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ReturnResultDto>(
            rows.Select(MapResult).ToList(), new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> CreateResultAsync(
        ReturnResultWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (ValidateCode(body.ItemCode) is { } e1) return e1;
        if (body.Qty is not { } qty || qty < 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var itemCode = body.ItemCode.Trim();
        if (!await db.Items.AnyAsync(i => i.ItemCode == itemCode && !i.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));

        var locationCode = NullIfBlank(body.LocationCode)?.Trim();
        if (locationCode is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Loc001()));
        if (!await db.Locations.AnyAsync(l => l.LocationCode == locationCode && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));

        var lotNo = NullIfBlank(body.LotNo)?.Trim();
        if (lotNo is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Lot001()));

        var returnNo = NullIfBlank(body.ReturnNo)?.Trim();
        ReturnPlan? plan = null;

        if (returnNo is not null)
        {
            var candidates = db.ReturnPlans.Where(p =>
                p.ReturnNo == returnNo
                && p.ItemCode == itemCode
                && p.Status != ReturnStatus.Returned
                && p.Status != ReturnStatus.Cancelled);
            var serialNo = NullIfBlank(body.SerialNo)?.Trim();
            if (serialNo is not null) candidates = candidates.Where(p => p.ReturnBranchNo == serialNo);

            var open = await candidates.ToListAsync(ct);
            if (open.Count == 0)
            {
                var doneQty = await db.ReturnPlans
                    .Where(p => p.ReturnNo == returnNo && p.ItemCode == itemCode && p.Status == ReturnStatus.Returned
                                && (serialNo == null || p.ReturnBranchNo == serialNo))
                    .Select(p => (decimal?)p.Qty).FirstOrDefaultAsync(ct);
                if (doneQty is { } planned && qty > planned)
                    return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty002()));
            }
            if (open.Count != 1)
                return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Return plan")));

            plan = open[0];
            if (qty > plan.Qty) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty002()));
        }

        var added = qty - (plan?.ActualQty ?? 0m);
        if (added < 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var now = DateTime.Now;
        var worker = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var row = new ReturnResult
        {
            ReturnNo = plan?.ReturnNo,
            ReturnBranchNo = plan?.ReturnBranchNo,
            PartnerCode = plan?.PartnerCode,
            ItemCode = itemCode,
            Qty = qty,
            LocationCode = locationCode,
            LotNo = lotNo,
            CreatedAt = now,
            CreatedBy = worker,
        };
        db.ReturnResults.Add(row);

        if (plan is not null)
        {
            plan.ActualQty = qty;
            plan.Status = qty >= plan.Qty ? ReturnStatus.Returned : ReturnStatus.Partial;
            plan.UpdatedAt = now;
            plan.UpdatedBy = worker;
        }

        await db.SaveChangesAsync(ct);
        await PutBackAsync(row, added, plan?.FactoryCode, worker, now, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var dto = await LoadResultDtoAsync(db, row.No, ct);
        return Outcome.Created($"/return-result?returnNo={row.ReturnNo}",
            new ApiSuccess<ReturnResultDto>(dto!));
    }

    private async Task PutBackAsync(
        ReturnResult row, decimal qty, string? planFactory, string? worker, DateTime now, CancellationToken ct)
    {
        if (qty <= 0) return;

        var factoryCode = planFactory
            ?? await db.Locations
                .Where(l => l.LocationCode == row.LocationCode && l.ZoneCode != null)
                .Join(db.Zones, l => l.ZoneCode, z => z.ZoneCode, (l, z) => z.FactoryCode)
                .FirstOrDefaultAsync(ct)
            ?? await db.Factories.Where(f => !f.IsDeleted).OrderBy(f => f.FactoryCode)
                .Select(f => f.FactoryCode).FirstAsync(ct);

        var stock = new Domain.Inventory.Stock
        {
            ItemCode = row.ItemCode,
            SerialNo = $"RTN-{row.No:D10}",
            FactoryCode = factoryCode,
            LocationCode = row.LocationCode,
            LotNo = row.LotNo,
            StockQty = qty,
            UpdatedAt = now,
            UpdatedBy = worker,
        };
        stock.Recalculate();
        db.Stocks.Add(stock);
        db.InventoryHistories.Add(new Domain.Inventory.InventoryHistory
        {
            CreatedAt = DateTime.Now,
            ItemCode = row.ItemCode,
            FactoryCode = factoryCode,
            LocationCode = row.LocationCode,
            LotNo = row.LotNo,
            SerialNo = stock.SerialNo,
            ActionType = Domain.Inventory.InventoryAction.Returns,
            Qty = qty,
            ReasonCode = row.ReasonCode,
            CreatedBy = worker,
        });
    }

    public async Task<ServiceResult> ExportPlansCsvAsync(CancellationToken ct)
    {
        var rows = await ProjectPlans(db,
            db.ReturnPlans.AsNoTracking().OrderBy(p => p.ScheduledDate).ThenBy(p => p.No)).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("return_no,return_branch_no,partner_code,partner_name,item_code,item_name," +
                      "qty,unit,actual_qty,scheduled_date,invoice_no,status,reason_code,lot_no," +
                      "created_at,created_by");
        foreach (var r in rows)
            sb.Append(Csv(r.ReturnNo)).Append(',').Append(Csv(r.SerialNo)).Append(',')
              .Append(Csv(r.ReturnedByCode)).Append(',').Append(Csv(r.ReturnedByName)).Append(',')
              .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
              .Append(Num(r.ExpectedQty)).Append(',').Append(Csv(r.Unit)).Append(',')
              .Append(Num(r.ActualQty)).Append(',')
              .Append(r.ReturnDate?.ToString("yyyy-MM-dd") ?? "").Append(',')
              .Append(Csv(r.InvoiceNo)).Append(',')
              .Append(Csv(ReturnStatus.ToApiName(r.Status))).Append(',')
              .Append(Csv(r.ShortageReason)).Append(',').Append(Csv(r.LotNo)).Append(',')
              .Append(r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(Csv(r.CreatedBy)).AppendLine();
        return CsvFile(sb, "returns-schedule.csv");
    }

    private sealed record PlanRow(
        string ReturnNo, string SerialNo, string? ReturnedByCode, string? ReturnedByName,
        string ItemCode, string? ItemName, decimal ExpectedQty, string? Unit, decimal ActualQty,
        DateOnly? ReturnDate, string? InvoiceNo, int Status, string? ShortageReason,
        string? LotNo, bool LabelIssued, DateTime CreatedAt, string? CreatedBy);

    private sealed record ResultRow(
        long No, DateTime CreatedAt, string ItemCode, string? ItemName, decimal Qty, string? Unit,
        string? ReturnNo, string? SerialNo, string? ReturnedByCode, string? ReturnedByName,
        string? LocationCode, string? LotNo, string? WorkerName, string? CreatedBy,
        int? PlanStatus, string? ShortageReason);

    private static IQueryable<PlanRow> ProjectPlans(ImsDbContext db, IQueryable<ReturnPlan> q) =>
        q.Select(p => new PlanRow(
            p.ReturnNo,
            p.ReturnBranchNo,
            p.PartnerCode,
            db.Partners.Where(x => x.PartnerCode == p.PartnerCode).Select(x => x.PartnerName).FirstOrDefault(),
            p.ItemCode,
            db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            p.Qty,
            db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            p.ActualQty,
            p.ScheduledDate,
            p.InvoiceNo,
            p.Status,
            p.ReasonCode,
            p.LotNo,
            db.PrintHistories.Any(h => h.Category == PrintCategory
                                    && h.DocNo == p.ReturnNo && h.ItemCode == p.ItemCode),
            p.CreatedAt,
            p.CreatedBy));

    private static IQueryable<ResultRow> ProjectResults(ImsDbContext db, IQueryable<ReturnResult> q) =>
        q.Select(r => new ResultRow(
            r.No,
            r.CreatedAt,
            r.ItemCode,
            db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            r.Qty,
            db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            r.ReturnNo,
            r.ReturnBranchNo,
            r.PartnerCode,
            db.Partners.Where(x => x.PartnerCode == r.PartnerCode).Select(x => x.PartnerName).FirstOrDefault(),
            r.LocationCode,
            r.LotNo,
            db.AppUsers.Where(u => u.UserCode == r.CreatedBy).Select(u => u.UserName).FirstOrDefault(),
            r.CreatedBy,
            db.ReturnPlans
                .Where(p => p.ReturnNo == r.ReturnNo && p.ReturnBranchNo == r.ReturnBranchNo)
                .Select(p => (int?)p.Status).FirstOrDefault(),
            db.ReturnPlans
                .Where(p => p.ReturnNo == r.ReturnNo && p.ReturnBranchNo == r.ReturnBranchNo)
                .Select(p => p.ReasonCode).FirstOrDefault()));

    private static ReturnPlanDto MapPlan(PlanRow r) => new(
        r.ReturnNo, r.SerialNo, r.ReturnedByCode, r.ReturnedByName, r.ItemCode, r.ItemName,
        r.ExpectedQty, r.Unit, r.ActualQty, r.ReturnDate, r.InvoiceNo,
        ReturnStatus.ToApiName(r.Status), r.ShortageReason, r.LotNo, r.LabelIssued,
        r.CreatedAt, r.CreatedBy);

    private static ReturnResultDto MapResult(ResultRow r) => new(
        r.No, r.CreatedAt, r.ItemCode, r.ItemName, r.Qty, r.Unit, r.ReturnNo, r.SerialNo,
        r.ReturnedByCode, r.ReturnedByName, r.LocationCode, r.LotNo,
        r.WorkerName ?? r.CreatedBy,
        ReturnStatus.ToApiName(r.PlanStatus ?? ReturnStatus.Returned),
        r.ShortageReason);

    private static async Task<ReturnPlanDto?> LoadPlanDtoAsync(ImsDbContext db, long no, CancellationToken ct)
    {
        var row = await ProjectPlans(db, db.ReturnPlans.AsNoTracking().Where(p => p.No == no))
            .FirstOrDefaultAsync(ct);
        return row is null ? null : MapPlan(row);
    }

    private static async Task<ReturnResultDto?> LoadResultDtoAsync(ImsDbContext db, long no, CancellationToken ct)
    {
        var row = await ProjectResults(db, db.ReturnResults.AsNoTracking().Where(r => r.No == no))
            .FirstOrDefaultAsync(ct);
        return row is null ? null : MapResult(row);
    }

    private static string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
