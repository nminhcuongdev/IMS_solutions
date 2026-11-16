namespace Ims.Api.Features.Permissions;

public sealed record MyPermissionsDto(
    string UserCode,
    string RoleCode,
    bool IsSuperAdmin,
    bool IsBuiltin,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<FunctionActions> Functions,
    IReadOnlyList<string>? VisibleFunctions = null);
