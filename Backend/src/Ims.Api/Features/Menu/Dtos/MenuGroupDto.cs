namespace Ims.Api.Features.Menu;

public sealed record MenuGroupDto(
    string LargeCode,
    string LargeName,
    int? DisplayOrder,
    IReadOnlyList<MenuFunctionDto> Functions);
