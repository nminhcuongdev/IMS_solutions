namespace Ims.Api.Features.Traceability;

public sealed record TraceShipmentDto(string ItemCode, string? LotNo, string? SoNo, string? PartnerCode, string? PartnerName, decimal ShippedQty, DateTime? ShippedAt, bool DestinationUnknown);
