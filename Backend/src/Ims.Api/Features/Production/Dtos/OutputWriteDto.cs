namespace Ims.Api.Features.Production;

public sealed record OutputWriteDto(string RequestId, decimal Qty, bool Complete = false,
    DateOnly? ExpiryDate = null, string? LotNo = null);
