using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Picking;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Picking;

internal sealed class PickingService(ImsDbContext db) : IPickingService
{
    private const int MaxSlipLength = 30;

    private readonly SortMap<PickingPlan> PlanSort = new SortMap<PickingPlan>()
        .Add("so_no", p => p.SoNo)
        .Add("so_branch_no", p => p.SoBranchNo)
        .Add("item_code", p => p.ItemCode)
        .Add("location_code", p => p.LocationCode)
        .Add("qty", p => p.Qty)
        .Add("picked_qty", p => p.PickedQty)
        .Add("scheduled_date", p => p.ScheduledDate)
        .Add("status", p => p.Status)
        .Add("created_at", p => p.CreatedAt)
        .Add("no", p => p.No)
        .Add("item_name", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("unit", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("created_by", p => p.CreatedBy)
        .Default("scheduled_date")
        .ThenAlwaysBy(p => p.No);

    private readonly SortMap<PickingResult> ResultSort = new SortMap<PickingResult>()
        .Add("created_at", r => r.CreatedAt)
        .Add("so_no", r => r.SoNo)
        .Add("item_code", r => r.ItemCode)
        .Add("qty", r => r.Qty)
        .Add("location_code", r => r.LocationCode)
        .Add("no", r => r.No)
        .Add("item_name", r => db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("unit", r => db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("serial_no", r => r.SoBranchNo)
        .Add("scheduled_date", r => db.PickingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo).Select(p => p.ScheduledDate).FirstOrDefault())
        .Add("worker", r => db.AppUsers.Where(u => u.UserCode == r.CreatedBy).Select(u => u.UserName).FirstOrDefault() ?? r.CreatedBy)
        .Default("created_at")
        .ThenAlwaysBy(r => r.No);

    public async Task<ServiceResult> ListPlansAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? soNo = null, string? status = null)
    {
        var query = db.PickingPlans.AsNoTracking();

        if (dateFrom is { } from) query = query.Where(p => p.ScheduledDate >= from);
        if (dateTo is { } to) query = query.Where(p => p.ScheduledDate <= to);
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(p => EF.Functions.Like(p.ItemCode, LikeContains(ic), LikeEscape));
        if (Trimmed(soNo) is { } sn)
            query = query.Where(p => EF.Functions.Like(p.SoNo, LikeContains(sn), LikeEscape));
        if (Trimmed(itemName) is { } iname)
            query = query.Where(p => db.Items.Any(i =>
                i.ItemCode == p.ItemCode && EF.Functions.Like(i.ItemName, LikeContains(iname), LikeEscape)));
        if (Trimmed(q) is { } free)
            query = query.Where(p => EF.Functions.Like(p.SoNo, LikeContains(free), LikeEscape)
                                  || EF.Functions.Like(p.ItemCode, LikeContains(free), LikeEscape));
        if (PickingStatus.FromApiName(Trimmed(status)) is { } st)
            query = query.Where(p => p.Status == st);

        query = PlanSort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await ProjectPlans(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<PickingPlanDto>(
            rows.Select(MapPlan).ToList(), new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> CreatePlanAsync(
        PickingPlanWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (ValidateCode(body.SoNo, MaxSlipLength) is { } e1) return e1;
        if (ValidateCode(body.ItemCode) is { } e2) return e2;
        if (body.PlannedQty < 1) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var itemCode = body.ItemCode.Trim();
        if (!await db.Items.AnyAsync(i => i.ItemCode == itemCode && !i.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));

        var locationCode = NullIfBlank(body.LocationCode)?.Trim();
        if (locationCode is not null
            && !await db.Locations.AnyAsync(l => l.LocationCode == locationCode && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));

        var plan = new PickingPlan
        {
            SoNo = body.SoNo.Trim(),
            SoBranchNo = NullIfBlank(body.SerialNo)?.Trim() ?? "01",
            ItemCode = itemCode,
            LocationCode = locationCode,
            Qty = body.PlannedQty,
            PickedQty = 0,
            ScheduledDate = body.ScheduledDate,
            Status = PickingStatus.NotPicked,
            CreatedAt = DateTime.Now,
            CreatedBy = user.FindFirstValue(ClaimTypes.NameIdentifier),
        };
        if (await db.PickingPlans.AnyAsync(x => x.SoNo == plan.SoNo && x.SoBranchNo == plan.SoBranchNo, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        db.PickingPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        var dto = await LoadPlanDtoAsync(db, plan.No, ct);
        return Outcome.Created($"/picking-schedule/{plan.No}", new ApiSuccess<PickingPlanDto>(dto!));
    }

    public async Task<ServiceResult> ChangeStatusAsync(
        PickingStatusChangeDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var lines = (body.Lines ?? Array.Empty<PickingPlanKeyDto>())
            .Select(l => (No: Trimmed(l.SoNo), Serial: Trimmed(l.SerialNo) ?? "01"))
            .Where(l => l.No is not null).Distinct().ToList();
        var soNos = lines.Count > 0
            ? lines.Select(l => l.No!).Distinct().ToList()
            : (body.SoNos ?? Array.Empty<string>())
                .Select(x => x?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!)
                .Distinct().ToList();
        if (soNos.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel006()));

        var target = Trimmed(body.Status) switch
        {
            "done" => PickingStatus.Picked,
            "notDone" => PickingStatus.NotPicked,
            "cancelled" => PickingStatus.Cancelled,
            _ => (int?)null,
        };
        if (target is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel006()));

        var plans = await db.PickingPlans.Where(p => soNos.Contains(p.SoNo)).ToListAsync(ct);
        if (lines.Count > 0) plans = plans.Where(p => lines.Contains((p.SoNo, p.SoBranchNo))).ToList();
        Func<PickingPlan, string> label = lines.Count > 0 ? p => $"{p.SoNo}/{p.SoBranchNo}" : p => p.SoNo;
        var requested = lines.Count > 0 ? lines.Select(l => $"{l.No}/{l.Serial}").ToList() : soNos;
        var who = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var updated = 0;
        var touched = new HashSet<string>();

        foreach (var p in plans)
        {
            if (p.Status == PickingStatus.Picked || p.Status == PickingStatus.Cancelled) continue;
            if (p.Status == target) continue;
            p.Status = target.Value;
            p.UpdatedAt = DateTime.Now;
            p.UpdatedBy = who;
            updated++;
            touched.Add(label(p));
        }
        await db.SaveChangesAsync(ct);

        var skipped = requested.Where(x => !touched.Contains(x)).ToList();
        return Outcome.Ok(new ApiSuccess<PickingStatusChangeResult>(
            new PickingStatusChangeResult(updated, skipped)));
    }

    public async Task<ServiceResult> ListResultsAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, DateOnly? dateFrom = null,
        DateOnly? dateTo = null, string? itemCode = null, string? itemName = null, string? soNo = null)
    {
        var query = db.PickingResults.AsNoTracking();

        if (dateFrom is { } from)
            query = query.Where(r => r.CreatedAt >= from.ToDateTime(TimeOnly.MinValue));
        if (dateTo is { } to)
            query = query.Where(r => r.CreatedAt < to.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(r => EF.Functions.Like(r.ItemCode, LikeContains(ic), LikeEscape));
        if (Trimmed(soNo) is { } sn)
            query = query.Where(r => EF.Functions.Like(r.SoNo, LikeContains(sn), LikeEscape));
        if (Trimmed(itemName) is { } iname)
            query = query.Where(r => db.Items.Any(i =>
                i.ItemCode == r.ItemCode && EF.Functions.Like(i.ItemName, LikeContains(iname), LikeEscape)));

        query = ResultSort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await ProjectResults(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<PickingResultDto>(
            rows.Select(MapResult).ToList(), new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> CreateResultAsync(
        PickingResultWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (ValidateCode(body.ItemCode) is { } e1) return e1;
        if (body.Qty is not { } qty || qty < 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var itemCode = body.ItemCode.Trim();
        if (!await db.Items.AnyAsync(i => i.ItemCode == itemCode && !i.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));

        var soNo = NullIfBlank(body.SoNo)?.Trim();
        if (soNo is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel006()));

        var candidates = db.PickingPlans.Where(p =>
            p.SoNo == soNo
            && p.ItemCode == itemCode
            && p.Status != PickingStatus.Picked
            && p.Status != PickingStatus.Cancelled);

        var serialNo = NullIfBlank(body.SerialNo)?.Trim();
        if (serialNo is not null) candidates = candidates.Where(p => p.SoBranchNo == serialNo);

        var open = await candidates.OrderBy(p => p.SoBranchNo).ToListAsync(ct);
        if (open.Count == 0) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("PickingPlan")));
        if (open.Count > 1 && serialNo is null)
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("PickingPlan")));

        var plan = open[0];
        if (qty > plan.Qty) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty002()));

        var locationCode = NullIfBlank(body.LocationCode)?.Trim() ?? plan.LocationCode;
        if (locationCode is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Loc001()));
        if (!await db.Locations.AnyAsync(l => l.LocationCode == locationCode && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));

        var delta = qty - plan.PickedQty;
        if (delta < 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));
        var worker = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var lotNo = NullIfBlank(body.LotNo)?.Trim();
        var taken = new List<Domain.Inventory.Stock>();
        var allocations = new List<Inventory.StockAllocation>();
        if (await Inventory.StockMovement.TakeAsync(db, itemCode, lotNo, locationCode, delta,
                Domain.Inventory.InventoryAction.Picking, null, worker, ErrorCatalog.Loc003(), ct, taken, allocations) is { } refused)
            return refused;

        var lots = taken.Select(s => s.LotNo).ToList();
        if (plan.PickedQty > 0)
            lots.Add(await db.PickingResults
                .Where(r => r.SoNo == plan.SoNo && r.SoBranchNo == plan.SoBranchNo && r.ItemCode == itemCode)
                .OrderByDescending(r => r.No).Select(r => r.LotNo).FirstOrDefaultAsync(ct));
        var distinctLots = lots.Distinct().ToList();
        var pickedLot = lotNo ?? (distinctLots.Count == 1 ? distinctLots[0] : null);

        var row = new PickingResult
        {
            CreatedAt = DateTime.Now,
            SoNo = plan.SoNo,
            SoBranchNo = plan.SoBranchNo,
            ItemCode = itemCode,
            Qty = qty,
            LocationCode = locationCode,
            LotNo = pickedLot,
            CreatedBy = user.FindFirstValue(ClaimTypes.NameIdentifier),
        };
        db.PickingResults.Add(row);
        foreach (var allocation in allocations)
            db.PickingResultLots.Add(new Ims.Domain.Picking.PickingResultLot
            {
                PickingResult = row, CreatedAt = row.CreatedAt, ItemCode = itemCode,
                LotNo = allocation.LotNo, SerialNo = allocation.SerialNo,
                Qty = allocation.Qty, CreatedBy = worker ?? "system",
            });

        plan.PickedQty = qty;
        plan.Status = qty >= plan.Qty ? PickingStatus.Picked : PickingStatus.NotPicked;
        plan.UpdatedAt = DateTime.Now;
        plan.UpdatedBy = row.CreatedBy;

        await db.SaveChangesAsync(ct);

        var dto = await LoadResultDtoAsync(db, row.No, ct);
        return Outcome.Created($"/picking-result/{row.No}", new ApiSuccess<PickingResultDto>(dto!));
    }

