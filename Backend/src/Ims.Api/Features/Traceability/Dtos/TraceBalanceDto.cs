namespace Ims.Api.Features.Traceability;

public sealed record TraceBalanceDto(string ItemCode, string? LotNo,
    decimal ReceivedQty, decimal ProducedQty, decimal ConsumedQty,
    decimal ReturnedInputQty, decimal ShippedQty, decimal CustomerReturnQty,
    decimal AdjustmentQty, decimal StockQty, decimal ExpectedQty, decimal DiffQty, bool Ok);
