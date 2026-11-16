using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Inventory;
using Ims.Domain.Inventory;
using Ims.Domain.Production;

namespace Ims.Api.Features.Production;

internal sealed partial class ProductionService
{
    public async Task<ServiceResult> ReceiptAsync(string orderNo, ReceiptWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var order = await OrderAsync(orderNo, ct);
        if (order is null) return MissingOrder();
        if (order.StatusCode >= 2) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts004()));
        order.ReceivedAt = DateTime.Now;
        order.ReceivedBy = Worker(user);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/production-order/{orderNo}",
            new ApiSuccess<ProductionReceiptDto>(new ProductionReceiptDto(order.ProductionOrderNo, order.ReceivedAt, order.ReceivedBy)));
    }

    public async Task<ServiceResult> ConsumptionAsync(string orderNo, ConsumptionWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.RequestId)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldRequestId", "Request ID")));
        if (body.Qty <= 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty003()));
        var old = await db.ProductionConsumptions.Where(x => x.RequestId == body.RequestId && x.ReversesNo == null).ToListAsync(ct);
        if (old.Count > 0) return Outcome.Ok(new ApiSuccess<IReadOnlyList<ProductionResult>>(old));
        var order = await OrderAsync(orderNo, ct);
        if (order is null) return MissingOrder();
        if (order.StatusCode >= 2) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts004()));
        var lines = await db.ProductionOrderLines.Where(x => x.ProductionOrderNo == orderNo && x.ProcessCode == body.ProcessCode && x.ItemCode == body.ItemCode).OrderBy(x => x.LineNo).ToListAsync(ct);
        if (body.LineNo is { } lineNo) lines = lines.Where(x => x.LineNo == lineNo).ToList();
        if (lines.Count == 0) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Production line")));
        if (!await db.Items.AnyAsync(x => x.ItemCode == body.ItemCode && x.HtScanRequired && x.IsActive && !x.IsDeleted, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Scn010()));
        var line = lines[0];
        if (body.LineNo is null && lines.Count > 1)
        {
            var used = await Effective(db.ProductionConsumptions).Where(x => x.ProductionOrderNo == orderNo && x.ItemCode == body.ItemCode)
                .GroupBy(x => x.LineNo).Select(x => new { LineNo = x.Key, Qty = x.Sum(y => y.Qty) }).ToListAsync(ct);
            var returns = await Effective(db.ProductionReturns).Where(x => x.ProductionOrderNo == orderNo && x.ItemCode == body.ItemCode)
                .GroupBy(x => x.LineNo).Select(x => new { LineNo = x.Key, Qty = x.Sum(y => y.Qty) }).ToListAsync(ct);
            line = lines.FirstOrDefault(x => used.Where(u => u.LineNo == x.LineNo).Select(u => u.Qty).FirstOrDefault()
                - returns.Where(r => r.LineNo == x.LineNo).Select(r => r.Qty).FirstOrDefault() < x.PlannedQty) ?? lines[^1];
        }
        if (!await db.Processes.AnyAsync(x => x.ProcessCode == line.ProcessCode && x.IsActive && !x.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Process")));
        var worker = Worker(user);
        var allocations = new List<StockAllocation>();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var takeError = await StockMovement.TakeAsync(db, body.ItemCode, body.LotNo, null, body.Qty,
            InventoryAction.Consume, null, worker, ErrorCatalog.Inv005(), ct,
            allocations: allocations, shortageError: ErrorCatalog.Inv005(), refuseExpired: true,
            factoryCode: order.FactoryCode);
        if (takeError is not null)
        {
            var code = (takeError.Body as ApiError)?.Error.Code;
            return takeError.StatusCode == 404 || code == "SCN-005"
                ? Outcome.Conflict(new ApiError(ErrorCatalog.Inv005())) : takeError;
        }
        var rows = allocations
            .GroupBy(x => x.SerialNo)
            .Select(g => new ProductionResult
            {
                RequestId = body.RequestId, ProductionOrderNo = orderNo, ResultType = ProductionResultType.Consume,
                LineNo = line.LineNo, ItemCode = body.ItemCode, LotNo = g.First().LotNo, SerialNo = g.Key,
                Qty = g.Sum(x => x.Qty), LocationCode = g.First().LocationCode!, FactoryCode = g.First().FactoryCode,
                ExpiryDate = g.First().ExpiryDate, CreatedAt = DateTime.Now, CreatedBy = worker,
            }).ToList();
        db.ProductionResults.AddRange(rows);
        order.StatusCode = 1;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome.Created($"/production-order/{orderNo}", new ApiSuccess<IReadOnlyList<ProductionResult>>(rows));
    }

    public async Task<ServiceResult> OutputAsync(string orderNo, OutputWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.RequestId)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldRequestId", "Request ID")));
        if (body.Qty <= 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty003()));
        var old = await db.ProductionOutputs.Where(x => x.RequestId == body.RequestId && x.ReversesNo == null).OrderBy(x => x.No).ToListAsync(ct);
        if (old.Count > 0) return Outcome.Ok(new ApiSuccess<ProductionOutputResultDto>(await OutputResultAsync(old, ct)));
        var order = await OrderAsync(orderNo, ct);
        if (order is null) return MissingOrder();
        if (order.StatusCode >= 2) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts004()));
        var item = await db.Items.FirstOrDefaultAsync(x => x.ItemCode == order.OutputItemCode && !x.IsDeleted && x.IsActive, ct);
        if (item is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Item")));
        var lot = order.OutputLotNo ?? body.LotNo?.Trim();
        if (string.IsNullOrWhiteSpace(lot))
        {
            var prefix = DateTime.Today.ToString("yyMMdd");
            var existing = await db.ProductionOrders.Where(x => x.OutputItemCode == order.OutputItemCode && x.OutputLotNo != null && x.OutputLotNo.StartsWith(prefix)).Select(x => x.OutputLotNo).ToListAsync(ct);
            var seq = Enumerable.Range(1, 999).First(x => !existing.Contains($"{prefix}-{x:000}"));
            lot = $"{prefix}-{seq:000}";
        }
        if (FieldHelpers.ValidateCode(lot, 20) is { } lotError) return lotError;
        if (order.OutputLotNo is null && (await db.Stocks.AnyAsync(x => x.ItemCode == item.ItemCode && x.LotNo == lot, ct)
            || await db.ProductionOrders.AnyAsync(x => x.OutputItemCode == item.ItemCode && x.OutputLotNo == lot && x.ProductionOrderNo != orderNo, ct)))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup003()));
        var pack = item.PackageQty;
        var qtys = new List<decimal>();
        if (pack <= 1) qtys.Add(body.Qty);
        else
        {
            var remainder = body.Qty;
            while (remainder > 0)
            {
                var n = Math.Min(pack, remainder);
                qtys.Add(n);
                remainder -= n;
            }
        }
        var worker = Worker(user);
        if (body.ExpiryDate is { } typed && typed < DateOnly.FromDateTime(DateTime.Today))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Inv001()));
        var expiry = body.ExpiryDate ?? (item.ExpirationPeriod is { } days ? DateOnly.FromDateTime(DateTime.Today.AddDays(days)) : null);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        order.OutputLotNo = lot;
        var rows = qtys.Select(q => new ProductionResult
        {
            RequestId = body.RequestId, ProductionOrderNo = orderNo, ResultType = ProductionResultType.Output,
            ItemCode = order.OutputItemCode, LotNo = lot, SerialNo = Guid.NewGuid().ToString("N")[..30], Qty = q,
            LocationCode = order.StorageLocationCode, FactoryCode = order.FactoryCode, ExpiryDate = expiry,
            CreatedAt = DateTime.Now, CreatedBy = worker,
        }).ToList();
        db.ProductionResults.AddRange(rows);
        await db.SaveChangesAsync(ct);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            row.SerialNo = $"PRD-{rows[0].No}-{i + 1}";
            db.Stocks.Add(new Stock { ItemCode = row.ItemCode, SerialNo = row.SerialNo,
                FactoryCode = order.FactoryCode, LocationCode = row.LocationCode, LotNo = lot,
                StockQty = row.Qty, AvailableQty = row.Qty, ExpiryDate = expiry, UpdatedAt = DateTime.Now, UpdatedBy = worker });
            db.InventoryHistories.Add(new InventoryHistory { ItemCode = row.ItemCode, SerialNo = row.SerialNo,
                FactoryCode = order.FactoryCode, LocationCode = row.LocationCode, LotNo = lot,
                Qty = row.Qty, ActionType = InventoryAction.Produce, CreatedAt = DateTime.Now, CreatedBy = worker });
        }
        order.StatusCode = body.Complete ? 2 : 1;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome.Created($"/production-order/{orderNo}", new ApiSuccess<ProductionOutputResultDto>(await OutputResultAsync(rows, ct)));
    }

    private async Task<ProductionOutputResultDto> OutputResultAsync(List<ProductionResult> rows, CancellationToken ct)
    {
        var first = rows[0];
        var qty = rows.Sum(x => x.Qty);
        var order = await db.ProductionOrders.FirstAsync(x => x.ProductionOrderNo == first.ProductionOrderNo, ct);
        var std = await db.Items.Where(x => x.ItemCode == first.ItemCode).Select(x => x.PackageQty).FirstAsync(ct);
        var total = await Effective(db.ProductionOutputs).Where(x => x.ProductionOrderNo == first.ProductionOrderNo).SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        return new ProductionOutputResultDto(first.ProductionOrderNo, first.ItemCode, first.LotNo ?? "", qty,
            rows.Select(x => x.SerialNo).ToList(), total > order.PlannedQty, rows.Count,
            std > 0 ? std : 1, std <= 1 ? qty : qty % std);
    }

    public async Task<ServiceResult> LabelAsync(string orderNo, LabelWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.OutputRequestId))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldOutputRequestId", "Output Request ID")));
        var outputs = await db.ProductionOutputs.Where(x => x.RequestId == body.OutputRequestId && x.ProductionOrderNo == orderNo && x.ReversesNo == null)
            .OrderBy(x => x.No).ToListAsync(ct);
        if (outputs.Count == 0) return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Production output")));
        var outputNos = outputs.Select(x => x.No).ToArray();
        if (await db.ProductionResults.AnyAsync(x => x.ReversesNo != null && outputNos.Contains(x.ReversesNo.Value), ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.Sys404("Production output")));
        if (outputs.Any(x => x.LabelIssuedAt != null)) return Outcome.Conflict(new ApiError(ErrorCatalog.Dup002()));
        var std = await db.Items.Where(x => x.ItemCode == outputs[0].ItemCode).Select(x => x.PackageQty).FirstAsync(ct);
        var qty = outputs.Sum(x => x.Qty);
        var now = DateTime.Now;
        var worker = Worker(user);
        foreach (var o in outputs) { o.LabelIssuedAt = now; o.LabelIssuedBy = worker; }
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/production-order/{orderNo}", new ApiSuccess<ProductionLabelResultDto>(new ProductionLabelResultDto(
            orderNo, outputs[0].ItemCode, outputs[0].LotNo, qty, std > 0 ? std : 1, outputs.Count,
            std <= 1 ? qty : qty % std, outputs.Select(x => new ProductionLabelDto(x.SerialNo, x.Qty)).ToList())));
    }

    private IQueryable<ProductionResult> Effective(IQueryable<ProductionResult> rows) =>
        rows.Where(x => x.ReversesNo == null && !db.ProductionResults.Any(r => r.ReversesNo == x.No));
}
