namespace Ims.Api.Features.Production;

public sealed record ProductionReceiptDto(string ProductionOrderNo, DateTime? ReceivedAt, string? ReceivedBy);
