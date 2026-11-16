using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Inventory;
using Ims.Domain.Production;

namespace Ims.Api.Features.Production;

internal sealed partial class ProductionService
{
    public async Task<ServiceResult> ReturnAsync(string orderNo, ReturnWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.RequestId)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldRequestId", "Request ID")));
        if (body.Qty <= 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty003()));
        var old = await db.ProductionReturns.Where(x => x.RequestId == body.RequestId && x.ReversesNo == null).ToListAsync(ct);
        if (old.Count > 0) return Outcome.Ok(new ApiSuccess<IReadOnlyList<ProductionResult>>(old));
        var order = await OrderAsync(orderNo, ct);
        if (order is null) return MissingOrder();
        if (order.StatusCode == 3) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts004()));
        var consumed = await Effective(db.ProductionConsumptions).Where(x => x.ProductionOrderNo == orderNo
            && x.ItemCode == body.ItemCode && x.LotNo == body.LotNo).OrderByDescending(x => x.No).ToListAsync(ct);
        var returned = await Effective(db.ProductionReturns).Where(x => x.ProductionOrderNo == orderNo
            && x.ItemCode == body.ItemCode && x.LotNo == body.LotNo).ToListAsync(ct);
        if (consumed.Sum(x => x.Qty) - returned.Sum(x => x.Qty) < body.Qty)
            return Outcome.Conflict(new ApiError(ErrorCatalog.Qty004()));
        var open = ProductionReturnRules.OpenBySource(consumed, returned);
        var worker = Worker(user);
        var bySerial = new Dictionary<string, ProductionResult>(StringComparer.Ordinal);
        var remaining = body.Qty;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var (source, available) in open)
        {
            if (remaining <= 0) break;
            var n = Math.Min(available, remaining);
            if (n <= 0) continue;
            remaining -= n;
            var stock = await db.Stocks.FirstOrDefaultAsync(x => x.ItemCode == source.ItemCode && x.SerialNo == source.SerialNo, ct);
            if (stock is null)
            {
                stock = new Stock { ItemCode = source.ItemCode, SerialNo = source.SerialNo,
                    FactoryCode = source.FactoryCode, LocationCode = source.LocationCode,
                    LotNo = source.LotNo, ExpiryDate = source.ExpiryDate, UpdatedAt = DateTime.Now };
                db.Stocks.Add(stock);
            }
            stock.StockQty += n;
            stock.Recalculate();
            stock.UpdatedAt = DateTime.Now;
            stock.UpdatedBy = worker;
            var key = source.SerialNo + "|" + source.LineNo;
            if (bySerial.TryGetValue(key, out var row)) row.Qty += n;
            else bySerial[key] = new ProductionResult
            {
                RequestId = body.RequestId, ProductionOrderNo = orderNo, ResultType = ProductionResultType.Return,
                LineNo = source.LineNo, ItemCode = source.ItemCode, LotNo = source.LotNo, SerialNo = source.SerialNo,
                Qty = n, LocationCode = source.LocationCode, FactoryCode = source.FactoryCode,
                ExpiryDate = source.ExpiryDate, CreatedAt = DateTime.Now, CreatedBy = worker,
            };
            db.InventoryHistories.Add(new InventoryHistory { ItemCode = source.ItemCode, SerialNo = source.SerialNo,
                FactoryCode = source.FactoryCode, LocationCode = source.LocationCode, LotNo = source.LotNo,
                Qty = n, ActionType = InventoryAction.ProdReturn, CreatedAt = DateTime.Now, CreatedBy = worker });
        }
        var rows = bySerial.Values.ToList();
        db.ProductionResults.AddRange(rows);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome.Created($"/production-order/{orderNo}", new ApiSuccess<IReadOnlyList<ProductionResult>>(rows));
    }
}
