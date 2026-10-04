namespace Ims.Api.Features.Receiving;

public sealed record ReceivingPlanWriteDto(
    string PoNo,
    string? SerialNo = null,
    string? SupplierCode = null,
    string ItemCode = "",
    decimal PlannedQty = 0,
    string? Unit = null,
    DateOnly? ScheduledDate = null,
    string? InvoiceNo = null,
    string? FactoryCode = null,
    string? LotNo = null,
    string? LocationCode = null);
