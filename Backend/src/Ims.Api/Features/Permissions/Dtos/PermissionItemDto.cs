namespace Ims.Api.Features.Permissions;

public sealed record PermissionItemDto(string RoleCode, string PermissionCode, bool IsAllowed);
