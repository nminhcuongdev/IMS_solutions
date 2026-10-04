namespace Ims.Api.Features.Picking;

public sealed record PickingStatusChangeResult(int Updated, IReadOnlyList<string> Skipped);
