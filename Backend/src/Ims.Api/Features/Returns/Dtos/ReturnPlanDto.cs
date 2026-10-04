namespace Ims.Api.Features.Returns;

public sealed record ReturnPlanDto(
    string ReturnNo,
    string SerialNo,
    string? ReturnedByCode,
    string? ReturnedByName,
    string ItemCode,
    string? ItemName,
    decimal ExpectedQty,
    string? Unit,
    decimal ActualQty,
    DateOnly? ReturnDate,
    string? InvoiceNo,
    string Status,
    string? ShortageReason,
    string? LotNo,
    bool LabelIssued,
    DateTime CreatedAt,
    string? CreatedBy);
