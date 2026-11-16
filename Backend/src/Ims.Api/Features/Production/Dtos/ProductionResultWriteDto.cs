namespace Ims.Api.Features.Production;

public sealed record ProductionResultWriteDto(
    string? Type,
    string? RequestId,
    string? ProcessCode = null,
    string? ItemCode = null,
    string? LotNo = null,
    decimal? Qty = null,
    int? LineNo = null,
    bool Complete = false,
    System.DateOnly? ExpiryDate = null,
    string? OutputRequestId = null);
