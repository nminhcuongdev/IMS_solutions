namespace Ims.Api.Features.Permissions;

public sealed record PermissionMatrixRowDto(
    string PermissionCode,
    string Device,
    string LargeCode,
    string LargeName,
    string FunctionCode,
    string FunctionName,
    string ActionType,
    bool IsAllowed,
    bool Locked);
