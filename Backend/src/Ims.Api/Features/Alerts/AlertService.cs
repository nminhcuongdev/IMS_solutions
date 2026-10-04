using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Picking;
using Ims.Domain.Receiving;
using Ims.Domain.Shipping;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Alerts;

internal sealed class AlertService(ImsDbContext db) : IAlertService
{
    private const int RowLimit = 50;

    public async Task<ServiceResult> ListAsync(CancellationToken ct)
    {
        var configs = await db.AlertConfigs.AsNoTracking()
            .Where(c => c.IsActive)
            .ToDictionaryAsync(c => c.AlertTypeCode, ct);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var blocks = new List<AlertBlockDto>();

        foreach (var code in AlertType.Ordered)
        {
            if (!configs.TryGetValue(code, out var cfg)) continue;

            var found = code switch
            {
                AlertType.ExpiryWarning => await ExpiryAsync(db, today, (int)(cfg.MaxThreshold ?? 7m), ct),
                AlertType.LowStock => await LowStockAsync(db, ct),
                AlertType.StockOverflow => await OverflowAsync(db, ct),
                AlertType.HoldReview => await HoldAsync(db, ct),
                AlertType.ReceivingDelay => await ReceivingDelayAsync(db, Deadline(today, cfg), ct),
                AlertType.PickingDelay => await PickingDelayAsync(db, Deadline(today, cfg), ct),
                AlertType.ShippingDelay => await ShippingDelayAsync(db, Deadline(today, cfg), ct),
                _ => (0, (IReadOnlyList<AlertRowDto>)Array.Empty<AlertRowDto>()),
            };

            blocks.Add(new AlertBlockDto(
                code, AlertType.LabelKey(code), AlertType.ExtraLabelKey(code), found.Item1, found.Item2));
        }

        return Outcome.Ok(new ApiSuccess<List<AlertBlockDto>>(blocks));
    }

    private static DateOnly Deadline(DateOnly today, AlertConfig cfg)
        => today.AddDays(-(int)(cfg.MaxThreshold ?? 0m));

    private static string Qty(decimal d) => d.ToString("0.####", CultureInfo.InvariantCulture);

    private static async Task<(int, IReadOnlyList<AlertRowDto>)> TakeAsync(
        IQueryable<AlertRowDto> q, CancellationToken ct)
        => (await q.CountAsync(ct), await q.Take(RowLimit).ToListAsync(ct));

    private static Task<(int, IReadOnlyList<AlertRowDto>)> ExpiryAsync(
        ImsDbContext db, DateOnly today, int days, CancellationToken ct)
    {
        var until = today.AddDays(days);
        var q = from s in db.Stocks.AsNoTracking()
                join i in db.Items.AsNoTracking() on s.ItemCode equals i.ItemCode
                where !i.IsDeleted && s.ExpiryDate != null
                      && s.ExpiryDate >= today && s.ExpiryDate <= until
                orderby s.ExpiryDate, s.ItemCode, s.SerialNo
                select new AlertRowDto(
                    s.ItemCode, i.ItemName, s.ExpiryDate!.Value.ToString("yyyy-MM-dd"));
        return TakeAsync(q, ct);
    }

    private sealed record QtyRow(string ItemCode, string ItemName, decimal Qty, decimal Threshold);

    private static Task<(int, IReadOnlyList<AlertRowDto>)> LowStockAsync(ImsDbContext db, CancellationToken ct)
    {
        var q = from s in db.Stocks.AsNoTracking()
                join i in db.Items.AsNoTracking() on s.ItemCode equals i.ItemCode
                where !i.IsDeleted && i.ReorderLevel1 != null && s.StockQty < i.ReorderLevel1
                orderby s.StockQty, s.ItemCode, s.SerialNo
                select new QtyRow(s.ItemCode, i.ItemName, s.StockQty, i.ReorderLevel1!.Value);
        return QtyRowsAsync(q, ct);
    }

