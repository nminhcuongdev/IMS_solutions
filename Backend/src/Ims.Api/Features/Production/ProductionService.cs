using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Production;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Production;

internal sealed partial class ProductionService : IProductionService
{
    private readonly ImsDbContext db;

    private static readonly SortMap<ProductionOrderRowDto> OrderSort = new SortMap<ProductionOrderRowDto>()
        .Add("no", x => x.No).Add("production_order_no", x => x.ProductionOrderNo)
        .Add("order_date", x => x.OrderDate).Add("factory_code", x => x.FactoryCode)
        .Add("output_item_code", x => x.OutputItemCode).Add("output_item_name", x => x.OutputItemName)
        .Add("planned_qty", x => x.PlannedQty).Add("output_qty", x => x.OutputQty)
        .Add("uom_code", x => x.UomCode).Add("production_lot_no", x => x.ProductionLotNo)
        .Add("output_lot_no", x => x.OutputLotNo).Add("storage_location_code", x => x.StorageLocationCode)
        .Add("planned_start_date", x => x.PlannedStartDate).Add("planned_end_date", x => x.PlannedEndDate)
        .Add("started_at", x => x.StartedAt).Add("completed_at", x => x.CompletedAt)
        .Add("status_code", x => x.StatusCode).Default("production_order_no").ThenAlwaysBy(x => x.No);

    private static readonly SortMap<ProductionLineRowDto> LineSort = new SortMap<ProductionLineRowDto>()
        .Add("no", x => x.No).Add("production_order_no", x => x.ProductionOrderNo)
        .Add("output_item_code", x => x.OutputItemCode).Add("line_no", x => x.LineNo)
        .Add("scheduled_date", x => x.ScheduledDate).Add("process_code", x => x.ProcessCode)
        .Add("item_code", x => x.ItemCode).Add("item_name", x => x.ItemName)
        .Add("category", x => x.Category).Add("planned_qty", x => x.PlannedQty)
        .Add("consumed_qty", x => x.ConsumedQty).Add("uom_code", x => x.UomCode)
        .Add("production_lot_no", x => x.ProductionLotNo).Add("output_lot_no", x => x.OutputLotNo)
        .Add("storage_location_code", x => x.StorageLocationCode).Add("picked", x => x.Picked)
        .Add("received", x => x.Received).Add("ht_input_count", x => x.HtInputCount)
        .Add("ht_scan_required", x => x.HtScanRequired).Add("planned_start_date", x => x.PlannedStartDate)
        .Add("planned_end_date", x => x.PlannedEndDate).Add("started_at", x => x.StartedAt)
        .Add("completed_at", x => x.CompletedAt).Add("status_code", x => x.StatusCode)
        .Default("production_order_no").ThenAlwaysBy(x => x.No);

    public ProductionService(ImsDbContext db) => this.db = db;

