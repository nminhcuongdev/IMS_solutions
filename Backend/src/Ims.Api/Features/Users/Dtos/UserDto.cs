namespace Ims.Api.Features.Users;

public sealed record UserDto(
    string UserCode, string UserName, string RoleCode, string? RoleName,
    string? FactoryCode, string? DefaultLanguage, bool IsActive,
    string? Remark1, string? Remark2, string? Remark3, string? Remark4, string? Remark5,
    DateTime CreatedAt, DateTime? UpdatedAt);
