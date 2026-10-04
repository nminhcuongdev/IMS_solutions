namespace Ims.Api.Features.Picking;

public sealed record PickingResultWriteDto(
    string ItemCode = "",
    decimal? Qty = null,
    string? SoNo = null,
    string? SerialNo = null,
    string? LocationCode = null,
    string? LotNo = null);
