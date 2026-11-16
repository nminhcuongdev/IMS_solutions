using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Inventory;

internal sealed class InventoryService(ImsDbContext db) : IInventoryService
{
    private const int ExpiringSoonDays = 30;

    private readonly SortMap<Stock> StockSort = new SortMap<Stock>()
        .Add("item_code", s => s.ItemCode)
        .Add("serial_no", s => s.SerialNo)
        .Add("lot_no", s => s.LotNo)
        .Add("location_code", s => s.LocationCode)
        .Add("stock_qty", s => s.StockQty)
        .Add("expiry_date", s => s.ExpiryDate)
        .Add("updated_at", s => s.UpdatedAt)
        .Add("no", s => s.ItemCode, s => s.SerialNo)
        .Add("item_name", s => db.Items.Where(i => i.ItemCode == s.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("category", s => db.Items.Where(i => i.ItemCode == s.ItemCode)
            .Select(i => db.ItemGroups.Where(g => g.ItemGroupCode == i.ItemGroupCode)
                .Select(g => g.ItemGroupName).FirstOrDefault()).FirstOrDefault())
        .Add("warehouse", s => db.Locations.Where(l => l.LocationCode == s.LocationCode).Select(l => l.LargeCode).FirstOrDefault())
        .Add("hold", s => s.IsOnHold)
        .Add("hold_reason", s => db.Reasons.Where(r => r.ReasonCode == s.ReasonCode).Select(r => r.ReasonName).FirstOrDefault())
        .Add("unit", s => db.Items.Where(i => i.ItemCode == s.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("reorder_point", s => db.Items.Where(i => i.ItemCode == s.ItemCode).Select(i => i.ReorderLevel1).FirstOrDefault())
        .Add("over_short", s => s.StockQty - db.Items.Where(i => i.ItemCode == s.ItemCode).Select(i => i.ReorderLevel1).FirstOrDefault())
        .Add("remaining_days", s => s.ExpiryDate)
        .Default("item_code")
        .ThenAlwaysBy(s => s.SerialNo);

    private readonly SortMap<HistorySourceRow> HistorySort = new SortMap<HistorySourceRow>()
        .Add("created_at", h => h.CreatedAt)
        .Add("item_code", h => h.ItemCode)
        .Add("action_type", h => h.ActionType)
        .Add("qty", h => h.Qty)
        .Add("no", h => h.No)
        .Add("item_name", h => db.Items.Where(i => i.ItemCode == h.ItemCode).Select(i => i.ItemName).FirstOrDefault())
        .Add("lot_no", h => h.LotNo)
        .Add("unit", h => db.Items.Where(i => i.ItemCode == h.ItemCode).Select(i => i.UomCode).FirstOrDefault())
        .Add("location_code", h => h.LocationCode)
        .Add("reason", h => db.Reasons.Where(r => r.ReasonCode == h.ReasonCode).Select(r => r.ReasonName).FirstOrDefault())
        .Add("worker", h => db.AppUsers.Where(u => u.UserCode == h.CreatedBy).Select(u => u.UserName).FirstOrDefault() ?? h.CreatedBy)
        .Default("created_at")
        .ThenAlwaysBy(h => h.No);

    public async Task<ServiceResult> ListStockAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        string? warehouse = null, string? itemCode = null, string? itemName = null, string? category = null,
        string? lot = null, string? expiration = null, string? stockLevel = null, string? hold = null,
        string? groupBy = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var query = Filter(db, warehouse, itemCode, itemName, category, lot, expiration, stockLevel, hold, q, today);

        var axis = Trimmed(groupBy)?.ToLowerInvariant();
        if (axis is "item" or "location")
            return await GroupedAsync(db, query, axis, page, pageSize, sort, ct);

        query = StockSort.Apply(query, sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await Project(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<StockDto>(
            rows.Select(r => Map(r, today)).ToList(), new PageMeta(total, pg, sz)));
    }

    private static IQueryable<Stock> Filter(
        ImsDbContext db, string? warehouse, string? itemCode, string? itemName, string? category, string? lot,
        string? expiration, string? stockLevel, string? hold, string? q, DateOnly today)
    {
        var query = db.Stocks.AsNoTracking();

        if (Trimmed(warehouse) is { } wh)
            query = query.Where(s => db.Locations.Any(l => l.LocationCode == s.LocationCode && l.LargeCode == wh));
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(s => EF.Functions.Like(s.ItemCode, LikeContains(ic), LikeEscape));
        if (Trimmed(lot) is { } lt)
            query = query.Where(s => s.LotNo != null && EF.Functions.Like(s.LotNo, LikeContains(lt), LikeEscape));
        if (Trimmed(itemName) is { } iname)
            query = query.Where(s => db.Items.Any(i =>
                i.ItemCode == s.ItemCode && EF.Functions.Like(i.ItemName, LikeContains(iname), LikeEscape)));
        if (Trimmed(category) is { } cat)
        {
            if (Enum.TryParse<ItemGroupCategory>(cat, true, out var kind) && Enum.IsDefined(kind))
                query = query.Where(s => db.Items.Any(i => i.ItemCode == s.ItemCode
                    && db.ItemGroups.Any(g => g.ItemGroupCode == i.ItemGroupCode && g.Category == kind)));
            else
                query = query.Where(s => db.Items.Any(i => i.ItemCode == s.ItemCode && i.ItemGroupCode == cat));
        }
        if (Trimmed(q) is { } free)
            query = query.Where(s => EF.Functions.Like(s.ItemCode, LikeContains(free), LikeEscape)
                                  || (s.LotNo != null && EF.Functions.Like(s.LotNo, LikeContains(free), LikeEscape)));

        query = Trimmed(hold) switch
        {
            "hold" => query.Where(s => s.IsOnHold),
            "notHold" => query.Where(s => !s.IsOnHold),
            _ => query,
        };

        var soon = today.AddDays(ExpiringSoonDays);
        query = Trimmed(expiration) switch
        {
            "expired" => query.Where(s => s.ExpiryDate != null && s.ExpiryDate < today),
            "expiringSoon" => query.Where(s => s.ExpiryDate != null && s.ExpiryDate >= today && s.ExpiryDate <= soon),
            _ => query,
        };

        query = Trimmed(stockLevel) switch
        {
            "low" => query.Where(s => db.Items.Any(i =>
                i.ItemCode == s.ItemCode && i.ReorderLevel1 != null && s.StockQty < i.ReorderLevel1)),
            "overflow" => query.Where(s => db.Items.Any(i =>
                i.ItemCode == s.ItemCode && i.SurplusPoint != null && s.StockQty >= i.SurplusPoint)),
            _ => query,
        };

        return query;
    }

    private static async Task<ServiceResult> GroupedAsync(
        ImsDbContext db, IQueryable<Stock> query, string axis, int? page, int? pageSize, string? sort,
        CancellationToken ct)
    {
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var todayForGroups = DateOnly.FromDateTime(DateTime.Now);

        if (axis == "item")
        {
            var grouped = query.GroupBy(s => s.ItemCode).Select(g => new ItemGroupRow
            {
                Key = g.Key,
                StockQty = g.Sum(x => x.StockQty),
                LotCount = g.Select(x => x.LotNo ?? "").Distinct().Count(),
                AnyOnHold = g.Any(x => x.IsOnHold),
                EarliestExpiry = g.Min(x => x.ExpiryDate),
            });
            var total = await grouped.CountAsync(ct);
            var projected = grouped.Select(x => new ItemGroupRow
            {
                Key = x.Key, StockQty = x.StockQty, LotCount = x.LotCount, AnyOnHold = x.AnyOnHold,
                EarliestExpiry = x.EarliestExpiry,
                Name = db.Items.Where(i => i.ItemCode == x.Key).Select(i => i.ItemName).FirstOrDefault(),
                ReorderPoint = db.Items.Where(i => i.ItemCode == x.Key).Select(i => i.ReorderLevel1).FirstOrDefault(),
                Unit = db.Items.Where(i => i.ItemCode == x.Key).Select(i => i.UomCode).FirstOrDefault(),
                Category = db.Items.Where(i => i.ItemCode == x.Key)
                    .Select(i => db.ItemGroups.Where(g2 => g2.ItemGroupCode == i.ItemGroupCode)
                        .Select(g2 => g2.ItemGroupName).FirstOrDefault()).FirstOrDefault(),
            });
            var rows = await ItemGroupSort.Apply(projected, sort).Skip((pg - 1) * sz).Take(sz).ToListAsync(ct);

            return Outcome.Ok(new ApiListSuccess<StockGroupDto>(
                rows.Select(x => new StockGroupDto(
                    x.Key, x.Name, x.Category, null, x.Unit, x.StockQty, x.LotCount, 1,
                    x.AnyOnHold, x.EarliestExpiry,
                    x.EarliestExpiry is { } e ? e.DayNumber - todayForGroups.DayNumber : null,
                    x.ReorderPoint)).ToList(),
                new PageMeta(total, pg, sz)));
        }

        var byLoc = query.Where(s => s.LocationCode != null)
            .GroupBy(s => s.LocationCode!).Select(g => new LocationGroupRow
            {
                Key = g.Key,
                StockQty = g.Sum(x => x.StockQty),
                LotCount = g.Select(x => x.LotNo ?? "").Distinct().Count(),
                ItemCount = g.Select(x => x.ItemCode).Distinct().Count(),
            });
        var totalLoc = await byLoc.CountAsync(ct);
        var locProjected = byLoc.Select(x => new LocationGroupRow
        {
            Key = x.Key, StockQty = x.StockQty, LotCount = x.LotCount, ItemCount = x.ItemCount,
            Warehouse = db.Locations.Where(l => l.LocationCode == x.Key).Select(l => l.LargeCode).FirstOrDefault(),
            Name = db.Locations.Where(l => l.LocationCode == x.Key).Select(l => l.LocationName).FirstOrDefault(),
        });
        var locRows = await LocationGroupSort.Apply(locProjected, sort).Skip((pg - 1) * sz).Take(sz)
            .ToListAsync(ct);

        return Outcome.Ok(new ApiListSuccess<StockGroupDto>(
            locRows.Select(x => new StockGroupDto(
                x.Key, x.Name, null, x.Warehouse, null, x.StockQty, x.LotCount, x.ItemCount)).ToList(),
            new PageMeta(totalLoc, pg, sz)));
    }

    public async Task<ServiceResult> AdjustStockAsync(StockAdjustDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (body.StockQty < 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));
        var reason = Trimmed(body.ReasonCode);
        if (reason is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));

        var row = await FindAsync(db, body.ItemCode, body.SerialNo, ct);
        if (row is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Stock")));
        var category = await db.Reasons.Where(x => x.ReasonCode == reason && !x.IsDeleted)
            .Select(x => (ReasonCategory?)x.ReasonCategoryValue).FirstOrDefaultAsync(ct);
        if (category is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")));
        if (category != ReasonCategory.Adjustment) return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));
        if (body.StockQty < row.ReservedQty) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var delta = body.StockQty - row.StockQty;
        var who = user.FindFirstValue(ClaimTypes.NameIdentifier);

        row.StockQty = body.StockQty;
        row.Recalculate();
        Touch(row, who);

        if (delta != 0) db.InventoryHistories.Add(History(row, InventoryAction.Adjust, delta, reason, who));

        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<StockDto>(await LoadDtoAsync(db, row, ct)));
    }

    public async Task<ServiceResult> HoldStockAsync(StockHoldDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var reason = Trimmed(body.ReasonCode);
        if (reason is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));
        if (!await db.Reasons.AnyAsync(x => x.ReasonCode == reason && !x.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")));

        return await BulkAsync(db, user, body.Keys, ct,
            canApply: s => !s.IsOnHold,
            apply: (s, who) =>
            {
                s.IsOnHold = true;
                s.ReasonCode = reason;
                s.Recalculate();
                return History(s, InventoryAction.Hold, null, reason, who);
            });
    }

    public async Task<ServiceResult> ReleaseHoldAsync(StockKeysDto body, ClaimsPrincipal user, CancellationToken ct) =>
        await BulkAsync(db, user, body.Keys, ct,
            canApply: s => s.IsOnHold,
            apply: (s, who) =>
            {
                var was = s.ReasonCode;
                s.IsOnHold = false;
                s.ReasonCode = null;
                s.Recalculate();
                return History(s, InventoryAction.HoldRelease, null, was, who);
            });

    public async Task<ServiceResult> DisposeStockAsync(StockDisposalDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var requestId = RequestReplays.Normalize(body.RequestId);
        if (RequestReplays.Invalid(requestId) is { } badId) return badId;
        if (await RequestReplays.FindAsync(db, requestId, ct) is { } replay) return replay;
        var reason = Trimmed(body.ReasonCode);
        if (reason is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));

        var row = await FindAsync(db, body.ItemCode, body.SerialNo, ct);
        if (row is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Stock")));
        if (!await db.Reasons.AnyAsync(x => x.ReasonCode == reason && !x.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Reason")));
        if (body.Qty is <= 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty003()));
        if (row.IsOnHold) return Outcome.Conflict(new ApiError(ErrorCatalog.Hld001()));

        var disposed = body.Qty ?? row.AvailableQty;
        if (disposed <= 0 || disposed > row.AvailableQty)
            return Outcome.Conflict(new ApiError(ErrorCatalog.Inv009()));

        var who = user.FindFirstValue(ClaimTypes.NameIdentifier);

        row.StockQty -= disposed;
        row.Recalculate();
        Touch(row, who);
        db.InventoryHistories.Add(History(row, InventoryAction.Disposal, -disposed, reason, who));

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        var result = Outcome.Ok(new ApiSuccess<StockDto>(await LoadDtoAsync(db, row, ct)));
        RequestReplays.Record(db, requestId, "disposal", result, who);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<ServiceResult> TransferStockAsync(StockTransferDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var requestId = RequestReplays.Normalize(body.RequestId);
        if (RequestReplays.Invalid(requestId) is { } badId) return badId;
        if (await RequestReplays.FindAsync(db, requestId, ct) is { } replay) return replay;
        var to = Trimmed(body.ToLocationCode);
        if (to is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Loc001()));

        var row = await FindAsync(db, body.ItemCode, body.SerialNo, ct);
        if (row is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Stock")));

        var dest = await db.Locations
            .FirstOrDefaultAsync(l => l.LocationCode == to && !l.IsDeleted, ct);
        if (dest is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));
        if (await db.Locations.AnyAsync(l => l.ParentLocationCode == dest.LocationCode && !l.IsDeleted, ct))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Loc001()));

        var destFactory = await LocationFactory.OfAsync(db, dest.LocationCode, ct);
        if (destFactory is null) return Outcome.Conflict(new ApiError(ErrorCatalog.Loc003NoFactory()));

        if (string.Equals(row.LocationCode, to, StringComparison.OrdinalIgnoreCase))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Loc001()));