    private static string Worker(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

    private static ServiceResult MissingOrder() => Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Production order")));

    private async Task<ProductionOrder?> OrderAsync(string orderNo, CancellationToken ct) =>
        await db.ProductionOrders.FirstOrDefaultAsync(x => x.ProductionOrderNo == orderNo, ct);

    public async Task<ServiceResult> ListAsync(CancellationToken ct, string? unit = null, string? orderNo = null,
        string? itemCode = null, string? itemName = null, string? productionLotNo = null,
        string? fgLotNo = null, int? status = null, DateOnly? startDateFrom = null,
        DateOnly? startDateTo = null, int? page = null, int? pageSize = null, string? sort = null)
    {
        var query = db.ProductionOrders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(orderNo)) query = query.Where(x => EF.Functions.Like(x.ProductionOrderNo, LikeContains(orderNo.Trim()), LikeEscape));
        if (!string.IsNullOrWhiteSpace(itemCode)) query = query.Where(x => EF.Functions.Like(x.OutputItemCode, LikeContains(itemCode.Trim()), LikeEscape));
        if (!string.IsNullOrWhiteSpace(itemName)) query = query.Where(x => db.Items.Any(i => i.ItemCode == x.OutputItemCode && EF.Functions.Like(i.ItemName, LikeContains(itemName.Trim()), LikeEscape)));
        if (!string.IsNullOrWhiteSpace(productionLotNo)) query = query.Where(x => x.ProductionLotNo != null && EF.Functions.Like(x.ProductionLotNo, LikeContains(productionLotNo.Trim()), LikeEscape));
        if (!string.IsNullOrWhiteSpace(fgLotNo)) query = query.Where(x => x.OutputLotNo != null && EF.Functions.Like(x.OutputLotNo, LikeContains(fgLotNo.Trim()), LikeEscape));
        if (status.HasValue) query = query.Where(x => x.StatusCode == status.Value);
        if (startDateFrom.HasValue) query = query.Where(x => x.PlannedStartDate >= startDateFrom.Value);
        if (startDateTo.HasValue) query = query.Where(x => x.PlannedStartDate <= startDateTo.Value);
        var pg = NormalizePage(page);
        var sz = NormalizePageSize(pageSize);
        var orders = await query.ToListAsync(ct);
        var selectedNos = orders.Select(x => x.ProductionOrderNo).ToList();
        var outputRows = await db.ProductionOutputs.AsNoTracking().Where(x => selectedNos.Contains(x.ProductionOrderNo)
            && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var inputRows = await db.ProductionConsumptions.AsNoTracking().Where(x => selectedNos.Contains(x.ProductionOrderNo)
            && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var returnRows = await db.ProductionReturns.AsNoTracking().Where(x => selectedNos.Contains(x.ProductionOrderNo)
            && x.ReversesNo == null && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var itemRows = await db.Items.AsNoTracking().Include(x => x.ItemGroup).ToDictionaryAsync(x => x.ItemCode, ct);
        var receipts = await db.ProductionReceipts.AsNoTracking().Where(x => selectedNos.Contains(x.ProductionOrderNo)).ToListAsync(ct);
        var starts = orders.ToDictionary(x => x.ProductionOrderNo, x =>
            inputRows.Where(y => y.ProductionOrderNo == x.ProductionOrderNo).Select(y => y.CreatedAt)
                .Concat(outputRows.Where(y => y.ProductionOrderNo == x.ProductionOrderNo).Select(y => y.CreatedAt))
                .Cast<DateTime?>().Min());
        var ends = orders.ToDictionary(x => x.ProductionOrderNo, x => x.StatusCode == 2
            ? inputRows.Where(y => y.ProductionOrderNo == x.ProductionOrderNo).Select(y => y.CreatedAt)
                .Concat(outputRows.Where(y => y.ProductionOrderNo == x.ProductionOrderNo).Select(y => y.CreatedAt))
                .Cast<DateTime?>().Max() : null);
        if (unit == "line")
        {
            var lines = await db.ProductionOrderLines.AsNoTracking().Where(x => selectedNos.Contains(x.ProductionOrderNo)).ToListAsync(ct);
            var processes = await db.Processes.AsNoTracking().ToDictionaryAsync(x => x.ProcessCode, ct);
            var mapped = lines.Select(l =>
            {
                var o = orders.First(x => x.ProductionOrderNo == l.ProductionOrderNo);
                itemRows.TryGetValue(l.ItemCode, out var item);
                return new ProductionLineRowDto(l.No, o.ProductionOrderNo, o.OutputItemCode, l.LineNo,
                    l.ScheduledDate, l.ProcessCode, l.ItemCode, item?.ItemName,
                    item?.ItemGroup?.Category.ToString(), l.PlannedQty,
                    inputRows.Where(x => x.ProductionOrderNo == l.ProductionOrderNo && x.LineNo == l.LineNo).Sum(x => x.Qty)
                        - returnRows.Where(x => x.ProductionOrderNo == l.ProductionOrderNo && x.LineNo == l.LineNo).Sum(x => x.Qty),
                    l.UomCode, o.ProductionLotNo, o.OutputLotNo, o.StorageLocationCode,
                    l.PickedAt is not null, receipts.Any(x => x.ProductionOrderNo == o.ProductionOrderNo),
                    inputRows.Where(x => x.ProductionOrderNo == o.ProductionOrderNo && x.LineNo == l.LineNo)
                        .Select(x => x.RequestId).Distinct().Count(),
                    item?.HtScanRequired ?? true, o.PlannedStartDate, o.PlannedEndDate,
                    starts[o.ProductionOrderNo], ends[o.ProductionOrderNo], o.StatusCode,
                    processes.GetValueOrDefault(l.ProcessCode)?.ProcessName,
                    processes.GetValueOrDefault(l.ProcessCode)?.IsActive ?? false);
            }).ToList();
            var rows = LineSort.Apply(mapped.AsQueryable(), sort).Skip((pg - 1) * sz).Take(sz).ToList();
            return Outcome.Ok(new ApiListSuccess<ProductionLineRowDto>(rows, new PageMeta(mapped.Count, pg, sz)));
        }
        var orderRows = orders.Select(o => new ProductionOrderRowDto(o.No, o.ProductionOrderNo, o.OrderDate,
            o.FactoryCode, o.OutputItemCode, itemRows.GetValueOrDefault(o.OutputItemCode)?.ItemName,
            o.PlannedQty, outputRows.Where(x => x.ProductionOrderNo == o.ProductionOrderNo).Sum(x => x.Qty),
            o.UomCode, o.ProductionLotNo, o.OutputLotNo, o.StorageLocationCode,
            o.PlannedStartDate, o.PlannedEndDate, starts[o.ProductionOrderNo], ends[o.ProductionOrderNo], o.StatusCode)).ToList();
        var sortedOrders = OrderSort.Apply(orderRows.AsQueryable(), sort).Skip((pg - 1) * sz).Take(sz).ToList();
        return Outcome.Ok(new ApiListSuccess<ProductionOrderRowDto>(sortedOrders, new PageMeta(orderRows.Count, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string orderNo, CancellationToken ct)
    {
        var order = await db.ProductionOrders.AsNoTracking().FirstOrDefaultAsync(x => x.ProductionOrderNo == orderNo, ct);
        if (order is null) return MissingOrder();
        var lines = await DetailedLinesAsync(order, ct);
        var inputTimes = await db.ProductionConsumptions.AsNoTracking().Where(x => x.ProductionOrderNo == orderNo && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).Select(x => x.CreatedAt).ToListAsync(ct);
        var outputTimes = await db.ProductionOutputs.AsNoTracking().Where(x => x.ProductionOrderNo == orderNo && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).Select(x => x.CreatedAt).ToListAsync(ct);
        var outputQty = await db.ProductionOutputs.Where(x => x.ProductionOrderNo == orderNo && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        var progress = order.PlannedQty > 0 ? outputQty / order.PlannedQty * 100 : 0;
        return Outcome.Ok(new ApiSuccess<object>(new { Order = order, Lines = lines, OutputQty = outputQty,
            Progress = progress, OverPlan = outputQty > order.PlannedQty,
            StartedAt = inputTimes.Concat(outputTimes).Cast<DateTime?>().Min(),
            CompletedAt = order.StatusCode == 2 ? inputTimes.Concat(outputTimes).Cast<DateTime?>().Max() : null,
            HtScanRequiredCount = lines.Count(x => x.HtScanRequired),
            PickedStatus = lines.Count == 0 ? "na" : lines.All(x => x.Picked) ? "all" : lines.Any(x => x.Picked) ? "part" : "none",
            ReceiptStatus = lines.Count == 0 ? "na" : lines.All(x => x.Received) ? "all" : "none",
            HtInputStatus = lines.All(x => !x.HtScanRequired) ? "na" : lines.Where(x => x.HtScanRequired).All(x => x.HtInputCount > 0) ? "all" : lines.Any(x => x.HtInputCount > 0) ? "part" : "none" }));
    }

    public async Task<ServiceResult> LinesAsync(string orderNo, CancellationToken ct)
    {
        if (!await db.ProductionOrders.AnyAsync(x => x.ProductionOrderNo == orderNo, ct)) return MissingOrder();
        var order = await OrderAsync(orderNo, ct);
        var lines = await DetailedLinesAsync(order!, ct);
        return Outcome.Ok(new ApiSuccess<IReadOnlyList<ProductionLineRowDto>>(lines));
    }

    private async Task<IReadOnlyList<ProductionLineRowDto>> DetailedLinesAsync(ProductionOrder order, CancellationToken ct)
    {
        var lines = await db.ProductionOrderLines.AsNoTracking().Where(x => x.ProductionOrderNo == order.ProductionOrderNo)
            .OrderBy(x => x.LineNo).ToListAsync(ct);
        var inputs = await db.ProductionConsumptions.AsNoTracking().Where(x => x.ProductionOrderNo == order.ProductionOrderNo && x.ReversesNo == null
            && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var returns = await db.ProductionReturns.AsNoTracking().Where(x => x.ProductionOrderNo == order.ProductionOrderNo && x.ReversesNo == null
            && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var received = await db.ProductionReceipts.AnyAsync(x => x.ProductionOrderNo == order.ProductionOrderNo, ct);
        var outputTimes = await db.ProductionOutputs.AsNoTracking().Where(x => x.ProductionOrderNo == order.ProductionOrderNo && x.ReversesNo == null
            && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).Select(x => x.CreatedAt).ToListAsync(ct);
        var times = inputs.Select(x => x.CreatedAt).Concat(outputTimes).ToList();
        var itemCodes = lines.Select(x => x.ItemCode).Distinct().ToList();
        var items = await db.Items.AsNoTracking().Include(x => x.ItemGroup)
            .Where(x => itemCodes.Contains(x.ItemCode)).ToDictionaryAsync(x => x.ItemCode, ct);
        var processes = await db.Processes.AsNoTracking().ToDictionaryAsync(x => x.ProcessCode, ct);
        return lines.Select(line =>
        {
            items.TryGetValue(line.ItemCode, out var item);
            return new ProductionLineRowDto(line.No, order.ProductionOrderNo, order.OutputItemCode,
                line.LineNo, line.ScheduledDate, line.ProcessCode, line.ItemCode, item?.ItemName,
                item?.ItemGroup?.Category.ToString(), line.PlannedQty,
                inputs.Where(x => x.LineNo == line.LineNo).Sum(x => x.Qty) - returns.Where(x => x.LineNo == line.LineNo).Sum(x => x.Qty),
                line.UomCode, order.ProductionLotNo, order.OutputLotNo, order.StorageLocationCode,
                line.PickedAt is not null, received,
                inputs.Where(x => x.LineNo == line.LineNo).Select(x => x.RequestId).Distinct().Count(),
                item?.HtScanRequired ?? true, order.PlannedStartDate, order.PlannedEndDate,
                times.Cast<DateTime?>().Min(), order.StatusCode == 2 ? times.Cast<DateTime?>().Max() : null,
                order.StatusCode, processes.GetValueOrDefault(line.ProcessCode)?.ProcessName,
                processes.GetValueOrDefault(line.ProcessCode)?.IsActive ?? false);
        }).ToList();
    }

    public async Task<ServiceResult> WorksAsync(string orderNo, CancellationToken ct)
    {
        if (!await db.ProductionOrders.AnyAsync(x => x.ProductionOrderNo == orderNo, ct)) return MissingOrder();
        var receipts = await db.ProductionReceipts.AsNoTracking().Where(x => x.ProductionOrderNo == orderNo).ToListAsync(ct);
        var consumptions = await db.ProductionConsumptions.AsNoTracking().Where(x => x.ProductionOrderNo == orderNo).ToListAsync(ct);
        var outputs = await db.ProductionOutputs.AsNoTracking().Where(x => x.ProductionOrderNo == orderNo).ToListAsync(ct);
        var labels = await db.ProductionLabels.AsNoTracking().Where(x => x.ProductionOrderNo == orderNo).ToListAsync(ct);
        var returns = await db.ProductionReturns.AsNoTracking().Where(x => x.ProductionOrderNo == orderNo).ToListAsync(ct);
        var picked = await db.ProductionOrderLines.AsNoTracking().Where(x => x.ProductionOrderNo == orderNo && x.PickedAt != null).ToListAsync(ct);
        var timeline = picked.Select(x => new ProductionWorkDto("picking", x.No, orderNo, x.ItemCode, null,
                x.PlannedQty, x.PickedAt!.Value, x.PickedBy))
            .Concat(receipts.Select(x => new ProductionWorkDto("receipt", x.No, orderNo, null, null, null, x.CreatedAt, x.CreatedBy)))
            .Concat(consumptions.Select(x => new ProductionWorkDto("consumption", x.No, orderNo, x.ItemCode, x.LotNo, x.Qty, x.CreatedAt, x.CreatedBy)))
            .Concat(outputs.Select(x => new ProductionWorkDto("output", x.No, orderNo, x.ItemCode, x.LotNo, x.Qty, x.CreatedAt, x.CreatedBy)))
            .Concat(labels.Select(x => new ProductionWorkDto("label", x.No, orderNo, x.ItemCode, x.LotNo, x.TotalQty, x.CreatedAt, x.CreatedBy)))
            .Concat(returns.Select(x => new ProductionWorkDto("return", x.No, orderNo, x.ItemCode, x.LotNo, x.Qty, x.CreatedAt, x.CreatedBy)))
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.No).ToList();
        return Outcome.Ok(new ApiSuccess<object>(new { Receipts = receipts, Consumptions = consumptions, Outputs = outputs,
            Labels = labels, Returns = returns, Timeline = timeline }));
    }
}
