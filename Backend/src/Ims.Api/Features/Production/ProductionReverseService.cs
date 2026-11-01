using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.Permissions;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Production;

namespace Ims.Api.Features.Production;

internal sealed partial class ProductionService
{
    private async Task<ServiceResult?> CheckReverseAsync(ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!(await new PermissionEvaluator(db).ReadAsync(user, ct)).Allows("prd_result", "reverse"))
            return new ServiceResult(403, new ApiError(ErrorCatalog.Auth006()));
        if (string.IsNullOrWhiteSpace(body.ReasonCode)) return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));
        if (!await db.Reasons.AnyAsync(x => x.ReasonCode == body.ReasonCode && !x.IsDeleted
            && x.ReasonCategoryValue == ReasonCategory.Adjustment, ct))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Rsn001()));
        return null;
    }

    public async Task<ServiceResult> ReverseConsumptionAsync(long no, ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (await CheckReverseAsync(body, user, ct) is { } error) return error;
        var source = await db.ProductionConsumptions.FirstOrDefaultAsync(x => x.No == no && x.ReversesNo == null, ct);
        if (source is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Consumption")));
        if (await db.ProductionConsumptions.AnyAsync(x => x.ReversesNo == no, ct)) return Outcome.Conflict(new ApiError(ErrorCatalog.Inv008()));
        var order = await OrderAsync(source.ProductionOrderNo, ct);
        if (order?.StatusCode == 3) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts004()));
        var returned = await db.ProductionReturns.Where(x => x.ConsumptionNo == no && x.ReversesNo == null
            && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        if (returned > 0) return Outcome.Conflict(new ApiError(ErrorCatalog.Inv005()));
        var worker = Worker(user);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var stock = await db.Stocks.FirstOrDefaultAsync(x => x.ItemCode == source.ItemCode && x.SerialNo == source.SerialNo, ct);
        if (stock is null)
        {
            stock = new Stock { ItemCode = source.ItemCode, SerialNo = source.SerialNo, FactoryCode = source.FactoryCode,
                LocationCode = source.LocationCode, LotNo = source.LotNo, ExpiryDate = source.ExpiryDate, UpdatedAt = DateTime.Now };
            db.Stocks.Add(stock);
        }
        stock.StockQty += source.Qty;
        stock.Recalculate();
        stock.UpdatedAt = DateTime.Now;
        stock.UpdatedBy = worker;
        var reverse = new ProductionConsumption { RequestId = $"REV-{no}", ProductionOrderNo = source.ProductionOrderNo,
            LineNo = source.LineNo, ItemCode = source.ItemCode, LotNo = source.LotNo, SerialNo = source.SerialNo,
            Qty = source.Qty, LocationCode = source.LocationCode, FactoryCode = source.FactoryCode,
            ExpiryDate = source.ExpiryDate, ReversesNo = no, ReasonCode = body.ReasonCode,
            CreatedAt = DateTime.Now, CreatedBy = worker };
        db.ProductionConsumptions.Add(reverse);
        db.InventoryHistories.Add(new InventoryHistory { ItemCode = source.ItemCode, SerialNo = source.SerialNo,
            FactoryCode = source.FactoryCode, LocationCode = source.LocationCode, LotNo = source.LotNo,
            Qty = source.Qty, ActionType = InventoryAction.Adjust, ReasonCode = body.ReasonCode,
            CreatedAt = DateTime.Now, CreatedBy = worker });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome.Ok(new ApiSuccess<ProductionConsumption>(reverse));
    }

    public async Task<ServiceResult> ReverseOutputAsync(long no, ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (await CheckReverseAsync(body, user, ct) is { } error) return error;
        var source = await db.ProductionOutputs.FirstOrDefaultAsync(x => x.No == no && x.ReversesNo == null, ct);
        if (source is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Output")));
        if (await db.ProductionOutputs.AnyAsync(x => x.ReversesNo == no, ct)) return Outcome.Conflict(new ApiError(ErrorCatalog.Inv008()));
        var order = await OrderAsync(source.ProductionOrderNo, ct);
        if (order?.StatusCode == 3) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts004()));
        if (await db.ShippingResultLots.AnyAsync(x => x.ItemCode == source.ItemCode && x.SerialNo == source.SerialNo, ct)
            || await db.PickingResultLots.AnyAsync(x => x.ItemCode == source.ItemCode && x.SerialNo == source.SerialNo, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Inv006()));
        if (await db.ProductionConsumptions.AnyAsync(x => x.ItemCode == source.ItemCode && x.SerialNo == source.SerialNo
            && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No), ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Inv005()));
        var stock = await db.Stocks.FirstOrDefaultAsync(x => x.ItemCode == source.ItemCode && x.SerialNo == source.SerialNo, ct);
        if (stock is null || stock.StockQty != source.Qty || stock.ReservedQty != 0)
            return Outcome.Conflict(new ApiError(ErrorCatalog.Inv005()));
        var worker = Worker(user);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        stock.StockQty -= source.Qty;
        stock.Recalculate();
        stock.UpdatedAt = DateTime.Now;
        stock.UpdatedBy = worker;
        var reverse = new ProductionOutput { RequestId = $"REV-{no}", ProductionOrderNo = source.ProductionOrderNo,
            ItemCode = source.ItemCode, LotNo = source.LotNo, SerialNo = source.SerialNo,
            Qty = source.Qty, LocationCode = source.LocationCode, ExpiryDate = source.ExpiryDate,
            ReversesNo = no, ReasonCode = body.ReasonCode, CreatedAt = DateTime.Now, CreatedBy = worker };
        db.ProductionOutputs.Add(reverse);
        db.InventoryHistories.Add(new InventoryHistory { ItemCode = source.ItemCode, SerialNo = source.SerialNo,
            FactoryCode = stock.FactoryCode, LocationCode = source.LocationCode, LotNo = source.LotNo,
            Qty = -source.Qty, ActionType = InventoryAction.Adjust, ReasonCode = body.ReasonCode,
            CreatedAt = DateTime.Now, CreatedBy = worker });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome.Ok(new ApiSuccess<ProductionOutput>(reverse));
    }

    public async Task<ServiceResult> ReverseReturnAsync(long no, ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (await CheckReverseAsync(body, user, ct) is { } error) return error;
        var source = await db.ProductionReturns.FirstOrDefaultAsync(x => x.No == no && x.ReversesNo == null, ct);
        if (source is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Production return")));
        if (await db.ProductionReturns.AnyAsync(x => x.ReversesNo == no, ct)) return Outcome.Conflict(new ApiError(ErrorCatalog.Inv008()));
        var order = await OrderAsync(source.ProductionOrderNo, ct);
        if (order?.StatusCode == 3) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts004()));
        var stock = await db.Stocks.FirstOrDefaultAsync(x => x.ItemCode == source.ItemCode && x.SerialNo == source.SerialNo, ct);
        if (stock is null || stock.AvailableQty < source.Qty || stock.ReservedQty != 0)
            return Outcome.Conflict(new ApiError(ErrorCatalog.Inv005()));
        var worker = Worker(user);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        stock.StockQty -= source.Qty;
        stock.Recalculate();
        stock.UpdatedAt = DateTime.Now;
        stock.UpdatedBy = worker;
        var reverse = new ProductionReturn { RequestId = $"REV-{no}", ProductionOrderNo = source.ProductionOrderNo,
            LineNo = source.LineNo, ConsumptionNo = source.ConsumptionNo, ItemCode = source.ItemCode,
            LotNo = source.LotNo, SerialNo = source.SerialNo, Qty = source.Qty,
            LocationCode = source.LocationCode, FactoryCode = source.FactoryCode,
            ExpiryDate = source.ExpiryDate, ReversesNo = no, ReasonCode = body.ReasonCode,
            CreatedAt = DateTime.Now, CreatedBy = worker };
        db.ProductionReturns.Add(reverse);
        db.InventoryHistories.Add(new InventoryHistory { ItemCode = source.ItemCode, SerialNo = source.SerialNo,
            FactoryCode = source.FactoryCode, LocationCode = source.LocationCode, LotNo = source.LotNo,
            Qty = -source.Qty, ActionType = InventoryAction.Adjust, ReasonCode = body.ReasonCode,
            CreatedAt = DateTime.Now, CreatedBy = worker });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome.Ok(new ApiSuccess<ProductionReturn>(reverse));
    }
}
