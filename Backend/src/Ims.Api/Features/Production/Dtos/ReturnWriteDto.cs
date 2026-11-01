namespace Ims.Api.Features.Production;

public sealed record ReturnWriteDto(string RequestId, string ItemCode, string? LotNo, decimal Qty);
