using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Domain.Receiving;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Receiving;

internal sealed class ReceivingService(ImsDbContext db, Ht.ILotLookup lots) : IReceivingService
{
    private const int MaxPoLength = 30;

    internal const string PrintCategory = "Receiving";

    private readonly SortMap<ReceivingPlan> PlanSort = new SortMap<ReceivingPlan>()
        .Add("po_no", p => p.PoNo)
        .Add("po_branch_no", p => p.PoBranchNo)
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
        .Add("packing_type", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.PackingTypeCode).FirstOrDefault())
        .Add("packing_qty", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => (decimal?)i.PackageQty).FirstOrDefault())
        .Add("shortage_reason", p => p.ReasonCode)
        .Add("created_by", p => p.CreatedBy)
        .Default("scheduled_date")
        .ThenAlwaysBy(p => p.No);

    private readonly SortMap<ReceivingResult> ResultSort = new SortMap<ReceivingResult>()
        .Add("created_at", r => r.CreatedAt)
        .Add("po_no", r => r.PoNo)
        .Add("item_code", r => r.ItemCode)
        .Add("qty", r => r.Qty)
        .Add("location_code", r => r.LocationCode)
        .Add("expiry_date", r => r.ExpiryDate)
        .Add("no", r => r.No)
        .Add("item_name", r => db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("unit", r => db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("serial_no", r => r.PoBranchNo)
        .Add("invoice_no", r => r.InvoiceNo)
        .Add("worker", r => db.AppUsers.Where(u => u.UserCode == r.CreatedBy).Select(u => u.UserName).FirstOrDefault() ?? r.CreatedBy)
        .Add("status", r => db.ReceivingPlans.Where(p => p.PoNo == r.PoNo && p.PoBranchNo == r.PoBranchNo).Select(p => (int?)p.Status).FirstOrDefault() ?? ReceivingStatus.Arrived)
        .Add("shortage_reason", r => db.ReceivingPlans.Where(p => p.PoNo == r.PoNo && p.PoBranchNo == r.PoBranchNo).Select(p => p.ReasonCode).FirstOrDefault())
        .Default("created_at")
        .ThenAlwaysBy(r => r.No);

    public async Task<ServiceResult> ListPlansAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? poNo = null, string? status = null)
    {
        var query = db.ReceivingPlans.AsNoTracking();

        if (dateFrom is { } from) query = query.Where(p => p.ScheduledDate >= from);
        if (dateTo is { } to) query = query.Where(p => p.ScheduledDate <= to);
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(p => EF.Functions.Like(p.ItemCode, "%" + ic + "%"));
        if (Trimmed(poNo) is { } po)
            query = query.Where(p => EF.Functions.Like(p.PoNo, "%" + po + "%"));
        if (Trimmed(itemName) is { } iname)
            query = query.Where(p => db.Items.Any(i =>
                i.ItemCode == p.ItemCode && EF.Functions.Like(i.ItemName, "%" + iname + "%")));
        if (Trimmed(q) is { } free)
            query = query.Where(p => EF.Functions.Like(p.PoNo, "%" + free + "%")
                                  || EF.Functions.Like(p.ItemCode, "%" + free + "%"));
        if (ReceivingStatus.FromApiName(Trimmed(status)) is { } st)
            query = query.Where(p => p.Status == st);

        query = PlanSort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await ProjectPlans(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ReceivingPlanDto>(
            rows.Select(MapPlan).ToList(), new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> CreatePlanAsync(ReceivingPlanWriteDto body, CancellationToken ct)
    {
        if (ValidateCode(body.PoNo, MaxPoLength) is { } e1) return e1;
        if (ValidateCode(body.ItemCode) is { } e2) return e2;
        if (body.PlannedQty < 1) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var itemCode = body.ItemCode.Trim();
        var packageQty = await db.Items
            .Where(i => i.ItemCode == itemCode && !i.IsDeleted)
            .Select(i => (decimal?)i.PackageQty)
            .FirstOrDefaultAsync(ct);
        if (packageQty is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));

        if (packageQty > 0 && body.PlannedQty % packageQty.Value != 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var supplierCode = NullIfBlank(body.SupplierCode)?.Trim();
        if (supplierCode is not null
            && !await db.Partners.AnyAsync(p => p.PartnerCode == supplierCode && !p.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Partner")));

        var factoryCode = NullIfBlank(body.FactoryCode)?.Trim();
        if (factoryCode is not null
            && !await db.Factories.AnyAsync(f => f.FactoryCode == factoryCode && !f.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")));

        var plan = new ReceivingPlan
        {
            PoNo = body.PoNo.Trim(),
            PoBranchNo = NullIfBlank(body.SerialNo)?.Trim() ?? "01",
            PartnerCode = supplierCode,
            ItemCode = itemCode,
            Qty = body.PlannedQty,
            ActualQty = 0,
            FactoryCode = factoryCode,
            ScheduledDate = body.ScheduledDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Status = ReceivingStatus.NotArrived,
            InvoiceNo = NullIfBlank(body.InvoiceNo)?.Trim(),
            LotNo = NullIfBlank(body.LotNo)?.Trim(),
            LocationCode = NullIfBlank(body.LocationCode)?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        db.ReceivingPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        var dto = await LoadPlanDtoAsync(db, plan.No, ct);
        return Outcome.Created($"/receiving-schedule?poNo={plan.PoNo}", new ApiSuccess<ReceivingPlanDto>(dto!));
    }

    public async Task<ServiceResult> ChangeStatusAsync(
        ReceivingStatusChangeDto body, CancellationToken ct, ClaimsPrincipal? user = null)
    {
        var lines = (body.Lines ?? Array.Empty<ReceivingPlanKeyDto>())
            .Select(l => (Po: Trimmed(l.PoNo), Serial: Trimmed(l.SerialNo) ?? "01"))
            .Where(l => l.Po is not null).Distinct().ToList();
        var poNos = lines.Count > 0
            ? lines.Select(l => l.Po).Distinct().ToList()
            : body.PoNos?.Select(x => x?.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList()
              ?? new List<string?>();
        if (poNos.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));

        var target = ActionToStatus(Trimmed(body.Status));
        if (target is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel006()));

        var worker = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var rows = await db.ReceivingPlans.Where(p => poNos.Contains(p.PoNo)).ToListAsync(ct);
        if (lines.Count > 0)
        {
            rows = rows.Where(r => lines.Contains((r.PoNo, r.PoBranchNo))).ToList();
            return await ApplyStatusAsync(rows, target.Value,
                lines.Where(l => !rows.Any(r => r.PoNo == l.Po && r.PoBranchNo == l.Serial))
                     .Select(l => $"{l.Po}/{l.Serial}"),
                r => $"{r.PoNo}/{r.PoBranchNo}", worker, ct);
        }
        return await ApplyStatusAsync(rows, target.Value,
            poNos.Where(p => rows.All(r => r.PoNo != p)).Select(p => p!), r => r.PoNo, worker, ct);
    }

    private async Task<ServiceResult> ApplyStatusAsync(
        List<ReceivingPlan> rows, int target, IEnumerable<string> missing,
        Func<ReceivingPlan, string> label, string? worker, CancellationToken ct)
    {
        var skipped = new SortedSet<string>(StringComparer.Ordinal);
        var updated = 0;
        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var row in rows)
        {
            var allowed = target switch
            {
                ReceivingStatus.Arrived => row.Status is not (ReceivingStatus.Arrived or ReceivingStatus.Cancelled),
                ReceivingStatus.NotArrived => true,
                _ => row.Status is not (ReceivingStatus.Arrived or ReceivingStatus.Cancelled),
            };
            if (!allowed
                || (target == ReceivingStatus.Arrived && !await ReceiveRemainderAsync(row, worker, now, ct))
                || (target == ReceivingStatus.NotArrived && !await ReverseReceiptsAsync(row, worker, now, ct)))
            {
                skipped.Add(label(row));
                continue;
            }
            if (target == ReceivingStatus.NotArrived)
            {
                row.ActualQty = 0;
                row.ReasonCode = null;
            }
            row.Status = target;
            row.UpdatedAt = now;
            row.UpdatedBy = worker ?? row.UpdatedBy;
            updated++;
        }
        foreach (var m in missing) skipped.Add(m);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome.Ok(new ApiSuccess<ReceivingStatusChangeResult>(
            new ReceivingStatusChangeResult(updated, skipped.ToList())));
    }

    private async Task<bool> ReverseReceiptsAsync(ReceivingPlan plan, string? worker, DateTime now, CancellationToken ct)
    {
        var receipts = await db.ReceivingResults
            .Where(r => r.PoNo == plan.PoNo && r.PoBranchNo == plan.PoBranchNo)
            .ToListAsync(ct);
        var serials = receipts.Where(r => r.Qty > 0).Select(r => $"RCV-{r.No:D10}").ToList();
        var stocks = await db.Stocks
            .Where(s => s.ItemCode == plan.ItemCode && serials.Contains(s.SerialNo))
            .ToDictionaryAsync(s => s.SerialNo, ct);

        foreach (var receipt in receipts.Where(r => r.Qty > 0))
        {
            if (!stocks.TryGetValue($"RCV-{receipt.No:D10}", out var stock)
                || stock.StockQty - receipt.Qty < stock.ReservedQty)
                return false;
        }

        foreach (var receipt in receipts.Where(r => r.Qty > 0))
        {
            var stock = stocks[$"RCV-{receipt.No:D10}"];
            stock.StockQty -= receipt.Qty;
            stock.Recalculate();
            stock.UpdatedAt = now;
            stock.UpdatedBy = worker ?? stock.UpdatedBy;
            db.InventoryHistories.Add(new Domain.Inventory.InventoryHistory
            {
                CreatedAt = DateTime.Now,
                ItemCode = stock.ItemCode,
                FactoryCode = stock.FactoryCode,
                LocationCode = stock.LocationCode,
                LotNo = stock.LotNo,
                ActionType = Domain.Inventory.InventoryAction.Adjust,
                Qty = -receipt.Qty,
                CreatedBy = worker,
            });
        }

        db.ReceivingResults.RemoveRange(receipts);
        return true;
    }

    private async Task<bool> ReceiveRemainderAsync(ReceivingPlan plan, string? worker, DateTime now, CancellationToken ct)
    {
        var remainder = plan.Qty - plan.ActualQty;
        if (remainder <= 0) return true;

        var item = await db.Items.AsNoTracking()
            .Where(i => i.ItemCode == plan.ItemCode)
            .Select(i => new { i.DefaultLocationCode, i.ExpirationPeriod })
            .FirstOrDefaultAsync(ct);
        if (item is null) return false;
        var preferred = item.DefaultLocationCode;
        var locationCode = await db.Locations
            .Where(l => !l.IsDeleted && (l.LocationCode == preferred || l.LocationCode == plan.LocationCode))
            .OrderBy(l => l.LocationCode == preferred ? 0 : 1)
            .Select(l => l.LocationCode)
            .FirstOrDefaultAsync(ct);
        if (locationCode is null) return false;

        var result = new ReceivingResult
        {
            PoNo = plan.PoNo,
            PoBranchNo = plan.PoBranchNo,
            ItemCode = plan.ItemCode,
            Qty = remainder,
            LocationCode = locationCode,
            LotNo = plan.LotNo,
            ReasonCode = plan.ReasonCode,
            InvoiceNo = plan.InvoiceNo,
            CreatedAt = now,
            CreatedBy = worker,
        };
        db.ReceivingResults.Add(result);
        await db.SaveChangesAsync(ct);

        var factoryCode = plan.FactoryCode
            ?? await db.Factories.Where(f => !f.IsDeleted).OrderBy(f => f.FactoryCode)
                .Select(f => f.FactoryCode).FirstAsync(ct);
        var stock = new Domain.Inventory.Stock
        {
            ItemCode = plan.ItemCode,
            SerialNo = $"RCV-{result.No:D10}",
            FactoryCode = factoryCode,
            LocationCode = locationCode,
            LotNo = plan.LotNo,
            StockQty = remainder,
            ExpiryDate = item.ExpirationPeriod is > 0
                ? DateOnly.FromDateTime(DateTime.Now).AddDays(item.ExpirationPeriod.Value)
                : null,
            UpdatedAt = now,
            UpdatedBy = worker,
        };
        stock.Recalculate();
        db.Stocks.Add(stock);
        db.InventoryHistories.Add(new Domain.Inventory.InventoryHistory
        {
            CreatedAt = DateTime.Now,
            ItemCode = plan.ItemCode,
            FactoryCode = factoryCode,
            LocationCode = locationCode,
            LotNo = plan.LotNo,
            ActionType = Domain.Inventory.InventoryAction.Receiving,
            Qty = remainder,
            ReasonCode = plan.ReasonCode,
            CreatedBy = worker,
        });
        plan.ActualQty = plan.Qty;
        return true;
    }

    private static int? ActionToStatus(string? action) => action switch
    {
        "done" => ReceivingStatus.Arrived,
        "notDone" => ReceivingStatus.NotArrived,
        "cancelled" => ReceivingStatus.Cancelled,
        _ => null,
    };

    public async Task<ServiceResult> RegisterShortageAsync(string poNo, ReceivingShortageDto body, CancellationToken ct)
    {
        if (body.ShortageQty is not { } shortageQty || shortageQty < 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var reasonCode = NullIfBlank(body.ReasonCode)?.Trim();
        if (reasonCode is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));
        if (!await db.Reasons.AnyAsync(r => r.ReasonCode == reasonCode && !r.IsDeleted
                                         && r.ReasonCategoryValue == ReasonCategory.Shortage, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")));

        var code = poNo.Trim();
        var serialNo = NullIfBlank(body.SerialNo)?.Trim();
        var candidates = db.ReceivingPlans.Where(p =>
            p.PoNo == code
            && p.Status != ReceivingStatus.Arrived
            && p.Status != ReceivingStatus.Cancelled);
        if (serialNo is not null) candidates = candidates.Where(p => p.PoBranchNo == serialNo);

        var open = await candidates.ToListAsync(ct);
        if (open.Count != 1) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Receiving plan")));

        var plan = open[0];
        if (shortageQty > plan.Qty) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        plan.ActualQty = plan.Qty - shortageQty;
        plan.Status = ReceivingStatus.Partial;
        plan.ReasonCode = reasonCode;
        plan.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var dto = await LoadPlanDtoAsync(db, plan.No, ct);
        return Outcome.Ok(new ApiSuccess<ReceivingPlanDto>(dto!));
    }

    public async Task<ServiceResult> ListResultsAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, DateOnly? dateFrom = null,
        DateOnly? dateTo = null, string? itemCode = null, string? itemName = null, string? poNo = null,
        string? poNoPresence = null)
    {
        var query = db.ReceivingResults.AsNoTracking();

        if (dateFrom is { } from)
            query = query.Where(r => r.CreatedAt >= from.ToDateTime(TimeOnly.MinValue));
        if (dateTo is { } to)
            query = query.Where(r => r.CreatedAt < to.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(r => EF.Functions.Like(r.ItemCode, "%" + ic + "%"));
        if (Trimmed(poNo) is { } po)
            query = query.Where(r => r.PoNo != null && EF.Functions.Like(r.PoNo, "%" + po + "%"));
        if (Trimmed(itemName) is { } iname)
            query = query.Where(r => db.Items.Any(i =>
                i.ItemCode == r.ItemCode && EF.Functions.Like(i.ItemName, "%" + iname + "%")));

        query = Trimmed(poNoPresence) switch
        {
            "with" => query.Where(r => r.PoNo != null),
            "without" => query.Where(r => r.PoNo == null),
            _ => query,
        };

        query = ResultSort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await ProjectResults(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ReceivingResultDto>(
            rows.Select(MapResult).ToList(), new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> CreateResultAsync(
        ReceivingResultWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (ValidateCode(body.ItemCode) is { } e1) return e1;
        if (body.Qty is not { } qty || qty < 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var itemCode = body.ItemCode.Trim();
        var packageQty = await db.Items
            .Where(i => i.ItemCode == itemCode && !i.IsDeleted)
            .Select(i => (decimal?)i.PackageQty)
            .FirstOrDefaultAsync(ct);
        if (packageQty is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));

        var locationCode = NullIfBlank(body.LocationCode)?.Trim();
        if (locationCode is null && qty > 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Loc001()));
        if (locationCode is not null
            && !await db.Locations.AnyAsync(l => l.LocationCode == locationCode && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));

        var reasonCode = NullIfBlank(body.ReasonCode)?.Trim();
        if (reasonCode is not null
            && !await db.Reasons.AnyAsync(r => r.ReasonCode == reasonCode && !r.IsDeleted
                                            && r.ReasonCategoryValue == ReasonCategory.Shortage, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")));

        var lotNo = NullIfBlank(body.LotNo)?.Trim();
        if (lotNo is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Lot001()));
        if (!await lots.IsKnownLotAsync(lotNo, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.Lot002()));

        var poNo = NullIfBlank(body.PoNo)?.Trim();
        ReceivingPlan? plan = null;

        if (poNo is not null)
        {
            var candidates = db.ReceivingPlans.Where(p =>
                p.PoNo == poNo
                && p.ItemCode == itemCode
                && p.Status != ReceivingStatus.Arrived
                && p.Status != ReceivingStatus.Cancelled);
            var serialNo = NullIfBlank(body.SerialNo)?.Trim();
            if (serialNo is not null) candidates = candidates.Where(p => p.PoBranchNo == serialNo);

            var open = await candidates.ToListAsync(ct);
            if (open.Count != 1)
                return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Receiving plan")));

            plan = open[0];
            if (!string.IsNullOrWhiteSpace(plan.LotNo)
                && !string.Equals(plan.LotNo, lotNo, StringComparison.Ordinal))
                return Outcome.Conflict(new ApiError(ErrorCatalog.Scn006()));
            var expected = packageQty is > 0 ? plan.Qty / packageQty.Value : plan.Qty;

            if (plan.ActualQty + qty > plan.Qty)
                return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty002()));
            if (qty < expected && reasonCode is null)
                return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));
        }

        var now = DateTime.UtcNow;
        var worker = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var row = new ReceivingResult
        {
            PoNo = plan?.PoNo,
            PoBranchNo = plan?.PoBranchNo,
            ItemCode = itemCode,
            Qty = qty,
            LocationCode = locationCode,
            LotNo = NullIfBlank(body.LotNo)?.Trim(),
            IsOnHold = body.IsOnHold,
            ReasonCode = reasonCode,
            ExpiryDate = body.ExpiryDate,
            InvoiceNo = NullIfBlank(body.InvoiceNo)?.Trim() ?? plan?.InvoiceNo,
            CreatedAt = now,
            CreatedBy = worker,
        };
        db.ReceivingResults.Add(row);

        if (plan is not null)
        {
            plan.ActualQty += qty;
            plan.Status = plan.ActualQty >= plan.Qty ? ReceivingStatus.Arrived : ReceivingStatus.Partial;
            if (reasonCode is not null) plan.ReasonCode = reasonCode;
            plan.UpdatedAt = now;
            plan.UpdatedBy = worker;
        }

        await db.SaveChangesAsync(ct);

        if (qty > 0)
        {
            var factoryCode = plan?.FactoryCode
                ?? (locationCode is null ? null : await db.Locations
                    .Where(l => l.LocationCode == locationCode && l.ZoneCode != null)
                    .Join(db.Zones, l => l.ZoneCode, z => z.ZoneCode, (l, z) => z.FactoryCode)
                    .FirstOrDefaultAsync(ct))
                ?? await db.Factories.Where(f => !f.IsDeleted).OrderBy(f => f.FactoryCode)
                    .Select(f => f.FactoryCode).FirstAsync(ct);
            var period = await db.Items.Where(i => i.ItemCode == itemCode)
                .Select(i => i.ExpirationPeriod).FirstAsync(ct);
            var expiry = body.ExpiryDate
                ?? (period is > 0 ? DateOnly.FromDateTime(DateTime.Now).AddDays(period.Value) : null);

            var stock = new Domain.Inventory.Stock
            {
                ItemCode = itemCode,
                SerialNo = $"RCV-{row.No:D10}",
                FactoryCode = factoryCode,
                LocationCode = locationCode,
                LotNo = lotNo,
                StockQty = qty,
                IsOnHold = body.IsOnHold,
                ReasonCode = body.IsOnHold ? reasonCode : null,
                ExpiryDate = expiry,
                UpdatedAt = now,
                UpdatedBy = worker,
            };
            stock.Recalculate();
            db.Stocks.Add(stock);
            db.InventoryHistories.Add(new Domain.Inventory.InventoryHistory
            {
                CreatedAt = DateTime.Now,
                ItemCode = itemCode,
                FactoryCode = factoryCode,
                LocationCode = locationCode,
                LotNo = lotNo,
                ActionType = Domain.Inventory.InventoryAction.Receiving,
                Qty = qty,
                ReasonCode = reasonCode,
                CreatedBy = worker,
            });
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);

        var dto = await LoadResultDtoAsync(db, row.No, ct);
        return Outcome.Created($"/receiving-result?poNo={row.PoNo}", new ApiSuccess<ReceivingResultDto>(dto!));
    }

    public async Task<ServiceResult> ExportPlansCsvAsync(CancellationToken ct)
    {
        var rows = await ProjectPlans(db,
            db.ReceivingPlans.AsNoTracking().OrderBy(p => p.ScheduledDate).ThenBy(p => p.No)).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("po_no,po_branch_no,partner_code,partner_name,item_code,item_name," +
                      "qty,unit,actual_qty,packing_type_code,packing_qty,scheduled_date," +
                      "invoice_no,status,reason_code,lot_no,location_code,created_at,created_by");
        foreach (var r in rows)
            sb.Append(Csv(r.PoNo)).Append(',').Append(Csv(r.SerialNo)).Append(',')
              .Append(Csv(r.SupplierCode)).Append(',').Append(Csv(r.SupplierName)).Append(',')
              .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
              .Append(Num(r.PlannedQty)).Append(',').Append(Csv(r.Unit)).Append(',')
              .Append(Num(r.ActualQty)).Append(',').Append(Csv(r.PackingTypeCode)).Append(',')
              .Append(Num(r.PackingQty)).Append(',')
              .Append(r.ScheduledDate?.ToString("yyyy-MM-dd") ?? "").Append(',')
              .Append(Csv(r.InvoiceNo)).Append(',')
              .Append(Csv(ReceivingStatus.ToApiName(r.Status))).Append(',')
              .Append(Csv(r.ShortageReason)).Append(',')
              .Append(Csv(r.LotNo)).Append(',').Append(Csv(r.LocationCode)).Append(',')
              .Append(r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(Csv(r.CreatedBy)).AppendLine();
        return CsvFile(sb, "receiving-schedule.csv");
    }

    public async Task<ServiceResult> ExportResultsCsvAsync(CancellationToken ct)
    {
        var rows = await ProjectResults(db,
            db.ReceivingResults.AsNoTracking().OrderBy(r => r.CreatedAt).ThenBy(r => r.No)).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("no,created_at,item_code,item_name,qty,unit,po_no,po_branch_no," +
                      "invoice_no,location_code,worker,status,reason_code,expiry_date");
        foreach (var r in rows)
        {
            var dto = MapResult(r);
            sb.Append(dto.No).Append(',')
              .Append(dto.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(Csv(dto.ItemCode)).Append(',').Append(Csv(dto.ItemName)).Append(',')
              .Append(Num(dto.Qty)).Append(',').Append(Csv(dto.Unit)).Append(',')
              .Append(Csv(dto.PoNo)).Append(',').Append(Csv(dto.SerialNo)).Append(',')
              .Append(Csv(dto.InvoiceNo)).Append(',').Append(Csv(dto.LocationCode)).Append(',')
              .Append(Csv(dto.Worker)).Append(',').Append(Csv(dto.Status)).Append(',')
              .Append(Csv(dto.ShortageReason)).Append(',')
              .Append(dto.ExpiryDate?.ToString("yyyy-MM-dd") ?? "").AppendLine();
        }
        return CsvFile(sb, "receiving-result.csv");
    }

    private sealed record PlanRow(
        string PoNo, string SerialNo, string? SupplierCode, string? SupplierName,
        string ItemCode, string? ItemName, decimal PlannedQty, string? Unit, decimal ActualQty,
        string? PackingTypeCode, decimal? PackingQty, DateOnly? ScheduledDate, string? InvoiceNo,
        int Status, string? ShortageReason, string? LotNo, string? LocationCode, bool LabelIssued,
        DateTime CreatedAt, string? CreatedBy);

    private sealed record ResultRow(
        long No, DateTime CreatedAt, string ItemCode, string? ItemName, decimal Qty, string? Unit,
        string? PoNo, string? SerialNo, string? InvoiceNo, string? LocationCode,
        string? WorkerName, string? CreatedBy, int? PlanStatus, string? ShortageReason,
        DateOnly? ExpiryDate, string? LotNo, string? SupplierCode, string? SupplierName, DateOnly? ScheduledDate);

    private static IQueryable<PlanRow> ProjectPlans(ImsDbContext db, IQueryable<ReceivingPlan> q) =>
        q.Select(p => new PlanRow(
            p.PoNo,
            p.PoBranchNo,
            p.PartnerCode,
            db.Partners.Where(x => x.PartnerCode == p.PartnerCode).Select(x => x.PartnerName).FirstOrDefault(),
            p.ItemCode,
            db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            p.Qty,
            db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            p.ActualQty,
            db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.PackingTypeCode).FirstOrDefault(),
            db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => (decimal?)i.PackageQty).FirstOrDefault(),
            p.ScheduledDate,
            p.InvoiceNo,
            p.Status,
            p.ReasonCode,
            p.LotNo,
            p.LocationCode,
            db.PrintHistories.Any(h => h.Category == PrintCategory
                                    && h.DocNo == p.PoNo && h.ItemCode == p.ItemCode),
            p.CreatedAt,
            p.CreatedBy));

    private static IQueryable<ResultRow> ProjectResults(ImsDbContext db, IQueryable<ReceivingResult> q) =>
        q.Select(r => new ResultRow(
            r.No,
            r.CreatedAt,
            r.ItemCode,
            db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            r.Qty,
            db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            r.PoNo,
            r.PoBranchNo,
            r.InvoiceNo,
            r.LocationCode,
            db.AppUsers.Where(u => u.UserCode == r.CreatedBy).Select(u => u.UserName).FirstOrDefault(),
            r.CreatedBy,
            db.ReceivingPlans
                .Where(p => p.PoNo == r.PoNo && p.PoBranchNo == r.PoBranchNo)
                .Select(p => (int?)p.Status).FirstOrDefault(),
            db.ReceivingPlans
                .Where(p => p.PoNo == r.PoNo && p.PoBranchNo == r.PoBranchNo)
                .Select(p => p.ReasonCode).FirstOrDefault(),
            r.ExpiryDate,
            r.LotNo,
            db.ReceivingPlans
                .Where(p => p.PoNo == r.PoNo && p.PoBranchNo == r.PoBranchNo)
                .Select(p => p.PartnerCode).FirstOrDefault(),
            db.ReceivingPlans
                .Where(p => p.PoNo == r.PoNo && p.PoBranchNo == r.PoBranchNo)
                .Join(db.Partners, p => p.PartnerCode, x => x.PartnerCode, (p, x) => x.PartnerName)
                .FirstOrDefault(),
            db.ReceivingPlans
                .Where(p => p.PoNo == r.PoNo && p.PoBranchNo == r.PoBranchNo)
                .Select(p => p.ScheduledDate).FirstOrDefault()));

    private static ReceivingPlanDto MapPlan(PlanRow r) => new(
        r.PoNo, r.SerialNo, r.SupplierCode, r.SupplierName, r.ItemCode, r.ItemName,
        r.PlannedQty, r.Unit, r.ActualQty, r.PackingTypeCode, r.PackingQty,
        r.ScheduledDate, r.InvoiceNo, ReceivingStatus.ToApiName(r.Status),
        r.ShortageReason, r.LotNo, r.LocationCode, r.LabelIssued, r.CreatedAt, r.CreatedBy);

    private static ReceivingResultDto MapResult(ResultRow r) => new(
        r.No, r.CreatedAt, r.ItemCode, r.ItemName, r.Qty, r.Unit, r.PoNo, r.SerialNo,
        r.InvoiceNo, r.LocationCode,
        r.WorkerName ?? r.CreatedBy,
        ReceivingStatus.ToApiName(r.PlanStatus ?? ReceivingStatus.Arrived),
        r.ShortageReason, r.ExpiryDate, r.LotNo, r.SupplierCode, r.SupplierName, r.ScheduledDate);

    private static async Task<ReceivingPlanDto?> LoadPlanDtoAsync(ImsDbContext db, long no, CancellationToken ct)
    {
        var row = await ProjectPlans(db, db.ReceivingPlans.AsNoTracking().Where(p => p.No == no))
            .FirstOrDefaultAsync(ct);
        return row is null ? null : MapPlan(row);
    }

    private static async Task<ReceivingResultDto?> LoadResultDtoAsync(ImsDbContext db, long no, CancellationToken ct)
    {
        var row = await ProjectResults(db, db.ReceivingResults.AsNoTracking().Where(r => r.No == no))
            .FirstOrDefaultAsync(ct);
        return row is null ? null : MapResult(row);
    }

    private static string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
