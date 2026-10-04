namespace Ims.Api.Features.Shipping;

public sealed record ShippingPlanWriteDto(
    string SlipNo,
    string? SerialNo = null,
    string? CustomerCode = null,
    string ItemCode = "",
    decimal PlannedQty = 0,
    DateOnly? ScheduledDate = null,
    string? FactoryCode = null,
    string? GateCode = null,
    string? InvoiceNo = null);
