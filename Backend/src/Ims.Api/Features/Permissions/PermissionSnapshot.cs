namespace Ims.Api.Features.Permissions;

internal sealed record PermissionSnapshot(string UserCode, bool IsSuperAdmin, FunctionActions[] Functions)
{
    public bool Allows(string functionCode, string action) =>
        Functions.Any(f => f.FunctionCode == functionCode && f.Actions.Contains(action, StringComparer.Ordinal));
}
