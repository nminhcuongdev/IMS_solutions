namespace Ims.Api.Features.Traceability;

public sealed record TraceReceiptDto(string ItemCode, string? LotNo, string? PoNo, string? PartnerCode, string? PartnerName, DateTime? ReceivedAt, decimal Qty, bool SupplierUnknown);
