namespace Ims.Api.Features.Permissions;

public sealed record PermissionRowDto(
    string PermissionCode,
    string LargeCode,
    string LargeName,
    string FunctionCode,
    string FunctionName,
    string Action,
    bool General,
    bool Worker);
