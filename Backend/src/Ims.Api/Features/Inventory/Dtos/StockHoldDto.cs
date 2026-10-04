namespace Ims.Api.Features.Inventory;

public sealed record StockHoldDto(IReadOnlyList<StockKeyDto>? Keys, string? ReasonCode);
