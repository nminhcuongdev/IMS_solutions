namespace Ims.Api.Features.Returns;

public sealed record ReturnResultWriteDto(
    string ItemCode = "",
    decimal? Qty = null,
    string? ReturnNo = null,
    string? SerialNo = null,
    string? LocationCode = null,
    string? LotNo = null);
