namespace Ims.Api.Features.Production;

public sealed record ProductionWorkDto(string Type, long No, string ProductionOrderNo,
    string? ItemCode, string? LotNo, decimal? Qty, DateTime CreatedAt, string? CreatedBy,
    bool IsReversed = false);
