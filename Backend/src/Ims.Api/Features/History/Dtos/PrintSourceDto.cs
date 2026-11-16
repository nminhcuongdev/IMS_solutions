namespace Ims.Api.Features.History;

public sealed record PrintSourceDto(
    string DocNo, string ItemCode, string? ItemName, string? UomCode, decimal Qty, string? LotNo,
    DateOnly? ExpiryDate, DateOnly? ProductionDate, DateOnly? ScheduledDate, string? FactoryCode,
    string? FactoryName, string? Address, string? PartnerCode, string? PartnerName, string? Barcode);
