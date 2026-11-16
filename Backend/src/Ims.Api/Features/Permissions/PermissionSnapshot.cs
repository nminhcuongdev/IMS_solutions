namespace Ims.Api.Features.Permissions;

internal sealed record PermissionSnapshot(
    string UserCode,
    string RoleCode,
    bool IsSuperAdmin,
    bool IsBuiltin,
    IReadOnlySet<string> Allowed,
    FunctionActions[] Functions,
    IReadOnlySet<string> DisabledModules)
{
    public const string AnyScreen = "*";

    public bool Allows(string functionCode, string action) =>
        Allowed.Contains(functionCode + "." + action);

    public bool AllowsCode(string permissionCode) =>
        permissionCode == AnyScreen ? Functions.Length > 0 || IsSuperAdmin || IsBuiltin : Allowed.Contains(permissionCode);

    public bool Satisfies(IEnumerable<string> anyOf) => anyOf.Any(AllowsCode);
}
