using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Domain.Shipping;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Shipping;

internal sealed class ShippingService(ImsDbContext db) : IShippingService
{
    private const int MaxSlipLength = 30;

    internal const string PrintCategory = "Shipping";

    private readonly SortMap<ShippingPlan> PlanSort = new SortMap<ShippingPlan>()
        .Add("so_no", p => p.SoNo)
        .Add("so_branch_no", p => p.SoBranchNo)
        .Add("partner_code", p => p.PartnerCode)
        .Add("item_code", p => p.ItemCode)
        .Add("qty", p => p.Qty)
        .Add("actual_qty", p => p.ActualQty)
        .Add("scheduled_date", p => p.ScheduledDate)
        .Add("gate_code", p => p.GateCode)
        .Add("status", p => p.Status)
        .Add("created_at", p => p.CreatedAt)
        .Add("no", p => p.No)
        .Add("item_name", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("unit", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("packing_type", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => i.PackingTypeCode).FirstOrDefault())
        .Add("packing_qty", p => db.Items.Where(i => i.ItemCode == p.ItemCode).Select(i => (decimal?)i.PackageQty).FirstOrDefault())
        .Add("invoice_no", p => p.InvoiceNo)
        .Add("shortage_reason", p => p.ReasonCode)
        .Add("created_by", p => p.CreatedBy)
        .Default("scheduled_date")
        .ThenAlwaysBy(p => p.No);

    private readonly SortMap<ShippingResult> ResultSort = new SortMap<ShippingResult>()
        .Add("created_at", r => r.CreatedAt)
        .Add("so_no", r => r.SoNo)
        .Add("item_code", r => r.ItemCode)
        .Add("qty", r => r.Qty)
        .Add("location_code", r => r.LocationCode)
        .Add("no", r => r.No)
        .Add("item_name", r => db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("unit", r => db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("serial_no", r => r.SoBranchNo)
        .Add("scheduled_date", r => db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo).Select(p => p.ScheduledDate).FirstOrDefault())
        .Add("invoice_no", r => db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo).Select(p => p.InvoiceNo).FirstOrDefault())
        .Add("worker", r => db.AppUsers.Where(u => u.UserCode == r.CreatedBy).Select(u => u.UserName).FirstOrDefault() ?? r.CreatedBy)
        .Add("status", r => db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo).Select(p => (int?)p.Status).FirstOrDefault() ?? ShippingStatus.Shipped)
        .Add("shortage_reason", r => db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo).Select(p => p.ReasonCode).FirstOrDefault())
        .Add("expiry_date", r => r.ExpiryDate)
        .Default("created_at")
        .ThenAlwaysBy(r => r.No);

    public async Task<ServiceResult> ListPlansAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? slipNo = null, string? status = null)
    {
        var query = db.ShippingPlans.AsNoTracking();

        if (dateFrom is { } from) query = query.Where(p => p.ScheduledDate >= from);
        if (dateTo is { } to) query = query.Where(p => p.ScheduledDate <= to);
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(p => EF.Functions.Like(p.ItemCode, LikeContains(ic), LikeEscape));
        if (Trimmed(slipNo) is { } sn)
            query = query.Where(p => EF.Functions.Like(p.SoNo, LikeContains(sn), LikeEscape));
        if (Trimmed(itemName) is { } iname)
            query = query.Where(p => db.Items.Any(i =>
                i.ItemCode == p.ItemCode && EF.Functions.Like(i.ItemName, LikeContains(iname), LikeEscape)));
        if (Trimmed(q) is { } free)
            query = query.Where(p => EF.Functions.Like(p.SoNo, LikeContains(free), LikeEscape)
                                  || EF.Functions.Like(p.ItemCode, LikeContains(free), LikeEscape));
        if (ShippingStatus.FromApiName(Trimmed(status)) is { } st)
            query = query.Where(p => p.Status == st);

        query = PlanSort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await ProjectPlans(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ShippingPlanDto>(
            rows.Select(MapPlan).ToList(), new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> CreatePlanAsync(
        ShippingPlanWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (ValidateCode(body.SlipNo, MaxSlipLength) is { } e1) return e1;
        if (ValidateCode(body.ItemCode) is { } e2) return e2;
        if (body.PlannedQty < 1) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var itemCode = body.ItemCode.Trim();
        if (!await db.Items.AnyAsync(i => i.ItemCode == itemCode && !i.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));

        var partnerCode = NullIfBlank(body.CustomerCode)?.Trim();
        if (partnerCode is not null
            && !await db.Partners.AnyAsync(p => p.PartnerCode == partnerCode && !p.IsDeleted && p.IsCustomer, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Partner")));

        var factoryCode = NullIfBlank(body.FactoryCode)?.Trim();
        if (factoryCode is not null
            && !await db.Factories.AnyAsync(f => f.FactoryCode == factoryCode && !f.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")));

        var plan = new ShippingPlan
        {
            SoNo = body.SlipNo.Trim(),
            SoBranchNo = NullIfBlank(body.SerialNo)?.Trim() ?? "01",
            PartnerCode = partnerCode,
            ItemCode = itemCode,
            Qty = body.PlannedQty,
            ActualQty = 0,
            FactoryCode = factoryCode,
            GateCode = NullIfBlank(body.GateCode)?.Trim(),
            InvoiceNo = NullIfBlank(body.InvoiceNo)?.Trim(),
            ScheduledDate = body.ScheduledDate ?? DateOnly.FromDateTime(DateTime.Now),
            Status = ShippingStatus.Pending,
            CreatedAt = DateTime.Now,
            CreatedBy = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        };
        if (await db.ShippingPlans.AnyAsync(x => x.SoNo == plan.SoNo && x.SoBranchNo == plan.SoBranchNo, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        db.ShippingPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        var dto = await LoadPlanDtoAsync(db, plan.No, ct);
        return Outcome.Created($"/shipping-schedule?slipNo={plan.SoNo}",
            new ApiSuccess<ShippingPlanDto>(dto!));
    }

    public async Task<ServiceResult> ChangeStatusAsync(ShippingStatusChangeDto body, CancellationToken ct)
    {
        var lines = (body.Lines ?? Array.Empty<ShippingPlanKeyDto>())
            .Select(l => (No: Trimmed(l.SlipNo), Serial: Trimmed(l.SerialNo) ?? "01"))
            .Where(l => l.No is not null).Distinct().ToList();
        var slipNos = lines.Count > 0
            ? lines.Select(l => l.No).Distinct().ToList()
            : body.SlipNos?.Select(x => x?.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList()
              ?? new List<string?>();
        if (slipNos.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));

        var target = ActionToStatus(Trimmed(body.Status));
        if (target is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel006()));

        var rows = await db.ShippingPlans.Where(p => slipNos.Contains(p.SoNo)).ToListAsync(ct);
        IEnumerable<string> missing;
        Func<ShippingPlan, string> label;
        if (lines.Count > 0)
        {
            rows = rows.Where(r => lines.Contains((r.SoNo, r.SoBranchNo))).ToList();
            missing = lines.Where(l => !rows.Any(r => r.SoNo == l.No && r.SoBranchNo == l.Serial))
                .Select(l => $"{l.No}/{l.Serial}");
            label = r => $"{r.SoNo}/{r.SoBranchNo}";
        }
        else
        {
            missing = slipNos.Where(s => rows.All(x => x.SoNo != s)).Select(s => s!);
            label = r => r.SoNo;
        }

        var skipped = new SortedSet<string>(StringComparer.Ordinal);
        var updated = 0;
        var now = DateTime.Now;
        foreach (var row in rows)
        {
            if (row.Status is ShippingStatus.Shipped or ShippingStatus.Cancelled)
            {
                skipped.Add(label(row));
                continue;
            }
            row.Status = target.Value;
            row.UpdatedAt = now;
            updated++;
        }
        foreach (var m in missing) skipped.Add(m);

        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<ShippingStatusChangeResult>(
            new ShippingStatusChangeResult(updated, skipped.ToList())));
    }

    private static int? ActionToStatus(string? action) => action switch
    {
        "done" => ShippingStatus.Shipped,
        "notDone" => ShippingStatus.Pending,
        "cancelled" => ShippingStatus.Cancelled,
        _ => null,
    };

    public async Task<ServiceResult> RegisterShortageAsync(string soNo, ShippingShortageDto body, CancellationToken ct, ClaimsPrincipal? user = null)
    {
        if (body.ShortageQty is not { } shortageQty || shortageQty < 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var reasonCode = NullIfBlank(body.ReasonCode)?.Trim();
        if (reasonCode is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));
        if (!await db.Reasons.AnyAsync(r => r.ReasonCode == reasonCode && !r.IsDeleted
                                         && r.ReasonCategoryValue == ReasonCategory.Shortage, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")));

        var code = soNo.Trim();
        var serialNo = NullIfBlank(body.SerialNo)?.Trim();
        var candidates = db.ShippingPlans.Where(p =>
            p.SoNo == code
            && p.Status != ShippingStatus.Shipped
            && p.Status != ShippingStatus.Cancelled);
        if (serialNo is not null) candidates = candidates.Where(p => p.SoBranchNo == serialNo);

        var open = await candidates.ToListAsync(ct);
        if (open.Count != 1) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Shipping plan")));

        var plan = open[0];
        if (shortageQty > plan.Qty) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));
        var shipped = plan.Qty - shortageQty;
        var delta = shipped - plan.ActualQty;
        if (delta < 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var now = DateTime.Now;
        var worker = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var picks = await db.PickingPlans
            .Where(p => p.SoNo == plan.SoNo && p.SoBranchNo == plan.SoBranchNo && p.ItemCode == plan.ItemCode
                     && p.Status != Domain.Picking.PickingStatus.Cancelled)
            .OrderByDescending(p => p.No)
            .ToListAsync(ct);
        if (picks.Count > 0 && shipped > picks.Sum(p => p.PickedQty))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty002()));

        if (delta > 0)
        {
            var taken = new List<Domain.Inventory.Stock>();
            var allocations = new List<Inventory.StockAllocation>();
            if (picks.Count == 0
                && await Inventory.StockMovement.TakeAsync(db, plan.ItemCode, null, null, delta,
                       Domain.Inventory.InventoryAction.Shipping, reasonCode, worker, ErrorCatalog.Lot002(), ct, taken, allocations) is { } refused)
                return refused;

            var shelves = taken.Select(x => x.LocationCode).Distinct().ToList();
            var lots = taken.Select(x => x.LotNo).Distinct().ToList();
            var result = new ShippingResult
            {
                SoNo = plan.SoNo,
                SoBranchNo = plan.SoBranchNo,
                ItemCode = plan.ItemCode,
                Qty = shipped,
                LocationCode = shelves.Count == 1 ? shelves[0] : null,
                LotNo = lots.Count == 1 ? lots[0] : null,
                CreatedAt = now,
                CreatedBy = worker,
            };
            db.ShippingResults.Add(result);
            foreach (var allocation in allocations)
                db.ShippingResultLots.Add(new ShippingResultLot
                {
                    ShippingResult = result, CreatedAt = now, ItemCode = plan.ItemCode,
                    LotNo = allocation.LotNo, SerialNo = allocation.SerialNo,
                    Qty = allocation.Qty, CreatedBy = worker ?? "system",
                });
            if (picks.Count > 0)
                await AddPickedShipmentLotsAsync(result, plan.SoNo, plan.SoBranchNo, plan.ItemCode,
                    plan.ActualQty, delta, worker, ct);
        }

        var unshipped = picks.Sum(p => p.PickedQty) - shipped;
        foreach (var pick in picks)
        {
            if (unshipped <= 0) break;
            var back = Math.Min(pick.PickedQty, unshipped);
            if (back <= 0) continue;
            unshipped -= back;
            await ReturnPickedAsync(pick, back, plan.FactoryCode, reasonCode, worker, now, ct);
            pick.PickedQty -= back;
            pick.Status = pick.PickedQty >= pick.Qty
                ? Domain.Picking.PickingStatus.Picked
                : Domain.Picking.PickingStatus.NotPicked;
            pick.UpdatedAt = now;
            if (worker is not null) pick.UpdatedBy = worker;
        }

        plan.ActualQty = shipped;
        plan.Status = ShippingStatus.Partial;
        plan.ReasonCode = reasonCode;
        plan.UpdatedAt = now;
        if (worker is not null) plan.UpdatedBy = worker;
        await db.SaveChangesAsync(ct);

        var dto = await LoadPlanDtoAsync(db, plan.No, ct);
        return Outcome.Ok(new ApiSuccess<ShippingPlanDto>(dto!));
    }

    private async Task ReturnPickedAsync(
        Domain.Picking.PickingPlan pick, decimal qty, string? planFactory, string? reasonCode,
        string? worker, DateTime now, CancellationToken ct)
    {
        var lots = await db.PickingResultLots
            .Where(l => l.PickingResult.SoNo == pick.SoNo
                     && l.PickingResult.SoBranchNo == pick.SoBranchNo && l.ItemCode == pick.ItemCode)
            .OrderByDescending(l => l.PickingResultNo).ThenByDescending(l => l.No)
            .ToListAsync(ct);
        if (lots.Count == 0)
        {
            var source = await db.PickingResults
                .Where(r => r.SoNo == pick.SoNo && r.SoBranchNo == pick.SoBranchNo && r.ItemCode == pick.ItemCode)
                .OrderByDescending(r => r.No).FirstOrDefaultAsync(ct);
            var location = source?.LocationCode ?? pick.LocationCode;
            var sourceLot = source?.LotNo;
            var stock = await db.Stocks.Where(s => s.ItemCode == pick.ItemCode && s.LocationCode == location
                && s.LotNo == sourceLot && !s.IsOnHold).OrderBy(s => s.SerialNo).FirstOrDefaultAsync(ct);
            if (stock is null)
            {
                var factory = planFactory ?? await db.Locations.Where(l => l.LocationCode == location && l.ZoneCode != null)
                    .Join(db.Zones, l => l.ZoneCode, z => z.ZoneCode, (l, z) => z.FactoryCode)
                    .FirstAsync(ct);
                stock = new Domain.Inventory.Stock
                {
                    ItemCode = pick.ItemCode, SerialNo = $"PCK-{source?.No ?? pick.No:D10}-{now:yyMMddHHmmss}",
                    LotNo = source?.LotNo, LocationCode = location, FactoryCode = factory,
                };
                db.Stocks.Add(stock);
            }
            stock.StockQty += qty;
            stock.Recalculate();
            stock.UpdatedAt = now;
            stock.UpdatedBy = worker;
            db.InventoryHistories.Add(new Domain.Inventory.InventoryHistory
            {
                CreatedAt = now, ItemCode = stock.ItemCode, FactoryCode = stock.FactoryCode,
                LocationCode = stock.LocationCode, LotNo = stock.LotNo, SerialNo = stock.SerialNo,
                ActionType = Domain.Inventory.InventoryAction.Picking, Qty = qty,
                ReasonCode = reasonCode, CreatedBy = worker,
            });
            return;
        }
        var remaining = qty;
        foreach (var lot in lots)
        {
            if (remaining <= 0) break;
            var returned = Math.Min(lot.Qty, remaining);
            if (returned <= 0) continue;
            remaining -= returned;
            lot.Qty -= returned;
            if (lot.Qty == 0) db.PickingResultLots.Remove(lot);
            var stock = await db.Stocks.SingleOrDefaultAsync(s => s.ItemCode == lot.ItemCode && s.SerialNo == lot.SerialNo, ct);
            if (stock is null)
            {
                var location = await db.PickingResults.Where(r => r.No == lot.PickingResultNo)
                    .Select(r => r.LocationCode).SingleAsync(ct);
                var factory = planFactory ?? await db.Locations.Where(l => l.LocationCode == location && l.ZoneCode != null)
                    .Join(db.Zones, l => l.ZoneCode, z => z.ZoneCode, (l, z) => z.FactoryCode)
                    .FirstAsync(ct);
                stock = new Domain.Inventory.Stock
                {
                    ItemCode = lot.ItemCode, SerialNo = lot.SerialNo, LotNo = lot.LotNo,
                    LocationCode = location, FactoryCode = factory,
                };
                db.Stocks.Add(stock);
            }
            stock.StockQty += returned;
            stock.Recalculate();
            stock.UpdatedAt = now;
            stock.UpdatedBy = worker;
            db.InventoryHistories.Add(new Domain.Inventory.InventoryHistory
            {
                CreatedAt = now, ItemCode = stock.ItemCode, FactoryCode = stock.FactoryCode,
                LocationCode = stock.LocationCode, LotNo = stock.LotNo, SerialNo = stock.SerialNo,
                ActionType = Domain.Inventory.InventoryAction.Picking, Qty = returned,
                ReasonCode = reasonCode, CreatedBy = worker,
            });
        }
        if (remaining > 0)
            throw new InvalidOperationException("Picking allocation is missing for stock restoration.");
    }

    public async Task<ServiceResult> ListResultsAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, DateOnly? dateFrom = null,
        DateOnly? dateTo = null, string? itemCode = null, string? itemName = null, string? slipNo = null,
        string? slipNoPresence = null)
    {
        var query = db.ShippingResults.AsNoTracking();

        if (dateFrom is { } from)
            query = query.Where(r => r.CreatedAt >= from.ToDateTime(TimeOnly.MinValue));
        if (dateTo is { } to)
            query = query.Where(r => r.CreatedAt < to.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(r => EF.Functions.Like(r.ItemCode, LikeContains(ic), LikeEscape));
        if (Trimmed(slipNo) is { } sn)
            query = query.Where(r => r.SoNo != null && EF.Functions.Like(r.SoNo, LikeContains(sn), LikeEscape));
        if (Trimmed(itemName) is { } iname)
            query = query.Where(r => db.Items.Any(i =>
                i.ItemCode == r.ItemCode && EF.Functions.Like(i.ItemName, LikeContains(iname), LikeEscape)));

        query = Trimmed(slipNoPresence) switch
        {
            "with" => query.Where(r => r.SoNo != null),
            "without" => query.Where(r => r.SoNo == null),
            _ => query,
        };

        query = ResultSort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await ProjectResults(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<ShippingResultDto>(
            rows.Select(MapResult).ToList(), new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> CreateResultAsync(
        ShippingResultWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (ValidateCode(body.ItemCode) is { } e1) return e1;
        if (body.Qty is not { } qty || qty < 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var itemCode = body.ItemCode.Trim();
        if (!await db.Items.AnyAsync(i => i.ItemCode == itemCode && !i.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));

        var locationCode = NullIfBlank(body.LocationCode)?.Trim();
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

        var slipNo = NullIfBlank(body.SlipNo)?.Trim();
        ShippingPlan? plan = null;

        if (slipNo is not null)
        {
            var candidates = db.ShippingPlans.Where(p =>
                p.SoNo == slipNo
                && p.ItemCode == itemCode
                && p.Status != ShippingStatus.Shipped
                && p.Status != ShippingStatus.Cancelled);
            var serialNo = NullIfBlank(body.SerialNo)?.Trim();
            if (serialNo is not null) candidates = candidates.Where(p => p.SoBranchNo == serialNo);

            var open = await candidates.ToListAsync(ct);
            if (open.Count != 1)
                return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Shipping plan")));

            plan = open[0];
            if (qty > plan.Qty) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty002()));
            if (qty < plan.Qty && reasonCode is null)
                return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));
        }

        var delta = qty - (plan?.ActualQty ?? 0m);
        if (delta < 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var now = DateTime.Now;
        var worker = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var picks = plan is null
            ? null
            : await db.PickingPlans
                .Where(p => p.SoNo == plan.SoNo && p.SoBranchNo == plan.SoBranchNo && p.ItemCode == itemCode
                         && p.Status != Domain.Picking.PickingStatus.Cancelled)
                .Select(p => new { p.PickedQty, p.LocationCode }).ToListAsync(ct);
        var taken = new List<Domain.Inventory.Stock>();
        var allocations = new List<Inventory.StockAllocation>();
        if (picks is { Count: > 0 })
        {
            if (qty > picks.Sum(p => p.PickedQty)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty002()));
        }
        else if (await Inventory.StockMovement.TakeAsync(db, itemCode, lotNo, locationCode, delta,
                     Domain.Inventory.InventoryAction.Shipping, reasonCode, worker, ErrorCatalog.Lot002(), ct, taken, allocations) is { } refused)
            return refused;

        var shelves = new List<string?>();
        if (picks is { Count: > 0 })
        {
            shelves.AddRange(await db.PickingResults
                .Where(r => r.SoNo == plan!.SoNo && r.SoBranchNo == plan.SoBranchNo && r.ItemCode == itemCode && r.Qty > 0)
                .Select(r => r.LocationCode).ToListAsync(ct));
            if (shelves.Count == 0) shelves.AddRange(picks.Select(p => p.LocationCode));
        }
        else
        {
            shelves.AddRange(taken.Select(s => s.LocationCode));
            if (plan is not null && plan.ActualQty > 0)
                shelves.Add(await db.ShippingResults
                    .Where(r => r.SoNo == plan.SoNo && r.SoBranchNo == plan.SoBranchNo && r.ItemCode == itemCode)
                    .OrderByDescending(r => r.No).Select(r => r.LocationCode).FirstOrDefaultAsync(ct));
        }
        var distinctShelves = shelves.Distinct().ToList();
        var shelf = locationCode ?? (distinctShelves.Count == 1 ? distinctShelves[0] : null);

        var row = new ShippingResult
        {
            SoNo = plan?.SoNo,
            SoBranchNo = plan?.SoBranchNo,
            ItemCode = itemCode,
            Qty = qty,
            LocationCode = shelf,
            LotNo = lotNo,
            ExpiryDate = body.ExpiryDate,
            CreatedAt = now,
            CreatedBy = worker,
        };
        db.ShippingResults.Add(row);
        foreach (var allocation in allocations)
            db.ShippingResultLots.Add(new ShippingResultLot
            {
                ShippingResult = row, CreatedAt = now, ItemCode = itemCode,
                LotNo = allocation.LotNo, SerialNo = allocation.SerialNo,
                Qty = allocation.Qty, CreatedBy = worker ?? "system",
            });
        if (plan is not null && picks is { Count: > 0 })
            await AddPickedShipmentLotsAsync(row, plan.SoNo, plan.SoBranchNo, itemCode,
                plan.ActualQty, delta, worker, ct);

        if (plan is not null)
        {
            plan.ActualQty = qty;
            plan.Status = qty >= plan.Qty ? ShippingStatus.Shipped : ShippingStatus.Partial;
            if (plan.Status == ShippingStatus.Shipped) plan.ReasonCode = null;
            else if (reasonCode is not null) plan.ReasonCode = reasonCode;
            plan.UpdatedAt = now;
            plan.UpdatedBy = worker;
        }

        await db.SaveChangesAsync(ct);

        var dto = await LoadResultDtoAsync(db, row.No, ct);
        return Outcome.Created($"/shipping-result?slipNo={row.SoNo}",
            new ApiSuccess<ShippingResultDto>(dto!));
    }

    private async Task AddPickedShipmentLotsAsync(ShippingResult result, string soNo, string branchNo,
        string itemCode, decimal alreadyShipped, decimal qty, string? worker, CancellationToken ct)
    {
        var lots = await db.PickingResultLots
            .Where(l => l.PickingResult.SoNo == soNo && l.PickingResult.SoBranchNo == branchNo && l.ItemCode == itemCode)
            .OrderBy(l => l.PickingResultNo).ThenBy(l => l.No).ToListAsync(ct);
        var skip = alreadyShipped;
        var remaining = qty;
        foreach (var lot in lots)
        {
            if (remaining <= 0) break;
            if (skip >= lot.Qty)
            {
                skip -= lot.Qty;
                continue;
            }
            var shipped = Math.Min(lot.Qty - skip, remaining);
            skip = 0;
            remaining -= shipped;
            db.ShippingResultLots.Add(new ShippingResultLot
            {
                ShippingResult = result, CreatedAt = result.CreatedAt, ItemCode = itemCode,
                LotNo = lot.LotNo, SerialNo = lot.SerialNo, Qty = shipped,
                CreatedBy = worker ?? "system",
            });
        }
    }

    public async Task<ServiceResult> ExportPlansCsvAsync(CancellationToken ct)
    {
        var rows = await ProjectPlans(db,
            db.ShippingPlans.AsNoTracking().OrderBy(p => p.ScheduledDate).ThenBy(p => p.No)).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("so_no,so_branch_no,partner_code,partner_name,item_code,item_name," +
                      "qty,unit,actual_qty,packing_type_code,packing_qty," +
                      "scheduled_date,gate_code,invoice_no,status,reason_code," +
                      "created_at,created_by");
        foreach (var r in rows)
            sb.Append(Csv(r.SlipNo)).Append(',').Append(Csv(r.SerialNo)).Append(',')
              .Append(Csv(r.CustomerCode)).Append(',').Append(Csv(r.CustomerName)).Append(',')
              .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
              .Append(Num(r.PlannedQty)).Append(',').Append(Csv(r.Unit)).Append(',')
              .Append(Num(r.ActualQty)).Append(',')
              .Append(Csv(r.PackingTypeCode)).Append(',').Append(Num(r.PackingQty)).Append(',')
              .Append(r.ScheduledDate?.ToString("yyyy-MM-dd") ?? "").Append(',')
              .Append(Csv(r.GateCode)).Append(',')
              .Append(Csv(r.InvoiceNo)).Append(',')
              .Append(Csv(ShippingStatus.ToApiName(r.Status))).Append(',')
              .Append(Csv(r.ShortageReason)).Append(',')
              .Append(r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(Csv(r.CreatedBy)).AppendLine();
        return CsvFile(sb, "shipping-schedule.csv");
    }

    public async Task<ServiceResult> ExportResultsCsvAsync(CancellationToken ct)
    {
        var rows = await ProjectResults(db,
            db.ShippingResults.AsNoTracking().OrderByDescending(r => r.CreatedAt).ThenBy(r => r.No))
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("no,created_at,item_code,item_name,qty,unit,so_no,so_branch_no," +
                      "scheduled_date,invoice_no,location_code,lot_no,worker,status," +
                      "reason_code,expiry_date");
        foreach (var r in rows)
            sb.Append(r.No).Append(',')
              .Append(r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
              .Append(Num(r.Qty)).Append(',').Append(Csv(r.Unit)).Append(',')
              .Append(Csv(r.SlipNo)).Append(',').Append(Csv(r.SerialNo)).Append(',')
              .Append(r.ScheduledDate?.ToString("yyyy-MM-dd") ?? "").Append(',')
              .Append(Csv(r.InvoiceNo)).Append(',')
              .Append(Csv(r.LocationCode)).Append(',').Append(Csv(r.LotNo)).Append(',')
              .Append(Csv(r.WorkerName ?? r.CreatedBy)).Append(',')
              .Append(Csv(ShippingStatus.ToApiName(r.PlanStatus ?? ShippingStatus.Shipped))).Append(',')
              .Append(Csv(r.ShortageReason)).Append(',')
              .Append(r.ExpiryDate?.ToString("yyyy-MM-dd") ?? "").AppendLine();
        return CsvFile(sb, "shipping-result.csv");
    }

    private sealed record PlanRow(
        string SlipNo, string SerialNo, string? CustomerCode, string? CustomerName,
        string ItemCode, string? ItemName, decimal PlannedQty, string? Unit, decimal ActualQty,
        string? PackingTypeCode, decimal? PackingQty,
        DateOnly? ScheduledDate, string? GateCode, string? InvoiceNo, int Status, string? ShortageReason,
        bool LabelIssued, DateTime CreatedAt, string? CreatedBy);

    private sealed record ResultRow(
        long No, DateTime CreatedAt, string ItemCode, string? ItemName, decimal Qty, string? Unit,
        string? SlipNo, string? SerialNo, string? CustomerCode, string? CustomerName,
        string? LocationCode, string? LotNo, DateOnly? ExpiryDate, DateOnly? ScheduledDate,
        string? InvoiceNo, string? WorkerName, string? CreatedBy, int? PlanStatus,
        string? ShortageReason);

    private static IQueryable<PlanRow> ProjectPlans(ImsDbContext db, IQueryable<ShippingPlan> q) =>
        q.Select(p => new PlanRow(
            p.SoNo,
            p.SoBranchNo,
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
            p.GateCode,
            p.InvoiceNo,
            p.Status,
            p.ReasonCode,
            db.PrintHistories.Any(h => h.Category == PrintCategory
                                    && h.DocNo == p.SoNo && h.ItemCode == p.ItemCode),
            p.CreatedAt,
            p.CreatedBy));

    private static IQueryable<ResultRow> ProjectResults(ImsDbContext db, IQueryable<ShippingResult> q) =>
        q.Select(r => new ResultRow(
            r.No,
            r.CreatedAt,
            r.ItemCode,
            db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            r.Qty,
            db.Items.Where(i => i.ItemCode == r.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            r.SoNo,
            r.SoBranchNo,
            db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo)
                .Select(p => p.PartnerCode).FirstOrDefault(),
            db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo)
                .Select(p => db.Partners.Where(x => x.PartnerCode == p.PartnerCode)
                    .Select(x => x.PartnerName).FirstOrDefault()).FirstOrDefault(),
            r.LocationCode,
            r.LotNo,
            r.ExpiryDate,
            db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo)
                .Select(p => p.ScheduledDate).FirstOrDefault(),
            db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo)
                .Select(p => p.InvoiceNo).FirstOrDefault(),
            db.AppUsers.Where(u => u.UserCode == r.CreatedBy).Select(u => u.UserName).FirstOrDefault(),
            r.CreatedBy,
            db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo)
                .Select(p => (int?)p.Status).FirstOrDefault(),
            db.ShippingPlans.Where(p => p.SoNo == r.SoNo && p.SoBranchNo == r.SoBranchNo)
                .Select(p => p.ReasonCode).FirstOrDefault()));

    private static ShippingPlanDto MapPlan(PlanRow r) => new(
        r.SlipNo, r.SerialNo, r.CustomerCode, r.CustomerName, r.ItemCode, r.ItemName,
        r.PlannedQty, r.Unit, r.ActualQty, r.PackingTypeCode, r.PackingQty,
        r.ScheduledDate, r.GateCode, r.InvoiceNo,
        ShippingStatus.ToApiName(r.Status), r.ShortageReason, r.LabelIssued,
        r.CreatedAt, r.CreatedBy);

    private static ShippingResultDto MapResult(ResultRow r) => new(
        r.No, r.CreatedAt, r.ItemCode, r.ItemName, r.Qty, r.Unit, r.SlipNo, r.SerialNo,
        r.CustomerCode, r.CustomerName, r.LocationCode, r.LotNo, r.ExpiryDate,
        r.ScheduledDate, r.InvoiceNo,
        r.WorkerName ?? r.CreatedBy,
        ShippingStatus.ToApiName(r.PlanStatus ?? ShippingStatus.Shipped),
        r.ShortageReason);

    private static async Task<ShippingPlanDto?> LoadPlanDtoAsync(ImsDbContext db, long no, CancellationToken ct)
    {
        var row = await ProjectPlans(db, db.ShippingPlans.AsNoTracking().Where(p => p.No == no))
            .FirstOrDefaultAsync(ct);
        return row is null ? null : MapPlan(row);
    }

    private static async Task<ShippingResultDto?> LoadResultDtoAsync(ImsDbContext db, long no, CancellationToken ct)
    {
        var row = await ProjectResults(db, db.ShippingResults.AsNoTracking().Where(r => r.No == no))
            .FirstOrDefaultAsync(ct);
        return row is null ? null : MapResult(row);
    }

    private static string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
