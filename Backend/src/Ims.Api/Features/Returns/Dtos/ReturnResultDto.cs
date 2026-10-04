namespace Ims.Api.Features.Returns;

public sealed record ReturnResultDto(
    long No,
    DateTime CreatedAt,
    string ItemCode,
    string? ItemName,
    decimal Qty,
    string? Unit,
    string? ReturnNo,
    string? SerialNo,
    string? ReturnedByCode,
    string? ReturnedByName,
    string? LocationCode,
    string? LotNo,
    string? Worker,
    string Status,
    string? ShortageReason);
