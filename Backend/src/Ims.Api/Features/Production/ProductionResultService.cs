using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Production;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Production;

internal sealed partial class ProductionService
{
    private static readonly SortMap<ProductionWorkDto> WorkSort = new SortMap<ProductionWorkDto>()
        .Add("type", x => x.Type)
        .Add("no", x => x.No)
        .Add("production_order_no", x => x.ProductionOrderNo)
        .Add("item_code", x => x.ItemCode)
        .Add("lot_no", x => x.LotNo)
        .Add("qty", x => x.Qty)
        .Add("created_at", x => x.CreatedAt)
        .Add("created_by", x => x.CreatedBy)
        .Add("is_reversed", x => x.IsReversed)
        .Default("created_at").ThenAlwaysBy(x => x.No);

    private static readonly SortMap<ProductionResultFgDto> FgSort = new SortMap<ProductionResultFgDto>()
        .Add("no", x => x.No).Add("request_id", x => x.RequestId)
        .Add("production_order_no", x => x.ProductionOrderNo).Add("item_code", x => x.ItemCode)
        .Add("lot_no", x => x.LotNo).Add("qty", x => x.Qty)
        .Add("serial_count", x => x.SerialCount).Add("created_at", x => x.CreatedAt)
        .Add("created_by", x => x.CreatedBy).Add("label_issued", x => x.LabelIssued)
        .Add("is_reversed", x => x.IsReversed)
        .Add("item_name", x => x.ItemName ?? "").Add("production_lot_no", x => x.ProductionLotNo ?? "")
        .Add("unit", x => x.Unit ?? "").Add("location_code", x => x.LocationCode ?? "")
        .Add("serial_no", x => x.SerialFrom ?? "").Add("worker", x => x.Worker ?? "")
        .Default("no").ThenAlwaysBy(x => x.No);

