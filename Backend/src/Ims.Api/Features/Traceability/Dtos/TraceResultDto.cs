namespace Ims.Api.Features.Traceability;

public sealed record TraceResultDto(string ItemCode, string? LotNo,
    IReadOnlyList<TraceNodeDto> Nodes, bool MultipleSuppliers, TraceBalanceDto Balance,
    IReadOnlyList<TraceBalanceDto>? Balances = null);