        if (row.StockQty <= 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var who = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var from = row.LocationCode;

        db.LocationTransfers.Add(new LocationTransfer
        {
            CreatedAt = DateTime.Now,
            ItemCode = row.ItemCode,
            LotNo = row.LotNo,
            FromFactoryCode = row.FactoryCode,
            FromLocationCode = from!,
            ToFactoryCode = destFactory,
            ToLocationCode = to,
            Qty = row.StockQty,
            CreatedBy = who,
        });

        row.LocationCode = to;
        row.FactoryCode = destFactory;
        row.Recalculate();
        Touch(row, who);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        var result = Outcome.Ok(new ApiSuccess<StockDto>(await LoadDtoAsync(db, row, ct)));
        RequestReplays.Record(db, requestId, "transfer", result, who);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<ServiceResult> ListHistoryAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? action = null)
    {
        var query = HistoryFilter(db, dateFrom, dateTo, itemCode, itemName, action, q);
        query = HistorySort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await ProjectHistory(db, query.Skip((pg - 1) * sz).Take(sz)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<InventoryHistoryDto>(
            rows.Select(MapHistory).ToList(), new PageMeta(total, pg, sz)));
    }

    private static IQueryable<HistorySourceRow> HistoryFilter(
        ImsDbContext db, DateOnly? dateFrom, DateOnly? dateTo, string? itemCode, string? itemName, string? action,
        string? q)
    {
        var stored = db.InventoryHistories.AsNoTracking().Select(h => new HistorySourceRow
        {
            No = h.No, CreatedAt = h.CreatedAt, ItemCode = h.ItemCode, LotNo = h.LotNo,
            ActionType = h.ActionType, Qty = h.Qty, LocationCode = h.LocationCode,
            ReasonCode = h.ReasonCode, CreatedBy = h.CreatedBy,
        });
        var picked = db.ProductionOrderLines.AsNoTracking().Where(l => l.PickedAt != null)
            .Select(l => new HistorySourceRow
            {
                No = -1000000000000L - l.No, CreatedAt = l.PickedAt!.Value,
                ItemCode = l.ItemCode, LotNo = null, ActionType = InventoryAction.ProdPick,
                Qty = l.PlannedQty, LocationCode = null, ReasonCode = null, CreatedBy = l.PickedBy,
            });
        var received = db.ProductionOrders.AsNoTracking().Where(o => o.ReceivedAt != null)
            .Select(o => new HistorySourceRow
            {
                No = -2000000000000L - o.No, CreatedAt = o.ReceivedAt!.Value,
                ItemCode = o.OutputItemCode, LotNo = null, ActionType = InventoryAction.ProdReceipt,
                Qty = null, LocationCode = null, ReasonCode = null, CreatedBy = o.ReceivedBy,
            });
        var labels = db.ProductionOutputs.AsNoTracking()
            .Where(l => l.LabelIssuedAt != null && l.ReversesNo == null)
            .GroupBy(l => new { l.RequestId, l.ItemCode, l.LotNo })
            .Select(g => new HistorySourceRow
            {
                No = -3000000000000L - g.Min(l => l.No), CreatedAt = g.Min(l => l.LabelIssuedAt!.Value), ItemCode = g.Key.ItemCode,
                LotNo = g.Key.LotNo, ActionType = InventoryAction.ProdLabel, Qty = g.Count(),
                LocationCode = null, ReasonCode = null, CreatedBy = g.Max(l => l.LabelIssuedBy),
            });
        var selectedAction = Trimmed(action);
        IQueryable<HistorySourceRow> query;
        if (selectedAction is not null && InventoryAction.IsDefined(selectedAction))
            query = selectedAction switch
            {
                InventoryAction.ProdPick => picked,
                InventoryAction.ProdReceipt => received,
                InventoryAction.ProdLabel => labels,
                _ => stored.Where(h => h.ActionType == selectedAction),
            };
        else
            query = stored.Concat(picked).Concat(received).Concat(labels);

        if (dateFrom is { } from)
            query = query.Where(h => h.CreatedAt >= from.ToDateTime(TimeOnly.MinValue));
        if (dateTo is { } to)
            query = query.Where(h => h.CreatedAt < to.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (Trimmed(itemCode) is { } ic)
            query = query.Where(h => EF.Functions.Like(h.ItemCode, LikeContains(ic), LikeEscape));
        if (Trimmed(itemName) is { } iname)
            query = query.Where(h => db.Items.Any(i =>
                i.ItemCode == h.ItemCode && EF.Functions.Like(i.ItemName, LikeContains(iname), LikeEscape)));
        if (Trimmed(q) is { } free)
            query = query.Where(h => EF.Functions.Like(h.ItemCode, LikeContains(free), LikeEscape)
                                  || (h.LocationCode != null && EF.Functions.Like(h.LocationCode, LikeContains(free), LikeEscape))
                                  || (h.CreatedBy != null && EF.Functions.Like(h.CreatedBy, LikeContains(free), LikeEscape)));
        return query;
    }

    public async Task<ServiceResult> RestoreDisposalAsync(RestoreDisposalDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var nos = (body.Nos ?? Array.Empty<long>()).Distinct().ToList();
        if (nos.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));

        var rows = await db.InventoryHistories.AsNoTracking().Where(h => nos.Contains(h.No)).ToListAsync(ct);
        var who = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var updated = 0;
        var skipped = new List<string>();

        foreach (var no in nos)
        {
            var h = rows.FirstOrDefault(x => x.No == no);
            if (h is null || h.ActionType != InventoryAction.Disposal || h.Qty is not { } q || q >= 0)
            {
                skipped.Add(no.ToString());
                continue;
            }

            var amount = -q;
            var reference = no.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var already = await db.InventoryHistories.AnyAsync(x =>
                    x.ActionType == InventoryAction.RestoreDisposal && x.ReferenceNo == reference, ct)
                || (h.SerialNo is null
                    ? await db.InventoryHistories.AnyAsync(x =>
                        x.No > no && x.ActionType == InventoryAction.Adjust && x.ReferenceNo == null &&
                        x.ItemCode == h.ItemCode && x.LotNo == h.LotNo && x.Qty == amount, ct)
                    : await db.InventoryHistories.AnyAsync(x =>
                        x.No > no && x.ActionType == InventoryAction.Adjust && x.ReferenceNo == null &&
                        x.ItemCode == h.ItemCode && x.SerialNo == h.SerialNo &&
                        x.ReasonCode == h.ReasonCode && x.Qty == amount, ct));
            if (already) { skipped.Add(no.ToString()); continue; }

            var stock = h.SerialNo is null
                ? await db.Stocks.FirstOrDefaultAsync(s =>
                    s.ItemCode == h.ItemCode && s.LotNo == h.LotNo && s.LocationCode == h.LocationCode, ct)
                : await db.Stocks.FirstOrDefaultAsync(s =>
                    s.ItemCode == h.ItemCode && s.SerialNo == h.SerialNo, ct);
            if (stock is null) { skipped.Add(no.ToString()); continue; }

            stock.StockQty += amount;
            stock.Recalculate();
            Touch(stock, who);
            var restored = History(stock, InventoryAction.RestoreDisposal, amount, h.ReasonCode, who);
            restored.ReferenceNo = reference;
            db.InventoryHistories.Add(restored);
            updated++;
        }

        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<StockBulkResult>(new StockBulkResult(updated, skipped)));
    }

    public async Task<ServiceResult> ExportStockCsvAsync(
        CancellationToken ct, string? q = null, string? warehouse = null, string? itemCode = null,
        string? itemName = null, string? category = null, string? lot = null, string? expiration = null,
        string? stockLevel = null, string? hold = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var query = Filter(db, warehouse, itemCode, itemName, category, lot, expiration, stockLevel, hold, q, today);
        var rows = await Project(db, query.OrderBy(s => s.ItemCode).ThenBy(s => s.SerialNo)).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("item_code,item_name,category,warehouse,location_code,lot_no,serial_no," +
                      "is_on_hold,hold_reason,stock_qty,unit,reorder_point,surplus_point,over_short," +
                      "updated_at,expiry_date,remaining_days");
        foreach (var r in rows)
        {
            var d = Map(r, today);
            sb.Append(Csv(d.ItemCode)).Append(',').Append(Csv(d.ItemName)).Append(',')
              .Append(Csv(d.Category)).Append(',').Append(Csv(d.Warehouse)).Append(',')
              .Append(Csv(d.LocationCode)).Append(',').Append(Csv(d.LotNo)).Append(',')
              .Append(Csv(d.SerialNo)).Append(',')
              .Append(d.IsOnHold ? "1" : "0").Append(',').Append(Csv(d.HoldReason)).Append(',')
              .Append(Num(d.StockQty)).Append(',').Append(Csv(d.Unit)).Append(',')
              .Append(Num(d.ReorderPoint)).Append(',').Append(Num(d.SurplusPoint)).Append(',')
              .Append(Num(d.OverShort)).Append(',')
              .Append(d.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(d.ExpiryDate?.ToString("yyyy-MM-dd") ?? "").Append(',')
              .Append(Num(d.RemainingDays)).AppendLine();
        }
        return CsvFile(sb, "stock.csv");
    }

    public async Task<ServiceResult> ExportHistoryCsvAsync(
        CancellationToken ct, string? q = null, DateOnly? dateFrom = null, DateOnly? dateTo = null,
        string? itemCode = null, string? itemName = null, string? action = null)
    {
        var query = HistoryFilter(db, dateFrom, dateTo, itemCode, itemName, action, q);
        var rows = await ProjectHistory(db, query.OrderByDescending(h => h.CreatedAt).ThenBy(h => h.No))
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("no,created_at,item_code,item_name,lot_no,action_type,qty,unit," +
                      "location_code,reason_code,reason_name,worker");
        foreach (var r in rows)
            sb.Append(r.No).Append(',')
              .Append(r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
              .Append(Csv(r.ItemCode)).Append(',').Append(Csv(r.ItemName)).Append(',')
              .Append(Csv(r.LotNo)).Append(',').Append(Csv(r.ActionType)).Append(',')
              .Append(Num(r.Qty)).Append(',').Append(Csv(r.Unit)).Append(',')
              .Append(Csv(r.LocationCode)).Append(',')
              .Append(Csv(r.ReasonCode)).Append(',').Append(Csv(r.ReasonName)).Append(',')
              .Append(Csv(r.WorkerName ?? r.CreatedBy)).AppendLine();
        return CsvFile(sb, "inventory-history.csv");
    }

    private sealed class ItemGroupRow
    {
        public string Key { get; init; } = "";
        public decimal StockQty { get; init; }
        public int LotCount { get; init; }
        public bool AnyOnHold { get; init; }
        public DateOnly? EarliestExpiry { get; init; }
        public string? Name { get; init; }
        public decimal? ReorderPoint { get; init; }
        public string? Unit { get; init; }
        public string? Category { get; init; }
    }

    private sealed class LocationGroupRow
    {
        public string Key { get; init; } = "";
        public decimal StockQty { get; init; }
        public int LotCount { get; init; }
        public int ItemCount { get; init; }
        public string? Warehouse { get; init; }
        public string? Name { get; init; }
    }

    private static readonly SortMap<ItemGroupRow> ItemGroupSort = new SortMap<ItemGroupRow>()
        .Add("no", x => x.Key)
        .Add("item_code", x => x.Key)
        .Add("item_name", x => x.Name)
        .Add("category", x => x.Category)
        .Add("stock_qty", x => x.StockQty)
        .Add("unit", x => x.Unit)
        .Add("lot_count", x => x.LotCount)
        .Default("item_code")
        .ThenAlwaysBy(x => x.Key);

    private static readonly SortMap<LocationGroupRow> LocationGroupSort = new SortMap<LocationGroupRow>()
        .Add("no", x => x.Key)
        .Add("location_code", x => x.Key)
        .Add("warehouse", x => x.Warehouse)
        .Add("item_count", x => x.ItemCount)
        .Add("stock_qty", x => x.StockQty)
        .Add("lot_count", x => x.LotCount)
        .Default("location_code")
        .ThenAlwaysBy(x => x.Key);

    private sealed record StockRow(
        string ItemCode, string SerialNo, string? ItemName, string? Category,
        string? Warehouse, string? LocationCode, string? LocationName, string? LotNo,
        bool IsOnHold, string? ReasonName, decimal StockQty, decimal ReservedQty,
        decimal AvailableQty, string? Unit, decimal? ReorderPoint, decimal? SurplusPoint,
        DateTime UpdatedAt, DateOnly? ExpiryDate);

    private sealed record HistoryRow(
        long No, DateTime CreatedAt, string ItemCode, string? ItemName, string? LotNo,
        string ActionType, decimal? Qty, string? Unit, string? LocationCode,
        string? ReasonCode, string? ReasonName, string? WorkerName, string? CreatedBy);

    private static IQueryable<StockRow> Project(ImsDbContext db, IQueryable<Stock> q) =>
        q.Select(s => new StockRow(
            s.ItemCode,
            s.SerialNo,
            db.Items.Where(i => i.ItemCode == s.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            db.Items.Where(i => i.ItemCode == s.ItemCode)
                .Select(i => db.ItemGroups.Where(g => g.ItemGroupCode == i.ItemGroupCode)
                    .Select(g => g.ItemGroupName).FirstOrDefault()).FirstOrDefault(),
            db.Locations.Where(l => l.LocationCode == s.LocationCode).Select(l => l.LargeCode).FirstOrDefault(),
            s.LocationCode,
            db.Locations.Where(l => l.LocationCode == s.LocationCode).Select(l => l.LocationName).FirstOrDefault(),
            s.LotNo,
            s.IsOnHold,
            db.Reasons.Where(r => r.ReasonCode == s.ReasonCode).Select(r => r.ReasonName).FirstOrDefault(),
            s.StockQty,
            s.ReservedQty,
            s.AvailableQty,
            db.Items.Where(i => i.ItemCode == s.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            db.Items.Where(i => i.ItemCode == s.ItemCode).Select(i => i.ReorderLevel1).FirstOrDefault(),
            db.Items.Where(i => i.ItemCode == s.ItemCode).Select(i => i.SurplusPoint).FirstOrDefault(),
            s.UpdatedAt,
            s.ExpiryDate));

    private static IQueryable<HistoryRow> ProjectHistory(ImsDbContext db, IQueryable<HistorySourceRow> q) =>
        q.Select(h => new HistoryRow(
            h.No,
            h.CreatedAt,
            h.ItemCode,
            db.Items.Where(i => i.ItemCode == h.ItemCode).Select(i => i.ItemName).FirstOrDefault(),
            h.LotNo,
            h.ActionType,
            h.Qty,
            db.Items.Where(i => i.ItemCode == h.ItemCode).Select(i => i.UomCode).FirstOrDefault(),
            h.LocationCode,
            h.ReasonCode,
            db.Reasons.Where(r => r.ReasonCode == h.ReasonCode).Select(r => r.ReasonName).FirstOrDefault(),
            db.AppUsers.Where(u => u.UserCode == h.CreatedBy).Select(u => u.UserName).FirstOrDefault(),
            h.CreatedBy));

    private static StockDto Map(StockRow r, DateOnly today) => new(
        r.ItemCode, r.SerialNo, r.ItemName, r.Category, r.Warehouse, r.LocationCode,
        r.LocationName, r.LotNo, r.IsOnHold, r.ReasonName,
        r.StockQty, r.ReservedQty, r.AvailableQty, r.Unit, r.ReorderPoint, r.SurplusPoint,
        r.ReorderPoint is { } rp ? r.StockQty - rp : null,
        r.UpdatedAt, r.ExpiryDate,
        r.ExpiryDate is { } exp ? exp.DayNumber - today.DayNumber : null);

    private static InventoryHistoryDto MapHistory(HistoryRow r) => new(
        r.No, r.CreatedAt, r.ItemCode, r.ItemName, r.LotNo,
        r.ActionType, InventoryAction.ToLabelKey(r.ActionType),
        r.Qty, r.Unit, r.LocationCode, r.ReasonCode, r.ReasonName,
        r.WorkerName ?? r.CreatedBy);

    private static Task<Stock?> FindAsync(ImsDbContext db, string itemCode, string serialNo, CancellationToken ct)
    {
        var ic = itemCode?.Trim() ?? "";
        var sn = serialNo?.Trim() ?? "";
        return db.Stocks.FirstOrDefaultAsync(s => s.ItemCode == ic && s.SerialNo == sn, ct);
    }

    private static void Touch(Stock s, string? who)
    {
        s.UpdatedAt = DateTime.Now;
        s.UpdatedBy = who;
    }

    private static InventoryHistory History(Stock s, string action, decimal? qty, string? reason, string? who) => new()
        {
            CreatedAt = DateTime.Now,
            ItemCode = s.ItemCode,
            FactoryCode = s.FactoryCode,
            LocationCode = s.LocationCode,
            LotNo = s.LotNo,
            SerialNo = s.SerialNo,
            ActionType = action,
            Qty = qty,
            ReasonCode = reason,
            CreatedBy = who,
        };

    private static async Task<ServiceResult> BulkAsync(
        ImsDbContext db, ClaimsPrincipal user, IReadOnlyList<StockKeyDto>? keys, CancellationToken ct,
        Func<Stock, bool> canApply, Func<Stock, string?, InventoryHistory> apply)
    {
        var wanted = (keys ?? Array.Empty<StockKeyDto>())
            .Where(k => !string.IsNullOrWhiteSpace(k.ItemCode) && !string.IsNullOrWhiteSpace(k.SerialNo))
            .Select(k => (Item: k.ItemCode.Trim(), Serial: k.SerialNo.Trim()))
            .Distinct().ToList();
        if (wanted.Count == 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));

        var itemCodes = wanted.Select(w => w.Item).Distinct().ToList();
        var rows = await db.Stocks.Where(s => itemCodes.Contains(s.ItemCode)).ToListAsync(ct);

        var who = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var updated = 0;
        var skipped = new List<string>();

        foreach (var (item, serial) in wanted)
        {
            var row = rows.FirstOrDefault(s => s.ItemCode == item && s.SerialNo == serial);
            if (row is null || !canApply(row)) { skipped.Add($"{item}/{serial}"); continue; }

            db.InventoryHistories.Add(apply(row, who));
            Touch(row, who);
            updated++;
        }

        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<StockBulkResult>(new StockBulkResult(updated, skipped)));
    }

    private static async Task<StockDto> LoadDtoAsync(ImsDbContext db, Stock row, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var r = await Project(db, db.Stocks.AsNoTracking()
            .Where(s => s.ItemCode == row.ItemCode && s.SerialNo == row.SerialNo)).FirstAsync(ct);
        return Map(r, today);
    }

    private static string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