    private static Task<(int, IReadOnlyList<AlertRowDto>)> OverflowAsync(ImsDbContext db, CancellationToken ct)
    {
        var q = from s in db.Stocks.AsNoTracking()
                join i in db.Items.AsNoTracking() on s.ItemCode equals i.ItemCode
                where !i.IsDeleted && i.SurplusPoint != null && s.StockQty >= i.SurplusPoint
                orderby s.StockQty descending, s.ItemCode, s.SerialNo
                select new QtyRow(s.ItemCode, i.ItemName, s.StockQty, i.SurplusPoint!.Value);
        return QtyRowsAsync(q, ct);
    }

    private static async Task<(int, IReadOnlyList<AlertRowDto>)> QtyRowsAsync(
        IQueryable<QtyRow> q, CancellationToken ct)
    {
        var total = await q.CountAsync(ct);
        var rows = await q.Take(RowLimit).ToListAsync(ct);
        return (total, rows
            .Select(r => new AlertRowDto(r.ItemCode, r.ItemName, $"{Qty(r.Qty)} / {Qty(r.Threshold)}"))
            .ToList());
    }

    private static Task<(int, IReadOnlyList<AlertRowDto>)> HoldAsync(ImsDbContext db, CancellationToken ct)
    {
        var q = from s in db.Stocks.AsNoTracking()
                join i in db.Items.AsNoTracking() on s.ItemCode equals i.ItemCode
                join r0 in db.Reasons.AsNoTracking() on s.ReasonCode equals r0.ReasonCode into rj
                from r in rj.DefaultIfEmpty()
                where !i.IsDeleted && s.IsOnHold
                orderby s.ItemCode, s.SerialNo
                select new AlertRowDto(
                    s.ItemCode, i.ItemName, r != null ? r.ReasonName : s.ReasonCode);
        return TakeAsync(q, ct);
    }

    private static Task<(int, IReadOnlyList<AlertRowDto>)> ReceivingDelayAsync(
        ImsDbContext db, DateOnly deadline, CancellationToken ct)
    {
        var q = from p in db.ReceivingPlans.AsNoTracking()
                join i in db.Items.AsNoTracking() on p.ItemCode equals i.ItemCode
                where p.ScheduledDate != null && p.ScheduledDate < deadline
                      && p.Status != ReceivingStatus.Arrived && p.Status != ReceivingStatus.Cancelled
                orderby p.ScheduledDate, p.PoNo, p.PoBranchNo
                select new AlertRowDto(p.ItemCode, i.ItemName, p.PoNo);
        return TakeAsync(q, ct);
    }

    private static Task<(int, IReadOnlyList<AlertRowDto>)> PickingDelayAsync(
        ImsDbContext db, DateOnly deadline, CancellationToken ct)
    {
        var q = from p in db.PickingPlans.AsNoTracking()
                join i in db.Items.AsNoTracking() on p.ItemCode equals i.ItemCode
                where p.ScheduledDate != null && p.ScheduledDate < deadline
                      && p.Status != PickingStatus.Picked && p.Status != PickingStatus.Cancelled
                orderby p.ScheduledDate, p.SoNo, p.SoBranchNo
                select new AlertRowDto(p.ItemCode, i.ItemName, p.SoNo);
        return TakeAsync(q, ct);
    }

    private static Task<(int, IReadOnlyList<AlertRowDto>)> ShippingDelayAsync(
        ImsDbContext db, DateOnly deadline, CancellationToken ct)
    {
        var q = from p in db.ShippingPlans.AsNoTracking()
                join i in db.Items.AsNoTracking() on p.ItemCode equals i.ItemCode
                where p.ScheduledDate != null && p.ScheduledDate < deadline
                      && p.Status != ShippingStatus.Shipped && p.Status != ShippingStatus.Cancelled
                orderby p.ScheduledDate, p.SoNo, p.SoBranchNo
                select new AlertRowDto(p.ItemCode, i.ItemName, p.SoNo);
        return TakeAsync(q, ct);
    }
}
