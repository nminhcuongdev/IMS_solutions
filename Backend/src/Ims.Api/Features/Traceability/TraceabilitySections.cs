using Microsoft.EntityFrameworkCore;
using Ims.Domain.Production;

namespace Ims.Api.Features.Traceability;

internal sealed partial class TraceabilityService
{
    private async Task<TraceResultDto> WithSectionsAsync(TraceResultDto result, string direction,
        IReadOnlyList<TraceNodeDto> raw, IReadOnlyList<ProductionResult> produced, CancellationToken ct)
    {
        var codes = raw.Select(n => n.ItemCode).Distinct().ToList();
        var items = await db.Items.AsNoTracking().Where(i => codes.Contains(i.ItemCode)).ToDictionaryAsync(i => i.ItemCode, ct);
        var processes = await db.Processes.AsNoTracking().ToDictionaryAsync(p => p.ProcessCode, p => p.ProcessName, ct);
        var partners = await db.Partners.AsNoTracking().ToDictionaryAsync(p => p.PartnerCode, p => p.PartnerName, ct);
        var orderNos = raw.Where(n => n.Type is "process" or "material" or "output").Select(n => n.DocumentNo)
            .Concat(produced.Select(p => p.ProductionOrderNo)).Where(n => n != null).Distinct().ToList();
        var orders = await db.ProductionOrders.AsNoTracking().Where(o => orderNos.Contains(o.ProductionOrderNo))
            .ToDictionaryAsync(o => o.ProductionOrderNo, ct);
        string? Name(string code) => items.TryGetValue(code, out var i) ? i.ItemName : null;
        string? Partner(string? code) => code is not null && partners.TryGetValue(code, out var n) ? n : null;

        var root = raw[0];
        var madeBy = produced.Select(p => p.ProductionOrderNo).FirstOrDefault();
        var lot = new TraceLotInfoDto(root.ItemCode, Name(root.ItemCode), root.LotNo,
            produced.Select(p => p.ExpiryDate).FirstOrDefault(d => d != null), madeBy);
        TraceProductionDto? production = null;
        if (madeBy is not null && orders.TryGetValue(madeBy, out var order))
        {
            var times = produced.Select(p => p.CreatedAt).ToList();
            production = new TraceProductionDto(order.ProductionOrderNo, order.ProductionLotNo, order.OutputLotNo,
                times.Min(), order.StatusCode == 2 ? times.Max() : null, produced.Sum(p => p.Qty), order.UomCode);
        }

        var receipts = raw.Where(n => n.Type == "receiving").Select(n => new TraceReceiptDto(n.ItemCode, n.LotNo,
            n.DocumentNo, n.PartnerCode, Partner(n.PartnerCode), n.OccurredAt, n.Qty, n.DocumentNo is null)).ToList();
        var shipments = raw.Where(n => n.Type == "shipping").Select(n => new TraceShipmentDto(n.ItemCode, n.LotNo,
            n.DocumentNo, n.PartnerCode, Partner(n.PartnerCode), n.Qty, n.OccurredAt, n.DocumentNo is null)).ToList();

        if (direction == "backward")
        {
            var components = raw.Where(n => n.Type is "material" or "unknown").Select(n => new TraceComponentDto(
                n.Level, n.ProcessCode, n.ProcessCode is { } pc ? processes.GetValueOrDefault(pc) : null, n.ItemCode, Name(n.ItemCode),
                n.LotNo, n.InputQty ?? 0, n.ReturnedQty ?? 0, n.Qty, items.GetValueOrDefault(n.ItemCode)?.UomCode,
                n.ExpiryDate, n.ExpiryCheck ?? (n.ExpiryDate is null ? "empty" : null), n.OccurredAt, n.DocumentNo,
                n.Type == "unknown" || n.LotNo is null)).ToList();
            return result with
            {
                Direction = direction, Lot = lot, Production = production, Components = components,
                Receipts = receipts, Shipments = shipments,
            };
        }

        var consumedBy = raw.Select((n, i) => (n, i)).Where(x => x.n.Type == "process" && x.n.Level == 1)
            .Select(x => new TraceConsumedByDto(x.n.DocumentNo,
                x.n.DocumentNo is { } no && orders.TryGetValue(no, out var o) ? o.ProductionLotNo : null,
                x.n.ProcessCode, x.n.InputQty ?? 0, x.n.ReturnedQty ?? 0, x.n.Qty,
                raw.Where(c => c.Type == "output" && c.ParentNo == x.i).Select(c => new TraceOutputRefDto(c.ItemCode, c.LotNo)).ToList()))
            .ToList();
        return result with
        {
            Direction = direction, Lot = lot, Production = production, ConsumedBy = consumedBy,
            Receipts = receipts, Shipments = shipments,
        };
    }
}
