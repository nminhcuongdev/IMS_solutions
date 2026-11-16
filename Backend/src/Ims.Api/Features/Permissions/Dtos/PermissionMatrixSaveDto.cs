namespace Ims.Api.Features.Permissions;

public sealed record PermissionMatrixSaveDto(string? RoleCode, List<PermissionMatrixItemDto>? Items);
