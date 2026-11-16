namespace Ims.Api.Features.Traceability;

public sealed record TraceResultDto(string ItemCode, string? LotNo,
    IReadOnlyList<TraceNodeDto> Nodes, bool MultipleSuppliers, TraceBalanceDto Balance,
    IReadOnlyList<TraceBalanceDto>? Balances = null,
    IReadOnlyList<TraceEventDto>? Events = null)
{
    public string? Direction { get; init; }
    public TraceLotInfoDto? Lot { get; init; }
    public TraceProductionDto? Production { get; init; }
    public IReadOnlyList<TraceComponentDto>? Components { get; init; }
    public IReadOnlyList<TraceConsumedByDto>? ConsumedBy { get; init; }
    public IReadOnlyList<TraceReceiptDto>? Receipts { get; init; }
    public IReadOnlyList<TraceShipmentDto>? Shipments { get; init; }
}