    public async Task<ServiceResult> ExportPlansCsvAsync(CancellationToken ct)
    {
        var rows = await ProjectPlans(db,
            db.PickingPlans.AsNoTracking().OrderBy(p => p.ScheduledDate).ThenBy(p => p.No)).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("so_no,so_branch_no,item_code,item_name,location_code,location_name," +
                      "qty,unit,picked_qty,scheduled_date,status,created_at,created_by");
        foreach (var r in rows)
            sb.Append(Csv(r.SoNo)).Append(',').Append(Csv(r.SerialNo)).Append(',')
              .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
              .Append(Csv(r.LocationCode)).Append(',').Append(Csv(r.LocationName)).Append(',')
              .Append(Num(r.PlannedQty)).Append(',').Append(Csv(r.Unit)).Append(',')
              .Append(Num(r.PickedQty)).Append(',')
              .Append(r.ScheduledDate?.ToString("yyyy-MM-dd") ?? "").Append(',')
              .Append(Csv(PickingStatus.ToApiName(r.Status))).Append(',')
              .Append(r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(Csv(r.CreatedBy)).AppendLine();
        return CsvFile(sb, "picking-schedule.csv");
    }

    public async Task<ServiceResult> ExportResultsCsvAsync(CancellationToken ct)
    {
        var rows = await ProjectResults(db,
            db.PickingResults.AsNoTracking().OrderByDescending(r => r.CreatedAt).ThenBy(r => r.No))
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("no,created_at,item_code,item_name,qty,unit,so_no,so_branch_no," +
                      "scheduled_date,location_code,location_name,lot_no,worker");
        foreach (var r in rows)
            sb.Append(r.No).Append(',')
              .Append(r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
              .Append(Num(r.Qty)).Append(',').Append(Csv(r.Unit)).Append(',')
              .Append(Csv(r.SoNo)).Append(',').Append(Csv(r.SerialNo)).Append(',')
              .Append(r.ScheduledDate?.ToString("yyyy-MM-dd") ?? "").Append(',')
              .Append(Csv(r.LocationCode)).Append(',').Append(Csv(r.LocationName)).Append(',')
              .Append(Csv(r.LotNo)).Append(',')
              .Append(Csv(r.WorkerName ?? r.CreatedBy)).AppendLine();
        return CsvFile(sb, "picking-result.csv");
    }

    private sealed record PlanRow(
        string SoNo, string SerialNo, string ItemCode, string? ItemName,
        string? LocationCode, string? LocationName, decimal PlannedQty, string? Unit,
        decimal PickedQty, DateOnly? ScheduledDate, int Status,
        DateTime CreatedAt, string? CreatedBy);

    private sealed record ResultRow(
        long No, DateTime CreatedAt, string ItemCode, string? ItemName, decimal Qty, string? Unit,
        string SoNo, string SerialNo, DateOnly? ScheduledDate,
        string LocationCode, string? LocationName, string? LotNo,
        string? WorkerName, string? CreatedBy);

    private static IQueryable<PlanRow> ProjectPlans(ImsDbContext db, IQueryable<PickingPlan> q) =>
        q.Select(p => new PlanRow(
            p.SoNo,
            p.SoBranchNo,
            p.ItemCode,
            db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            p.LocationCode,
            db.Locations.Where(l => l.LocationCode == p.LocationCode).Select(l => l.LocationName).FirstOrDefault(),
            p.Qty,
            db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            p.PickedQty,
            p.ScheduledDate,
            p.Status,
            p.CreatedAt,
            p.CreatedBy));

    private static IQueryable<ResultRow> ProjectResults(ImsDbContext db, IQueryable<PickingResult> q) =>
        q.Select(r => new ResultRow(
            r.No,
            r.CreatedAt,
            r.ItemCode,
            db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            r.Qty,
            db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            r.SoNo,
            r.SoBranchNo,
            db.PickingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo)
                .Select(p => p.ScheduledDate).FirstOrDefault(),
            r.LocationCode,
            db.Locations.Where(l => l.LocationCode == r.LocationCode).Select(l => l.LocationName).FirstOrDefault(),
            r.LotNo,
            db.AppUsers.Where(u => u.UserCode == r.CreatedBy).Select(u => u.UserName).FirstOrDefault(),
            r.CreatedBy));

    private static PickingPlanDto MapPlan(PlanRow r) => new(
        r.SoNo, r.SerialNo, r.ItemCode, r.ItemName, r.LocationCode, r.LocationName,
        r.PlannedQty, r.Unit, r.PickedQty, r.ScheduledDate,
        PickingStatus.ToApiName(r.Status), r.CreatedAt, r.CreatedBy);

    private static PickingResultDto MapResult(ResultRow r) => new(
        r.No, r.CreatedAt, r.ItemCode, r.ItemName, r.Qty, r.Unit, r.SoNo, r.SerialNo,
        r.ScheduledDate, r.LocationCode, r.LocationName, r.LotNo,
        r.WorkerName ?? r.CreatedBy);

    private static async Task<PickingPlanDto?> LoadPlanDtoAsync(ImsDbContext db, long no, CancellationToken ct)
    {
        var row = await ProjectPlans(db, db.PickingPlans.AsNoTracking().Where(p => p.No == no))
            .FirstOrDefaultAsync(ct);
        return row is null ? null : MapPlan(row);
    }

    private static async Task<PickingResultDto?> LoadResultDtoAsync(ImsDbContext db, long no, CancellationToken ct)
    {
        var row = await ProjectResults(db, db.PickingResults.AsNoTracking().Where(r => r.No == no))
            .FirstOrDefaultAsync(ct);
        return row is null ? null : MapResult(row);
    }

    private static string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
