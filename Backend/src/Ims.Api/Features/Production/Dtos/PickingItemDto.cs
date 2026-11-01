namespace Ims.Api.Features.Production;

public sealed record PickingItemDto(string OrderNo, int? LineNo = null);
