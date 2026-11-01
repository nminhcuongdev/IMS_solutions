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
        if (string.IsNullOrWhiteSpace(body.RequestId)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));
        if (body.Qty <= 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty003()));
        var old = await db.ProductionReturns.Where(x => x.RequestId == body.RequestId && x.ReversesNo == null).ToListAsync(ct);
        if (old.Count > 0) return Outcome.Ok(new ApiSuccess<IReadOnlyList<ProductionReturn>>(old));
        var order = await OrderAsync(orderNo, ct);
        if (order is null) return MissingOrder();
        if (order.StatusCode == 3) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts004()));
        var consumed = await db.ProductionConsumptions.Where(x => x.ProductionOrderNo == orderNo
            && x.ItemCode == body.ItemCode && x.LotNo == body.LotNo && x.ReversesNo == null
            && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).OrderByDescending(x => x.No).ToListAsync(ct);
        var consumedNos = consumed.Select(x => x.No).ToList();
        var returned = await db.ProductionReturns.Where(x => consumedNos.Contains(x.ConsumptionNo) && x.ReversesNo == null
            && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        if (consumed.Sum(x => x.Qty) - returned.Sum(x => x.Qty) < body.Qty)
            return Outcome.Conflict(new ApiError(ErrorCatalog.Qty004()));
        var worker = Worker(user);
        var rows = new List<ProductionReturn>();
        var remaining = body.Qty;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var source in consumed)
        {
            if (remaining <= 0) break;
            var available = source.Qty - returned.Where(x => x.ConsumptionNo == source.No).Sum(x => x.Qty);
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
            rows.Add(new ProductionReturn { RequestId = body.RequestId, ProductionOrderNo = orderNo,
                LineNo = source.LineNo, ConsumptionNo = source.No, ItemCode = source.ItemCode,
                LotNo = source.LotNo, SerialNo = source.SerialNo, Qty = n,
                LocationCode = source.LocationCode, FactoryCode = source.FactoryCode,
                ExpiryDate = source.ExpiryDate, CreatedAt = DateTime.Now, CreatedBy = worker });
            db.InventoryHistories.Add(new InventoryHistory { ItemCode = source.ItemCode, SerialNo = source.SerialNo,
                FactoryCode = source.FactoryCode, LocationCode = source.LocationCode, LotNo = source.LotNo,
                Qty = n, ActionType = InventoryAction.ProdReturn, CreatedAt = DateTime.Now, CreatedBy = worker });
        }
        db.ProductionReturns.AddRange(rows);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome.Created($"/production-order/{orderNo}/return", new ApiSuccess<IReadOnlyList<ProductionReturn>>(rows));
    }
}
