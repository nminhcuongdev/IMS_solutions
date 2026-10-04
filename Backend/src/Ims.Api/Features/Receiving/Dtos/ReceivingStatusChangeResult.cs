namespace Ims.Api.Features.Receiving;

public sealed record ReceivingStatusChangeResult(int Updated, IReadOnlyList<string> Skipped);
