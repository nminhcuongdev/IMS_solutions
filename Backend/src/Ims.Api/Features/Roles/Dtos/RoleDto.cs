namespace Ims.Api.Features.Roles;

public sealed record RoleDto(string RoleCode, string RoleName, bool IsBuiltin, int UserCount);
