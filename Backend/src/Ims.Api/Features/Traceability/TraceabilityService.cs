using System.Text;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Traceability;

internal sealed class TraceabilityService(ImsDbContext db) : ITraceabilityService
{
    public async Task<ServiceResult> LotsAsync(TraceQueryDto query, CancellationToken ct)
    {
        if (query.Direction is not ("backward" or "forward"))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));
        var items = await db.Items.AsNoTracking().ToDictionaryAsync(i => i.ItemCode, i => i.ItemName, ct);
        var received = await db.ReceivingResults.AsNoTracking().Where(r => r.LotNo != null).ToListAsync(ct);
        var produced = await db.ProductionOutputs.AsNoTracking().Where(x => x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var consumed = await db.ProductionConsumptions.AsNoTracking().Where(x => x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var orders = await db.ProductionOrders.AsNoTracking().ToListAsync(ct);
        var partners = await db.Partners.AsNoTracking().ToDictionaryAsync(x => x.PartnerCode, x => x.PartnerName, ct);
        var shipments = await LoadShipmentsAsync(ct);
        var rows = new List<TraceLotDto>();
        if (query.Direction == "forward")
        {
            rows.AddRange(consumed.Where(x => query.ItemCode == null || x.ItemCode.Contains(query.ItemCode, StringComparison.OrdinalIgnoreCase))
                .Where(x => (query.MaterialLotNo ?? query.LotNo) == null || x.LotNo?.Contains(query.MaterialLotNo ?? query.LotNo!, StringComparison.OrdinalIgnoreCase) == true)
                .Where(x => query.OrderNo == null || x.ProductionOrderNo.Contains(query.OrderNo))
                .Where(x => query.ProductionLotNo == null || orders.Any(o => o.ProductionOrderNo == x.ProductionOrderNo && o.ProductionLotNo?.Contains(query.ProductionLotNo, StringComparison.OrdinalIgnoreCase) == true))
                .GroupBy(x => new { x.ItemCode, x.LotNo, x.ProductionOrderNo })
                .Select(g => new TraceLotDto(g.Key.ItemCode, g.Key.LotNo, items.GetValueOrDefault(g.Key.ItemCode),
                    g.Sum(x => x.Qty), null, null, null, null, null, g.Key.ProductionOrderNo,
                    orders.FirstOrDefault(o => o.ProductionOrderNo == g.Key.ProductionOrderNo)?.ProductionLotNo,
                    g.Min(x => x.CreatedAt))));
            rows.AddRange(received.Where(r => r.LotNo == (query.MaterialLotNo ?? query.LotNo) || query.MaterialLotNo == null && query.LotNo == null)
                .Where(r => query.ItemCode == null || r.ItemCode == query.ItemCode)
                .Where(r => query.PoNo == null || r.PoNo == query.PoNo)
                .Where(r => query.OrderNo == null || consumed.Any(c => c.ItemCode == r.ItemCode && c.LotNo == r.LotNo && c.ProductionOrderNo.Contains(query.OrderNo, StringComparison.OrdinalIgnoreCase)))
                .Where(r => query.ProductionLotNo == null || consumed.Any(c => c.ItemCode == r.ItemCode && c.LotNo == r.LotNo
                    && orders.Any(o => o.ProductionOrderNo == c.ProductionOrderNo && o.ProductionLotNo?.Contains(query.ProductionLotNo, StringComparison.OrdinalIgnoreCase) == true)))
                .Where(r => query.ProductionDateFrom == null || consumed.Any(c => c.ItemCode == r.ItemCode && c.LotNo == r.LotNo && DateOnly.FromDateTime(c.CreatedAt) >= query.ProductionDateFrom))
                .Where(r => query.ProductionDateTo == null || consumed.Any(c => c.ItemCode == r.ItemCode && c.LotNo == r.LotNo && DateOnly.FromDateTime(c.CreatedAt) <= query.ProductionDateTo))
                .Where(r => query.ReceivedFrom == null || DateOnly.FromDateTime(r.CreatedAt) >= query.ReceivedFrom)
                .Where(r => query.ReceivedTo == null || DateOnly.FromDateTime(r.CreatedAt) <= query.ReceivedTo)
                .GroupBy(r => new { r.ItemCode, r.LotNo, r.PoNo })
                .Select(g => new TraceLotDto(g.Key.ItemCode, g.Key.LotNo,
                    items.GetValueOrDefault(g.Key.ItemCode), g.Sum(x => x.Qty), g.Key.PoNo,
                    g.Min(x => x.CreatedAt), null, null, null)));
        }
        else
        {
            if (query.SoNo == null && query.Customer == null && query.ShippedFrom == null && query.ShippedTo == null)
                rows.AddRange(produced.Where(x => query.ItemCode == null || x.ItemCode == query.ItemCode)
                    .Where(x => query.FgLotNo == null || x.LotNo.Contains(query.FgLotNo, StringComparison.OrdinalIgnoreCase))
                    .Where(x => query.OrderNo == null || x.ProductionOrderNo.Contains(query.OrderNo))
                    .Where(x => query.ProductionLotNo == null || orders.Any(o => o.ProductionOrderNo == x.ProductionOrderNo && o.ProductionLotNo?.Contains(query.ProductionLotNo, StringComparison.OrdinalIgnoreCase) == true))
                    .Where(x => query.ProductionDateFrom == null || DateOnly.FromDateTime(x.CreatedAt) >= query.ProductionDateFrom)
                    .Where(x => query.ProductionDateTo == null || DateOnly.FromDateTime(x.CreatedAt) <= query.ProductionDateTo)
                    .GroupBy(x => new { x.ItemCode, x.LotNo, x.RequestId, x.ProductionOrderNo })
                    .Select(g => new TraceLotDto(g.Key.ItemCode, g.Key.LotNo, items.GetValueOrDefault(g.Key.ItemCode),
                        g.Sum(x => x.Qty), null, null, null, null, null, g.Key.ProductionOrderNo,
                        orders.FirstOrDefault(o => o.ProductionOrderNo == g.Key.ProductionOrderNo)?.ProductionLotNo,
                        g.Min(x => x.CreatedAt))));
            var filtered = shipments.Where(s => query.ItemCode == null || s.ItemCode.Contains(query.ItemCode, StringComparison.OrdinalIgnoreCase))
                .Where(s => query.FgLotNo == null || s.LotNo?.Contains(query.FgLotNo, StringComparison.OrdinalIgnoreCase) == true)
                .Where(s => query.SoNo == null || s.SoNo?.Contains(query.SoNo, StringComparison.OrdinalIgnoreCase) == true)
                .Where(s => query.Customer == null || s.Customer?.Contains(query.Customer, StringComparison.OrdinalIgnoreCase) == true
                    || s.Customer is not null && partners.GetValueOrDefault(s.Customer)?.Contains(query.Customer, StringComparison.OrdinalIgnoreCase) == true)
                .Where(s => query.OrderNo == null || produced.Any(o => o.ItemCode == s.ItemCode && o.LotNo == s.LotNo && o.ProductionOrderNo.Contains(query.OrderNo, StringComparison.OrdinalIgnoreCase)))
                .Where(s => query.ProductionLotNo == null || produced.Any(p => p.ItemCode == s.ItemCode && p.LotNo == s.LotNo
                    && orders.Any(o => o.ProductionOrderNo == p.ProductionOrderNo && o.ProductionLotNo?.Contains(query.ProductionLotNo, StringComparison.OrdinalIgnoreCase) == true)))
                .Where(s => query.ShippedFrom == null || DateOnly.FromDateTime(s.CreatedAt) >= query.ShippedFrom)
                .Where(s => query.ShippedTo == null || DateOnly.FromDateTime(s.CreatedAt) <= query.ShippedTo);
            rows.AddRange(filtered.GroupBy(s => new { s.ItemCode, s.LotNo, s.SoNo, s.Customer, Date = s.CreatedAt.Date })
                .Select(g => new TraceLotDto(g.Key.ItemCode, g.Key.LotNo,
                    items.GetValueOrDefault(g.Key.ItemCode), g.Sum(x => x.Qty), null, null,
                    g.Key.SoNo, g.Key.Customer, g.Min(x => x.CreatedAt))));
            if (query.SoNo == null && query.Customer == null && query.ShippedFrom == null && query.ShippedTo == null)
                rows.AddRange(received.Where(r => query.OrderNo == null && query.ProductionLotNo == null
                        && query.ProductionDateFrom == null && query.ProductionDateTo == null)
                    .Where(r => query.ItemCode == null || r.ItemCode == query.ItemCode)
                    .Where(r => query.FgLotNo == null || r.LotNo == query.FgLotNo)
                    .GroupBy(r => new { r.ItemCode, r.LotNo })
                    .Where(g => rows.All(s => s.ItemCode != g.Key.ItemCode || s.LotNo != g.Key.LotNo))
                    .Select(g => new TraceLotDto(g.Key.ItemCode, g.Key.LotNo,
                        items.GetValueOrDefault(g.Key.ItemCode), g.Sum(x => x.Qty), null, null,
                        null, null, null)));
        }
        if (query.SoNo == null && query.Customer == null && query.ShippedFrom == null
            && query.ShippedTo == null && query.PoNo == null && query.ReceivedFrom == null
            && query.ReceivedTo == null && query.OrderNo == null && query.ProductionLotNo == null
            && query.ProductionDateFrom == null && query.ProductionDateTo == null)
        {
            var stocks = await db.Stocks.AsNoTracking().Where(s => s.LotNo != null && s.StockQty > 0).ToListAsync(ct);
            rows.AddRange(stocks.Where(s => query.ItemCode == null || s.ItemCode == query.ItemCode)
                .Where(s => query.Direction == "backward"
                    ? query.FgLotNo == null || s.LotNo == query.FgLotNo
                    : (query.MaterialLotNo ?? query.LotNo) == null || s.LotNo == (query.MaterialLotNo ?? query.LotNo))
                .GroupBy(s => new { s.ItemCode, s.LotNo })
                .Where(g => rows.All(r => r.ItemCode != g.Key.ItemCode || r.LotNo != g.Key.LotNo))
                .Select(g => new TraceLotDto(g.Key.ItemCode, g.Key.LotNo,
                    items.GetValueOrDefault(g.Key.ItemCode), g.Sum(s => s.StockQty), null, null,
                    null, null, null)));
        }
        if (query.Direction == "forward")
            rows = rows.GroupBy(r => (r.ItemCode, r.LotNo))
            .Select(g => g.Aggregate((a, b) => a with
            {
                PoNo = a.PoNo ?? b.PoNo,
                ReceivedAt = a.ReceivedAt ?? b.ReceivedAt,
                OrderNo = a.OrderNo ?? b.OrderNo,
                ProductionLotNo = a.ProductionLotNo ?? b.ProductionLotNo,
                ProductionDate = a.ProductionDate ?? b.ProductionDate,
                Qty = Math.Max(a.Qty, b.Qty),
            })).ToList();
        var ordered = OrderLots(rows, query.Sort);
        var page = Math.Max(1, query.Page ?? 1);
        var size = Math.Clamp(query.PageSize ?? 20, 1, 200);
        return Outcome.Ok(new ApiListSuccess<TraceLotDto>(ordered.Skip((page - 1) * size).Take(size).ToList(),
            new PageMeta(rows.Count, page, size)));
    }

    private static IOrderedEnumerable<TraceLotDto> OrderLots(IEnumerable<TraceLotDto> rows, string? sort)
    {
        var descending = sort?.StartsWith('-') == true;
        var key = descending ? sort![1..] : sort;
        IOrderedEnumerable<TraceLotDto> ordered = key switch
        {
            "lot_no" => descending ? rows.OrderByDescending(r => r.LotNo) : rows.OrderBy(r => r.LotNo),
            "item_name" => descending ? rows.OrderByDescending(r => r.ItemName) : rows.OrderBy(r => r.ItemName),
            "qty" => descending ? rows.OrderByDescending(r => r.Qty) : rows.OrderBy(r => r.Qty),
            "po_no" => descending ? rows.OrderByDescending(r => r.PoNo) : rows.OrderBy(r => r.PoNo),
            "received_at" => descending ? rows.OrderByDescending(r => r.ReceivedAt) : rows.OrderBy(r => r.ReceivedAt),
            "so_no" => descending ? rows.OrderByDescending(r => r.SoNo) : rows.OrderBy(r => r.SoNo),
            "customer" => descending ? rows.OrderByDescending(r => r.Customer) : rows.OrderBy(r => r.Customer),
            "shipped_at" => descending ? rows.OrderByDescending(r => r.ShippedAt) : rows.OrderBy(r => r.ShippedAt),
            _ => descending ? rows.OrderByDescending(r => r.ItemCode) : rows.OrderBy(r => r.ItemCode),
        };
        return ordered.ThenBy(r => r.ItemCode).ThenBy(r => r.LotNo).ThenBy(r => r.PoNo).ThenBy(r => r.SoNo);
    }

    public Task<ServiceResult> BackwardAsync(string itemCode, string lotNo, CancellationToken ct) =>
        TraceAsync(itemCode, lotNo, "backward", ct);

    public Task<ServiceResult> ForwardAsync(string itemCode, string lotNo, CancellationToken ct) =>
        TraceAsync(itemCode, lotNo, "forward", ct);

    private async Task<ServiceResult> TraceAsync(string itemCode, string lotNo, string direction, CancellationToken ct)
    {
        var received = await db.ReceivingResults.AsNoTracking()
            .Where(r => r.ItemCode == itemCode && r.LotNo == lotNo).OrderBy(r => r.CreatedAt).ToListAsync(ct);
        var shipments = (await LoadShipmentsAsync(ct)).Where(s => s.ItemCode == itemCode && s.LotNo == lotNo).ToList();
        var stock = await db.Stocks.AsNoTracking().Where(s => s.ItemCode == itemCode && s.LotNo == lotNo).ToListAsync(ct);
        var produced = await db.ProductionOutputs.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var consumed = await db.ProductionConsumptions.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        if (received.Count == 0 && shipments.Count == 0 && stock.Count == 0 && produced.Count == 0 && consumed.Count == 0)
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Lot")));
        var plans = await db.ReceivingPlans.AsNoTracking().Where(p => p.ItemCode == itemCode).ToListAsync(ct);
        var nodes = new List<TraceNodeDto> { new(0, null, "lot", itemCode, lotNo, stock.Sum(s => s.StockQty), null, null, null) };
        if (direction == "backward")
        {
            var orders = produced.Select(x => x.ProductionOrderNo).Distinct().ToList();
            var inputs = await db.ProductionConsumptions.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo) && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
            var returns = await db.ProductionReturns.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo) && x.ReversesNo == null && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
            var lines = await db.ProductionOrderLines.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo)).ToListAsync(ct);
            var noScanItems = (await db.Items.AsNoTracking().Where(x => !x.HtScanRequired).Select(x => x.ItemCode).ToListAsync(ct)).ToHashSet();
            foreach (var line in lines.Where(x => noScanItems.Contains(x.ItemCode) && !inputs.Any(c => c.ProductionOrderNo == x.ProductionOrderNo && c.LineNo == x.LineNo)))
            {
                var processIndex = nodes.Count;
                nodes.Add(new TraceNodeDto(1, 0, "process", itemCode, lotNo, 0, line.ProductionOrderNo, null, null, line.ProcessCode));
                nodes.Add(new TraceNodeDto(2, processIndex, "unknown", line.ItemCode, null, 0,
                    line.ProductionOrderNo, null, null, line.ProcessCode));
            }
            foreach (var group in inputs.GroupBy(x => new { x.ProductionOrderNo, x.LineNo, x.ItemCode, x.LotNo }))
            {
                var process = lines.FirstOrDefault(x => x.ProductionOrderNo == group.Key.ProductionOrderNo && x.LineNo == group.Key.LineNo)?.ProcessCode;
                var processIndex = nodes.Count;
                nodes.Add(new TraceNodeDto(1, 0, "process", itemCode, lotNo, 0, group.Key.ProductionOrderNo, null, null, process));
                var inputQty = group.Sum(x => x.Qty);
                var returnedQty = returns.Where(x => group.Select(c => c.No).Contains(x.ConsumptionNo)).Sum(x => x.Qty);
                var expiry = group.Min(x => x.ExpiryDate);
                var check = group.Any(x => x.ExpiryDate < DateOnly.FromDateTime(x.CreatedAt)) ? "expired"
                    : group.Any(x => x.ExpiryDate <= DateOnly.FromDateTime(x.CreatedAt.AddDays(30))) ? "near" : "ok";
                var materialIndex = nodes.Count;
                nodes.Add(new TraceNodeDto(2, processIndex, "material", group.Key.ItemCode, group.Key.LotNo,
                    inputQty - returnedQty, group.Key.ProductionOrderNo, null, group.Min(x => x.CreatedAt),
                    process, inputQty, returnedQty, expiry, check));
                if (group.Key.LotNo is not null)
                    await AppendBackwardProductionAsync(nodes, group.Key.ItemCode, group.Key.LotNo, materialIndex, 3,
                        new HashSet<string> { $"{itemCode}|{lotNo}" }, ct);
            }
            nodes.AddRange(received.Select(r => new TraceNodeDto(1, 0, "receiving", itemCode, lotNo,
                r.Qty, r.PoNo, plans.FirstOrDefault(p => p.PoNo == r.PoNo && p.PoBranchNo == r.PoBranchNo)?.PartnerCode,
                r.CreatedAt)));
            nodes.AddRange(shipments.Select(s => new TraceNodeDto(1, 0, "shipping", itemCode, lotNo,
                s.Qty, s.SoNo, s.Customer, s.CreatedAt)));
        }
        else
        {
            var orders = consumed.Select(x => x.ProductionOrderNo).Distinct().ToList();
            var outputs = await db.ProductionOutputs.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo) && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
            var returns = await db.ProductionReturns.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo) && x.ReversesNo == null && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
            var lines = await db.ProductionOrderLines.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo)).ToListAsync(ct);
            foreach (var group in consumed.GroupBy(x => new { x.ProductionOrderNo, x.LineNo }))
            {
                var process = lines.FirstOrDefault(x => x.ProductionOrderNo == group.Key.ProductionOrderNo && x.LineNo == group.Key.LineNo)?.ProcessCode;
                var processIndex = nodes.Count;
                var returnedQty = returns.Where(x => group.Select(c => c.No).Contains(x.ConsumptionNo)).Sum(x => x.Qty);
                nodes.Add(new TraceNodeDto(1, 0, "process", itemCode, lotNo, group.Sum(x => x.Qty) - returnedQty,
                    group.Key.ProductionOrderNo, null, group.Min(x => x.CreatedAt), process,
                    group.Sum(x => x.Qty), returnedQty));
                foreach (var output in outputs.Where(x => x.ProductionOrderNo == group.Key.ProductionOrderNo)
                    .GroupBy(x => new { x.ItemCode, x.LotNo }))
                {
                    var outputIndex = nodes.Count;
                    nodes.Add(new TraceNodeDto(2, processIndex, "output", output.Key.ItemCode, output.Key.LotNo,
                        output.Sum(x => x.Qty), group.Key.ProductionOrderNo, null, output.Min(x => x.CreatedAt)));
                    await AppendForwardProductionAsync(nodes, output.Key.ItemCode, output.Key.LotNo, outputIndex, 3,
                        new HashSet<string> { $"{itemCode}|{lotNo}" }, ct);
                }
            }
            nodes.AddRange(shipments.Select(s => new TraceNodeDto(1, 0, "shipping", itemCode, lotNo,
                s.Qty, s.SoNo, s.Customer, s.CreatedAt)));
            if (received.Count > 0)
                nodes[0] = nodes[0] with
                {
                    DocumentNo = received[0].PoNo,
                    PartnerCode = plans.FirstOrDefault(p => p.PoNo == received[0].PoNo && p.PoBranchNo == received[0].PoBranchNo)?.PartnerCode,
                    OccurredAt = received[0].CreatedAt,
                };
        }
        nodes = await DecorateAsync(nodes, ct);
        var balance = await CalculateBalanceAsync(itemCode, lotNo, received, shipments, ct);
        var multiple = nodes.Where(n => n.Type == "receipt" && n.PartnerCode != null)
            .Select(n => n.PartnerCode).Distinct().Take(2).Count() > 1;
        var balances = new List<TraceBalanceDto> { balance };
        var linked = nodes.Where(x => x.LotNo != null && x.Type is "lot" or "lot-made")
            .Select(x => new { x.ItemCode, x.LotNo }).Distinct().Where(x => x.ItemCode != itemCode || x.LotNo != lotNo);
        foreach (var pair in linked)
        {
            var linkedReceived = await db.ReceivingResults.AsNoTracking().Where(x => x.ItemCode == pair.ItemCode && x.LotNo == pair.LotNo).ToListAsync(ct);
            var linkedShipped = (await LoadShipmentsAsync(ct)).Where(x => x.ItemCode == pair.ItemCode && x.LotNo == pair.LotNo).ToList();
            balances.Add(await CalculateBalanceAsync(pair.ItemCode, pair.LotNo!, linkedReceived, linkedShipped, ct));
        }
        return Outcome.Ok(new ApiSuccess<TraceResultDto>(new TraceResultDto(itemCode, lotNo, nodes, multiple, balance, balances)));
    }

    private async Task<List<TraceNodeDto>> DecorateAsync(List<TraceNodeDto> nodes, CancellationToken ct)
    {
        var itemCodes = nodes.Select(n => n.ItemCode).Distinct().ToList();
        var items = await db.Items.AsNoTracking().Where(x => itemCodes.Contains(x.ItemCode))
            .ToDictionaryAsync(x => x.ItemCode, ct);
        var processes = await db.Processes.AsNoTracking().ToDictionaryAsync(x => x.ProcessCode, x => x.ProcessName, ct);
        var partners = await db.Partners.AsNoTracking().ToDictionaryAsync(x => x.PartnerCode, x => x.PartnerName, ct);
        var lotItems = nodes.Where(n => n.LotNo != null).Select(n => n.ItemCode).Distinct().ToList();
        var outputs = await db.ProductionOutputs.AsNoTracking()
            .Where(x => lotItems.Contains(x.ItemCode) && x.ReversesNo == null)
            .Select(x => new { x.ItemCode, x.LotNo, x.ProductionOrderNo }).ToListAsync(ct);
        var orderNos = nodes.Where(n => n.DocumentNo != null).Select(n => n.DocumentNo!)
            .Concat(outputs.Select(x => x.ProductionOrderNo)).Distinct().ToList();
        var orders = await db.ProductionOrders.AsNoTracking().Where(x => orderNos.Contains(x.ProductionOrderNo))
            .ToDictionaryAsync(x => x.ProductionOrderNo, ct);
        string? MadeBy(string item, string? lot) =>
            outputs.FirstOrDefault(x => x.ItemCode == item && x.LotNo == lot)?.ProductionOrderNo;
        var remap = new Dictionary<int, int>();
        var keep = new List<int>();
        var processKeys = new Dictionary<(long?, string?, string?), int>();
        for (var i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            var parent = n.ParentNo is { } p ? remap[(int)p] : (int?)null;
            if (n.Type == "process" && processKeys.TryGetValue((parent, n.DocumentNo, n.ProcessCode), out var same))
            {
                remap[i] = same;
                continue;
            }
            remap[i] = keep.Count;
            if (n.Type == "process") processKeys[(parent, n.DocumentNo, n.ProcessCode)] = keep.Count;
            keep.Add(i);
        }
        var result = new List<TraceNodeDto>(keep.Count);
        foreach (var i in keep)
        {
            var n = nodes[i];
            items.TryGetValue(n.ItemCode, out var item);
            var madeBy = n.LotNo is null ? null : MadeBy(n.ItemCode, n.LotNo);
            var type = n.Type switch
            {
                "receiving" => "receipt",
                "material" or "output" or "lot" => madeBy is not null ? "lot-made" : "lot",
                _ => n.Type,
            };
            var orderNo = type == "lot-made" ? madeBy : type == "process" ? n.DocumentNo : null;
            var order = orderNo is not null && orders.TryGetValue(orderNo, out var o) ? o : null;
            var isProcess = type == "process";
            result.Add(n with
            {
                Type = type,
                ParentNo = n.ParentNo is { } p ? remap[(int)p] : null,
                ItemCode = isProcess ? "" : n.ItemCode,
                LotNo = isProcess ? null : n.LotNo,
                ItemName = isProcess ? null : item?.ItemName,
                Unit = item?.UomCode,
                ProcessName = n.ProcessCode is { } pc ? processes.GetValueOrDefault(pc) ?? pc : null,
                ProductionLotNo = order?.ProductionLotNo,
                ExpiryState = n.ExpiryCheck,
                PartnerName = n.PartnerCode is { } pt ? partners.GetValueOrDefault(pt) : null,
            });
        }
        return result;
    }

    private async Task AppendBackwardProductionAsync(List<TraceNodeDto> nodes, string itemCode, string lotNo,
        int parent, int depth, HashSet<string> seen, CancellationToken ct)
    {
        var key = $"{itemCode}|{lotNo}";
        if (depth > 10 || !seen.Add(key)) return;
        var outputs = await db.ProductionOutputs.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo
            && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var orders = outputs.Select(x => x.ProductionOrderNo).Distinct().ToList();
        var inputs = await db.ProductionConsumptions.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo)
            && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var returns = await db.ProductionReturns.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo)
            && x.ReversesNo == null && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var lines = await db.ProductionOrderLines.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo)).ToListAsync(ct);
        foreach (var group in inputs.GroupBy(x => new { x.ProductionOrderNo, x.LineNo, x.ItemCode, x.LotNo }))
        {
            var process = lines.FirstOrDefault(x => x.ProductionOrderNo == group.Key.ProductionOrderNo && x.LineNo == group.Key.LineNo)?.ProcessCode;
            var processIndex = nodes.Count;
            nodes.Add(new TraceNodeDto(depth, parent, "process", itemCode, lotNo, 0, group.Key.ProductionOrderNo, null, null, process));
            var inputQty = group.Sum(x => x.Qty);
            var returnedQty = returns.Where(x => group.Select(c => c.No).Contains(x.ConsumptionNo)).Sum(x => x.Qty);
            var materialIndex = nodes.Count;
            nodes.Add(new TraceNodeDto(depth + 1, processIndex, "material", group.Key.ItemCode, group.Key.LotNo,
                inputQty - returnedQty, group.Key.ProductionOrderNo, null, group.Min(x => x.CreatedAt), process,
                inputQty, returnedQty, group.Min(x => x.ExpiryDate)));
            if (group.Key.LotNo is not null)
                await AppendBackwardProductionAsync(nodes, group.Key.ItemCode, group.Key.LotNo, materialIndex, depth + 2, seen, ct);
        }
        if (orders.Count == 0)
        {
            var receipts = await db.ReceivingResults.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo).ToListAsync(ct);
            var plans = await db.ReceivingPlans.AsNoTracking().Where(p => p.ItemCode == itemCode).ToListAsync(ct);
            nodes.AddRange(receipts.Select(x => new TraceNodeDto(depth, parent, "receiving", itemCode, lotNo,
                x.Qty, x.PoNo, plans.FirstOrDefault(p => p.PoNo == x.PoNo && p.PoBranchNo == x.PoBranchNo)?.PartnerCode,
                x.CreatedAt)));
        }
        seen.Remove(key);
    }

    private async Task AppendForwardProductionAsync(List<TraceNodeDto> nodes, string itemCode, string lotNo,
        int parent, int depth, HashSet<string> seen, CancellationToken ct)
    {
        var key = $"{itemCode}|{lotNo}";
        if (depth > 10 || !seen.Add(key)) return;
        var inputs = await db.ProductionConsumptions.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo
            && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var orders = inputs.Select(x => x.ProductionOrderNo).Distinct().ToList();
        var returns = await db.ProductionReturns.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo)
            && x.ReversesNo == null && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var outputs = await db.ProductionOutputs.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo)
            && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var lines = await db.ProductionOrderLines.AsNoTracking().Where(x => orders.Contains(x.ProductionOrderNo)).ToListAsync(ct);
        foreach (var group in inputs.GroupBy(x => new { x.ProductionOrderNo, x.LineNo }))
        {
            var process = lines.FirstOrDefault(x => x.ProductionOrderNo == group.Key.ProductionOrderNo && x.LineNo == group.Key.LineNo)?.ProcessCode;
            var returnedQty = returns.Where(x => group.Select(c => c.No).Contains(x.ConsumptionNo)).Sum(x => x.Qty);
            var processIndex = nodes.Count;
            nodes.Add(new TraceNodeDto(depth, parent, "process", itemCode, lotNo, group.Sum(x => x.Qty) - returnedQty,
                group.Key.ProductionOrderNo, null, group.Min(x => x.CreatedAt), process, group.Sum(x => x.Qty), returnedQty));
            foreach (var output in outputs.Where(x => x.ProductionOrderNo == group.Key.ProductionOrderNo)
                .GroupBy(x => new { x.ItemCode, x.LotNo }))
            {
                var outputIndex = nodes.Count;
                nodes.Add(new TraceNodeDto(depth + 1, processIndex, "output", output.Key.ItemCode, output.Key.LotNo,
                    output.Sum(x => x.Qty), group.Key.ProductionOrderNo, null, output.Min(x => x.CreatedAt)));
                await AppendForwardProductionAsync(nodes, output.Key.ItemCode, output.Key.LotNo, outputIndex, depth + 2, seen, ct);
            }
        }
        var shipments = (await LoadShipmentsAsync(ct)).Where(x => x.ItemCode == itemCode && x.LotNo == lotNo);
        nodes.AddRange(shipments.Select(x => new TraceNodeDto(depth, parent, "shipping", itemCode, lotNo,
            x.Qty, x.SoNo, x.Customer, x.CreatedAt)));
        seen.Remove(key);
    }

    public async Task<ServiceResult> EventsAsync(string itemCode, string lotNo, CancellationToken ct)
    {
        var receiving = await db.ReceivingResults.AsNoTracking().Where(r => r.ItemCode == itemCode && r.LotNo == lotNo).ToListAsync(ct);
        var returns = await db.ReturnResults.AsNoTracking().Where(r => r.ItemCode == itemCode && r.LotNo == lotNo).ToListAsync(ct);
        var history = await db.InventoryHistories.AsNoTracking().Where(h => h.ItemCode == itemCode && h.LotNo == lotNo
            && h.ActionType != "Receiving" && h.ActionType != "Shipping" && h.ActionType != "Returns"
            && h.ActionType != "Consume" && h.ActionType != "Produce" && h.ActionType != "ProdReturn")
            .ToListAsync(ct);
        var inputs = await db.ProductionConsumptions.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var outputs = await db.ProductionOutputs.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var materialReturns = await db.ProductionReturns.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo == null && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).ToListAsync(ct);
        var shipments = (await LoadShipmentsAsync(ct)).Where(s => s.ItemCode == itemCode && s.LotNo == lotNo);
        var events = receiving.Select(r => new TraceEventDto("receiving", r.Qty, r.PoNo, null, r.CreatedAt, null))
            .Concat(shipments.Select(s => new TraceEventDto("shipping", -s.Qty, s.SoNo, s.Customer, s.CreatedAt, s.SerialNo)))
            .Concat(returns.Select(r => new TraceEventDto("return", r.Qty, r.ReturnNo, r.PartnerCode, r.CreatedAt, null)))
            .Concat(inputs.Select(x => new TraceEventDto("consumption", -x.Qty, x.ProductionOrderNo, null, x.CreatedAt, x.SerialNo)))
            .Concat(outputs.Select(x => new TraceEventDto("output", x.Qty, x.ProductionOrderNo, null, x.CreatedAt, x.SerialNo)))
            .Concat(materialReturns.Select(x => new TraceEventDto("production_return", x.Qty, x.ProductionOrderNo, null, x.CreatedAt, x.SerialNo)))
            .Concat(history.Select(h => new TraceEventDto(h.ActionType, h.Qty ?? 0, null, null, h.CreatedAt, h.SerialNo)))
            .OrderBy(e => e.OccurredAt).ToList();
        return Outcome.Ok(new ApiListSuccess<TraceEventDto>(events, new PageMeta(events.Count, 1, events.Count)));
    }

    public async Task<ServiceResult> BalanceAsync(string itemCode, string lotNo, CancellationToken ct)
    {
        var received = await db.ReceivingResults.AsNoTracking().Where(r => r.ItemCode == itemCode && r.LotNo == lotNo).ToListAsync(ct);
        var shipments = (await LoadShipmentsAsync(ct)).Where(s => s.ItemCode == itemCode && s.LotNo == lotNo).ToList();
        var balance = await CalculateBalanceAsync(itemCode, lotNo, received, shipments, ct);
        return Outcome.Ok(new ApiSuccess<TraceBalanceDto>(balance));
    }

    private async Task<TraceBalanceDto> CalculateBalanceAsync(string itemCode, string lotNo,
        IReadOnlyList<Ims.Domain.Receiving.ReceivingResult> received, IReadOnlyList<TraceShipment> shipments,
        CancellationToken ct)
    {
        var returned = await db.ReturnResults.AsNoTracking().Where(r => r.ItemCode == itemCode && r.LotNo == lotNo)
            .SumAsync(r => r.Qty, ct);
        var stock = await db.Stocks.AsNoTracking().Where(s => s.ItemCode == itemCode && s.LotNo == lotNo)
            .SumAsync(s => s.StockQty, ct);
        var adjustments = await db.InventoryHistories.AsNoTracking()
            .Where(h => h.ItemCode == itemCode && h.LotNo == lotNo && h.ActionType == "Adjust")
            .SumAsync(h => h.Qty, ct) ?? 0;
        var reversedConsumption = await db.ProductionConsumptions.AsNoTracking()
            .Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo != null)
            .SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        var reversedOutput = await db.ProductionOutputs.AsNoTracking()
            .Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo != null)
            .SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        var reversedReturn = await db.ProductionReturns.AsNoTracking()
            .Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo != null)
            .SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        adjustments += -reversedConsumption + reversedOutput + reversedReturn;
        var produced = await db.ProductionOutputs.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo == null && !db.ProductionOutputs.Any(r => r.ReversesNo == x.No)).SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        var consumed = await db.ProductionConsumptions.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo == null && !db.ProductionConsumptions.Any(r => r.ReversesNo == x.No)).SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        var returnedInput = await db.ProductionReturns.AsNoTracking().Where(x => x.ItemCode == itemCode && x.LotNo == lotNo && x.ReversesNo == null && !db.ProductionReturns.Any(r => r.ReversesNo == x.No)).SumAsync(x => (decimal?)x.Qty, ct) ?? 0;
        var expected = received.Sum(r => r.Qty) + produced - consumed + returnedInput - shipments.Sum(s => s.Qty) + returned + adjustments;
        var diff = stock - expected;
        return new TraceBalanceDto(itemCode, lotNo, received.Sum(r => r.Qty), produced, consumed, returnedInput,
            shipments.Sum(s => s.Qty), returned, adjustments, stock, expected, diff, diff == 0);
    }

    public async Task<ServiceResult> ExportAsync(string direction, string itemCode, string lotNo, CancellationToken ct)
    {
        var result = await TraceAsync(itemCode, lotNo, direction, ct);
        if (result.StatusCode != 200) return result;
        var trace = ((ApiSuccess<TraceResultDto>)result.Body!).Data!;
        var items = await db.Items.AsNoTracking().ToDictionaryAsync(x => x.ItemCode, ct);
        var groups = await db.ItemGroups.AsNoTracking().ToDictionaryAsync(x => x.ItemGroupCode, ct);
        var csv = new StringBuilder("No,Level,Parent No,Node Type,Item Code,Item Name,Item Category,Lot No,Document No,Process,Production Lot,Input Qty,Returned Qty,Net Qty,Unit,Expiry Date,Expiry Check,Receiving Date,Shipping Date,Supplier,Customer,Input Time,Worker,Note,Quantity Balance\r\n");
        for (var i = 0; i < trace.Nodes.Count; i++)
        {
            var n = trace.Nodes[i];
            items.TryGetValue(n.ItemCode, out var item);
            var category = item is not null && groups.TryGetValue(item.ItemGroupCode, out var group) ? group.Category.ToString() : null;
            csv.Append(i + 1).Append(',').Append(n.Level).Append(',').Append(n.ParentNo is null ? "" : n.ParentNo.Value + 1).Append(',')
                .Append(Csv(n.Type)).Append(',').Append(Csv(n.ItemCode)).Append(',')
                .Append(Csv(item?.ItemName)).Append(',').Append(Csv(category)).Append(',')
                .Append(Csv(n.LotNo)).Append(',').Append(Csv(n.DocumentNo)).Append(',')
                .Append(Csv(n.ProcessName ?? n.ProcessCode)).Append(',').Append(Csv(n.ProductionLotNo)).Append(',')
                .Append(n.InputQty?.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(n.ReturnedQty?.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(n.Qty.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Csv(item?.UomCode)).Append(',').Append(Csv(n.ExpiryDate?.ToString())).Append(',')
                .Append(Csv(n.ExpiryCheck)).Append(',')
                .Append(Csv(n.Type == "receipt" ? n.OccurredAt?.ToString("yyyy-MM-dd") : null)).Append(',')
                .Append(Csv(n.Type == "shipping" ? n.OccurredAt?.ToString("yyyy-MM-dd") : null)).Append(',')
                .Append(Csv(n.Type == "receipt" ? n.PartnerCode : null)).Append(',')
                .Append(Csv(n.Type == "shipping" ? n.PartnerCode : null)).Append(',')
                .Append(Csv(n.OccurredAt?.ToString("yyyy-MM-dd HH:mm:ss"))).Append(',')
                .Append(Csv(null)).Append(',').Append(Csv(null)).Append(',')
                .Append(Csv(i == 0 ? trace.Balance.ExpectedQty.ToString(CultureInfo.InvariantCulture) : null)).Append("\r\n");
        }
        return Outcome.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "traceability.csv");
    }

    private static string Csv(string? value) => '"' + (value ?? "").Replace("\"", "\"\"") + '"';

    private async Task<List<TraceShipment>> LoadShipmentsAsync(CancellationToken ct)
    {
        var results = await db.ShippingResults.AsNoTracking().OrderBy(r => r.No).ToListAsync(ct);
        var child = await db.ShippingResultLots.AsNoTracking().ToListAsync(ct);
        var picked = await db.PickingResultLots.AsNoTracking().ToListAsync(ct);
        var pickResults = await db.PickingResults.AsNoTracking().ToDictionaryAsync(r => r.No, ct);
        var plans = await db.ShippingPlans.AsNoTracking().ToListAsync(ct);
        var rows = new List<TraceShipment>();
        var shippedByLine = new Dictionary<string, decimal>();
        foreach (var result in results)
        {
            var lineKey = $"{result.SoNo}\u001f{result.SoBranchNo}\u001f{result.ItemCode}";
            var previous = shippedByLine.GetValueOrDefault(lineKey);
            var delta = Math.Max(0, result.Qty - previous);
            shippedByLine[lineKey] = Math.Max(previous, result.Qty);
            var customer = plans.FirstOrDefault(p => p.SoNo == result.SoNo && p.SoBranchNo == result.SoBranchNo
                && p.ItemCode == result.ItemCode)?.PartnerCode;
            var allocated = child.Where(c => c.ShippingResultNo == result.No).ToList();
            if (allocated.Count > 0)
            {
                rows.AddRange(allocated.Select(c => new TraceShipment(c.ItemCode, c.LotNo, c.SerialNo,
                    c.Qty, result.SoNo, result.SoBranchNo, customer, result.CreatedAt, result.No)));
                continue;
            }
            var fallback = picked.Where(c => pickResults.TryGetValue(c.PickingResultNo, out var p)
                && p.SoNo == result.SoNo && p.SoBranchNo == result.SoBranchNo && p.ItemCode == result.ItemCode)
                .OrderBy(c => c.PickingResultNo).ThenBy(c => c.No).ToList();
            if (fallback.Count > 0)
            {
                var skip = previous;
                var remaining = delta;
                foreach (var lot in fallback)
                {
                    if (remaining <= 0) break;
                    if (skip >= lot.Qty)
                    {
                        skip -= lot.Qty;
                        continue;
                    }
                    var qty = Math.Min(lot.Qty - skip, remaining);
                    skip = 0;
                    remaining -= qty;
                    rows.Add(new TraceShipment(lot.ItemCode, lot.LotNo, lot.SerialNo,
                        qty, result.SoNo, result.SoBranchNo, customer, result.CreatedAt, result.No));
                }
                continue;
            }
            rows.Add(new TraceShipment(result.ItemCode, result.LotNo, null, delta,
                result.SoNo, result.SoBranchNo, customer, result.CreatedAt, result.No));
        }
        return rows;
    }
}
