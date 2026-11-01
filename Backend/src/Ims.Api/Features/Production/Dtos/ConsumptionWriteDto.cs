namespace Ims.Api.Features.Production;

public sealed record ConsumptionWriteDto(string RequestId, string ProcessCode, string ItemCode,
    string? LotNo, decimal Qty, int? LineNo = null);
