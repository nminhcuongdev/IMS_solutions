using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Production;

internal sealed partial class ProductionService
{
    public async Task<ServiceResult> StatusAsync(StatusWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var target = body.Status switch { "done" => 2, "notDone" => 0, "cancelled" => 3, _ => -1 };
        if (target < 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel006()));
        if (body.OrderNos is not { Count: > 0 }) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));
        var orders = await db.ProductionOrders.Where(x => body.OrderNos.Contains(x.ProductionOrderNo)).ToListAsync(ct);
        if (orders.Select(x => x.StatusCode).Distinct().Count() > 1) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts003()));
        if (orders.Count > 0 && orders.All(x => x.StatusCode == 2) && target == 2)
            return Outcome.Conflict(new ApiError(ErrorCatalog.Sts002()));
        var skipped = new List<string>();
        foreach (var order in orders)
        {
            if (order.StatusCode == 3 && target == 2)
            {
                skipped.Add(order.ProductionOrderNo);
                continue;
            }
            if (target == 3 && (await db.ProductionConsumptions.AnyAsync(x => x.ProductionOrderNo == order.ProductionOrderNo && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No), ct)
                || await db.ProductionOutputs.AnyAsync(x => x.ProductionOrderNo == order.ProductionOrderNo && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No), ct)))
                return Outcome.Conflict(new ApiError(ErrorCatalog.Sts005()));
            order.StatusCode = target;
            order.UpdatedAt = DateTime.Now;
            order.UpdatedBy = Worker(user);
        }
        skipped.AddRange(body.OrderNos.Except(orders.Select(x => x.ProductionOrderNo)));
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<object>(new { Updated = orders.Count - skipped.Count(x => orders.Any(o => o.ProductionOrderNo == x)), Skipped = skipped }));
    }

    public async Task<ServiceResult> PickingAsync(PickingWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        if (body.Items is not { Count: > 0 }) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sel002()));
        var changed = 0;
        foreach (var selected in body.Items)
        {
            var order = await OrderAsync(selected.OrderNo, ct);
            if (order is null) return MissingOrder();
            if (order.StatusCode >= 2) return Outcome.Conflict(new ApiError(ErrorCatalog.Sts004()));
            var query = db.ProductionOrderLines.Where(x => x.ProductionOrderNo == selected.OrderNo);
            if (selected.LineNo.HasValue) query = query.Where(x => x.LineNo == selected.LineNo.Value);
            var lines = await query.ToListAsync(ct);
            foreach (var line in lines)
            {
                line.PickedAt = body.Picked ? DateTime.Now : null;
                line.PickedBy = body.Picked ? Worker(user) : null;
                changed++;
            }
        }
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<object>(new { Updated = changed }));
    }
}
