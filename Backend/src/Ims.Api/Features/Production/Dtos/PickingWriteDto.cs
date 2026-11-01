namespace Ims.Api.Features.Production;

public sealed record PickingWriteDto(IReadOnlyList<PickingItemDto> Items, bool Picked);
