namespace Ims.Api.Features.Menu;

public sealed record MenuResponseData(
    string UserCode,
    string RoleCode,
    bool IsSuperAdmin,
    IReadOnlyList<MenuGroupDto> Groups);
