namespace Ims.Api.Features.Shipping;

public sealed record ShippingStatusChangeResult(int Updated, IReadOnlyList<string> Skipped);
