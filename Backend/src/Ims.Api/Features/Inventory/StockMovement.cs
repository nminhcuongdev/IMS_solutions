using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Inventory;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Inventory;

internal static class StockMovement
{
    internal static async Task<ServiceResult?> TakeAsync(
        ImsDbContext db, string itemCode, string? lotNo, string? locationCode, decimal qty,
        string action, string? reasonCode, string? worker, ApiErrorBody noStockError, CancellationToken ct,
        List<Stock>? taken = null)
    {
        if (qty <= 0) return null;

        var rows = await db.Stocks
            .Where(s => s.ItemCode == itemCode && s.StockQty > 0
                     && (lotNo == null || s.LotNo == lotNo)
                     && (locationCode == null || s.LocationCode == locationCode))
            .ToListAsync(ct);
        if (rows.Count == 0) return Outcome.NotFound(new ApiError(noStockError));

        var free = rows.Where(s => !s.IsOnHold)
            .OrderBy(s => s.ExpiryDate is null).ThenBy(s => s.ExpiryDate)
            .ThenBy(s => s.SerialNo, StringComparer.Ordinal).ToList();
        if (free.Sum(s => s.AvailableQty) < qty)
        {
            var coveredByHeld = rows.Sum(s => s.IsOnHold ? s.StockQty : s.AvailableQty) >= qty;
            return Outcome.Conflict(new ApiError(coveredByHeld ? ErrorCatalog.Hld001() : ErrorCatalog.Inv005()));
        }

        var now = DateTime.UtcNow;
        var remaining = qty;
        foreach (var s in free)
        {
            if (remaining <= 0) break;
            var n = Math.Min(s.AvailableQty, remaining);
            if (n <= 0) continue;
            remaining -= n;

            s.StockQty -= n;
            s.Recalculate();
            s.UpdatedAt = now;
            s.UpdatedBy = worker;
            taken?.Add(s);
            db.InventoryHistories.Add(new InventoryHistory
            {
                CreatedAt = DateTime.Now,
                ItemCode = s.ItemCode,
                FactoryCode = s.FactoryCode,
                LocationCode = s.LocationCode,
                LotNo = s.LotNo,
                ActionType = action,
                Qty = -n,
                ReasonCode = reasonCode,
                CreatedBy = worker,
            });
        }
        return null;
    }
}
