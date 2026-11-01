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
        List<Stock>? taken = null, List<StockAllocation>? allocations = null,
        ApiErrorBody? shortageError = null, bool refuseExpired = false, string? factoryCode = null)
    {
        if (qty <= 0) return null;

        var lotManaged = await db.Items.Where(i => i.ItemCode == itemCode).Select(i => i.IsLotManaged).FirstOrDefaultAsync(ct);
        if (lotManaged && string.IsNullOrWhiteSpace(lotNo))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Lot001()));
        var rows = await db.Stocks
            .Where(s => s.ItemCode == itemCode && s.StockQty > 0
                     && (factoryCode == null || s.FactoryCode == factoryCode)
                     && (lotNo == null || s.LotNo == lotNo || (!lotManaged && (s.LotNo == null || s.LotNo == "")))
                     && (locationCode == null || s.LocationCode == locationCode))
            .ToListAsync(ct);
        if (rows.Count == 0) return lotManaged && lotNo is not null
            ? Outcome.Conflict(new ApiError(ErrorCatalog.Scn006()))
            : Outcome.NotFound(new ApiError(noStockError));

        var free = rows.Where(s => !s.IsOnHold)
            .OrderBy(s => lotNo != null && s.LotNo != lotNo)
            .ThenBy(s => s.ExpiryDate is null).ThenBy(s => s.ExpiryDate)
            .ThenBy(s => s.SerialNo, StringComparer.Ordinal).ToList();
        if (free.Sum(s => s.AvailableQty) < qty)
        {
            var coveredByHeld = rows.Sum(s => s.IsOnHold ? s.StockQty : s.AvailableQty) >= qty;
            return Outcome.Conflict(new ApiError(coveredByHeld ? ErrorCatalog.Hld001() : shortageError ?? ErrorCatalog.Inv005()));
        }
        if (refuseExpired)
        {
            var toCheck = qty;
            foreach (var row in free)
            {
                var used = Math.Min(row.AvailableQty, toCheck);
                if (used <= 0) continue;
                if (row.ExpiryDate < DateOnly.FromDateTime(DateTime.Today))
                    return Outcome.Conflict(new ApiError(ErrorCatalog.Inv001()));
                toCheck -= used;
                if (toCheck <= 0) break;
            }
        }

        var now = DateTime.Now;
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
            allocations?.Add(new StockAllocation(s.SerialNo, s.LotNo, n, s.LocationCode, s.ExpiryDate, s.FactoryCode));
            db.InventoryHistories.Add(new InventoryHistory
            {
                CreatedAt = DateTime.Now,
                ItemCode = s.ItemCode,
                FactoryCode = s.FactoryCode,
                LocationCode = s.LocationCode,
                LotNo = s.LotNo,
                SerialNo = s.SerialNo,
                ActionType = action,
                Qty = -n,
                ReasonCode = reasonCode,
                CreatedBy = worker,
            });
        }
        return null;
    }
}
