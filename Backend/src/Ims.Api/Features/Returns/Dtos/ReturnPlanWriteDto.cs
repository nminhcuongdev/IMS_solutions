namespace Ims.Api.Features.Returns;

public sealed record ReturnPlanWriteDto(
    string ReturnNo,
    string? SerialNo = null,
    string? ReturnedByCode = null,
    string ItemCode = "",
    decimal ExpectedQty = 0,
    DateOnly? ReturnDate = null,
    string? InvoiceNo = null,
    string? FactoryCode = null,
    string? LotNo = null);