    public async Task<ServiceResult> ResultsAsync(CancellationToken ct, string? view = null, string? type = null,
        string? orderNo = null, string? itemCode = null, string? lotNo = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, int? page = null,
        int? pageSize = null, string? sort = null, string? itemName = null, bool showReversed = false)
    {
        var pg = NormalizePage(page);
        var sz = NormalizePageSize(pageSize);
        if (view == "work")
        {
            var rows = new List<ProductionWorkDto>();
            if (type is null or "all" or "cons" or "consumption")
                rows.AddRange(await db.ProductionConsumptions.AsNoTracking().Where(x => showReversed || x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No))
                    .Select(x => new ProductionWorkDto("consumption", x.No, x.ProductionOrderNo, x.ItemCode, x.LotNo, x.Qty, x.CreatedAt, x.CreatedBy,
                        x.ReversesNo != null || db.ProductionConsumptions.Any(r => r.ReversesNo == x.No))).ToListAsync(ct));
            if (type is null or "all" or "out" or "output")
                rows.AddRange(await db.ProductionOutputs.AsNoTracking().Where(x => showReversed || x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No))
                    .Select(x => new ProductionWorkDto("output", x.No, x.ProductionOrderNo, x.ItemCode, x.LotNo, x.Qty, x.CreatedAt, x.CreatedBy,
                        x.ReversesNo != null || db.ProductionOutputs.Any(r => r.ReversesNo == x.No))).ToListAsync(ct));
            if (type is null or "all" or "rcpt" or "receipt")
                rows.AddRange(await db.ProductionOrders.AsNoTracking().Where(x => x.ReceivedAt != null)
                    .Select(x => new ProductionWorkDto("receipt", x.No, x.ProductionOrderNo, null, null, null, x.ReceivedAt!.Value, x.ReceivedBy)).ToListAsync(ct));
            if (type is null or "all" or "label")
                rows.AddRange((await db.ProductionOutputs.AsNoTracking().Where(x => x.LabelIssuedAt != null && x.ReversesNo == null).ToListAsync(ct))
                    .GroupBy(x => x.RequestId)
                    .Select(g => new ProductionWorkDto("label", g.Min(x => x.No), g.First().ProductionOrderNo, g.First().ItemCode,
                        g.First().LotNo, g.Sum(x => x.Qty), g.Min(x => x.LabelIssuedAt!.Value), g.First().LabelIssuedBy)));
            if (type is null or "all" or "pick" or "picking")
                rows.AddRange(await db.ProductionOrderLines.AsNoTracking().Where(x => x.PickedAt != null)
                    .Select(x => new ProductionWorkDto("picking", x.No, x.ProductionOrderNo, x.ItemCode, null, x.PlannedQty, x.PickedAt!.Value, x.PickedBy)).ToListAsync(ct));
            if (type is null or "all" or "ret" or "return")
                rows.AddRange(await db.ProductionReturns.AsNoTracking().Where(x => showReversed || x.ReversesNo == null && !db.ProductionReturns.Any(r => r.ReversesNo == x.No))
                    .Select(x => new ProductionWorkDto("return", x.No, x.ProductionOrderNo, x.ItemCode, x.LotNo, x.Qty, x.CreatedAt, x.CreatedBy,
                        x.ReversesNo != null || db.ProductionReturns.Any(r => r.ReversesNo == x.No))).ToListAsync(ct));
            var itemNames = await db.Items.AsNoTracking().ToDictionaryAsync(x => x.ItemCode, x => x.ItemName, ct);
            var filtered = rows.Where(x => orderNo == null || x.ProductionOrderNo.Contains(orderNo, StringComparison.OrdinalIgnoreCase))
                .Where(x => itemCode == null || x.ItemCode?.Contains(itemCode, StringComparison.OrdinalIgnoreCase) == true)
                .Where(x => itemName == null || x.ItemCode is not null && itemNames.GetValueOrDefault(x.ItemCode)?.Contains(itemName, StringComparison.OrdinalIgnoreCase) == true)
                .Where(x => lotNo == null || x.LotNo?.Contains(lotNo, StringComparison.OrdinalIgnoreCase) == true)
                .Where(x => dateFrom == null || DateOnly.FromDateTime(x.CreatedAt) >= dateFrom)
                .Where(x => dateTo == null || DateOnly.FromDateTime(x.CreatedAt) <= dateTo).ToList();
            var sorted = WorkSort.Apply(filtered.AsQueryable(), sort).Skip((pg - 1) * sz).Take(sz).ToList();
            return Outcome.Ok(new ApiListSuccess<ProductionWorkDto>(sorted, new PageMeta(filtered.Count, pg, sz)));
        }
        var query = db.ProductionOutputs.AsNoTracking().Where(x => showReversed || x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No));
        if (!string.IsNullOrWhiteSpace(orderNo)) query = query.Where(x => EF.Functions.Like(x.ProductionOrderNo, LikeContains(orderNo.Trim()), LikeEscape));
        if (!string.IsNullOrWhiteSpace(itemCode)) query = query.Where(x => EF.Functions.Like(x.ItemCode, LikeContains(itemCode.Trim()), LikeEscape));
        if (!string.IsNullOrWhiteSpace(itemName)) query = query.Where(x => db.Items.Any(i => i.ItemCode == x.ItemCode && EF.Functions.Like(i.ItemName, LikeContains(itemName.Trim()), LikeEscape)));
        if (!string.IsNullOrWhiteSpace(lotNo)) query = query.Where(x => EF.Functions.Like(x.LotNo, LikeContains(lotNo.Trim()), LikeEscape));
        if (dateFrom.HasValue) query = query.Where(x => x.CreatedAt >= dateFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (dateTo.HasValue) query = query.Where(x => x.CreatedAt < dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var outputs = await query.ToListAsync(ct);
        var reversedNos = (await db.ProductionOutputs.AsNoTracking().Where(x => x.ReversesNo != null)
            .Select(x => x.ReversesNo!.Value).ToListAsync(ct)).ToHashSet();
        var labeled = outputs.Where(x => x.LabelIssuedAt != null).Select(x => x.RequestId).ToHashSet();
        var codes = outputs.Select(x => x.ItemCode).Distinct().ToList();
        var items = await db.Items.AsNoTracking().Where(x => codes.Contains(x.ItemCode)).ToDictionaryAsync(x => x.ItemCode, ct);
        var orderNos = outputs.Select(x => x.ProductionOrderNo).Distinct().ToList();
        var orderLots = await db.ProductionOrders.AsNoTracking().Where(x => orderNos.Contains(x.ProductionOrderNo))
            .ToDictionaryAsync(x => x.ProductionOrderNo, x => x.ProductionLotNo, ct);
        var userCodes = outputs.Select(x => x.CreatedBy).Distinct().ToList();
        var users = await db.AppUsers.AsNoTracking().Where(x => userCodes.Contains(x.UserCode))
            .ToDictionaryAsync(x => x.UserCode, x => x.UserName, ct);
        var grouped = outputs.GroupBy(x => x.RequestId).Select(g =>
        {
            var first = g.OrderBy(x => x.No).First();
            var last = g.OrderBy(x => x.No).Last();
            items.TryGetValue(first.ItemCode, out var item);
            return new ProductionResultFgDto(first.No, g.Key, first.ProductionOrderNo, first.ItemCode, first.LotNo,
                g.Sum(x => x.Qty), g.Count(), g.Min(x => x.CreatedAt), first.CreatedBy,
                labeled.Contains(g.Key), g.Any(x => x.ReversesNo != null || reversedNos.Contains(x.No)),
                item?.ItemName, orderLots.GetValueOrDefault(first.ProductionOrderNo), item?.UomCode,
                first.LocationCode, first.SerialNo, last.SerialNo,
                users.TryGetValue(first.CreatedBy, out var name) ? $"{name} ({first.CreatedBy})" : first.CreatedBy);
        }).ToList();
        var result = FgSort.Apply(grouped.AsQueryable(), sort).Skip((pg - 1) * sz).Take(sz).ToList();
        return Outcome.Ok(new ApiListSuccess<ProductionResultFgDto>(result, new PageMeta(grouped.Count, pg, sz)));
    }

    public async Task<ServiceResult> ExportOrdersAsync(CancellationToken ct)
    {
        var orders = await db.ProductionOrders.AsNoTracking().OrderBy(x => x.ProductionOrderNo).ToListAsync(ct);
        var lines = await db.ProductionOrderLines.AsNoTracking().ToListAsync(ct);
        var sb = new StringBuilder("Production Order No,Order Date,Factory Code,Product Code,Product Planned,Product Unit,Production Lot No,Finished Goods Lot No,Storage Location Code,Start Date,End Date,Line No,Process Code,Material Code,Material Planned,Material Unit,Production Scheduled Date\r\n");
        foreach (var order in orders)
        {
            var orderLines = lines.Where(x => x.ProductionOrderNo == order.ProductionOrderNo).ToList();
            if (orderLines.Count == 0) orderLines.Add(new ProductionOrderLine());
            foreach (var line in orderLines)
                sb.Append(Csv(order.ProductionOrderNo)).Append(',').Append(order.OrderDate).Append(',')
                  .Append(Csv(order.FactoryCode)).Append(',').Append(Csv(order.OutputItemCode)).Append(',')
                  .Append(Num(order.PlannedQty)).Append(',').Append(Csv(order.UomCode)).Append(',')
                  .Append(Csv(order.ProductionLotNo)).Append(',').Append(Csv(order.OutputLotNo)).Append(',')
                  .Append(Csv(order.StorageLocationCode)).Append(',').Append(order.PlannedStartDate).Append(',')
                  .Append(order.PlannedEndDate).Append(',').Append(line.LineNo == 0 ? "" : line.LineNo).Append(',')
                  .Append(Csv(line.ProcessCode)).Append(',').Append(Csv(line.ItemCode)).Append(',')
                  .Append(line.LineNo == 0 ? "" : Num(line.PlannedQty)).Append(',').Append(Csv(line.UomCode)).Append(',')
                  .Append(line.LineNo == 0 ? "" : line.ScheduledDate).AppendLine();
        }
        return CsvFile(sb, "production_order.csv");
    }

    public async Task<ServiceResult> ExportResultsAsync(CancellationToken ct)
    {
        var outputs = await db.ProductionOutputs.AsNoTracking().Where(x => x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var inputs = await db.ProductionConsumptions.AsNoTracking().Where(x => x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var sb = new StringBuilder("Type,No,Production Order No,Item Code,Lot No,Serial No,Quantity,Created At,Worker\r\n");
        foreach (var row in inputs)
            sb.Append("Consumption,").Append(row.No).Append(',').Append(Csv(row.ProductionOrderNo)).Append(',')
              .Append(Csv(row.ItemCode)).Append(',').Append(Csv(row.LotNo)).Append(',')
              .Append(Csv(row.SerialNo)).Append(',').Append(Num(row.Qty)).Append(',')
              .Append(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',').Append(Csv(row.CreatedBy)).AppendLine();
        foreach (var row in outputs)
            sb.Append("Output,").Append(row.No).Append(',').Append(Csv(row.ProductionOrderNo)).Append(',')
              .Append(Csv(row.ItemCode)).Append(',').Append(Csv(row.LotNo)).Append(',')
              .Append(Csv(row.SerialNo)).Append(',').Append(Num(row.Qty)).Append(',')
              .Append(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',').Append(Csv(row.CreatedBy)).AppendLine();
        return CsvFile(sb, "production_result.csv");
    }
}
