namespace Ims.Api.Features.Users;

public sealed record UserWriteDto(
    string UserCode, string UserName, string RoleCode,
    string? Password = null,
    string? FactoryCode = null,
    string? DefaultLanguage = null,
    bool IsActive = true,
    string? Remark1 = null, string? Remark2 = null, string? Remark3 = null,
    string? Remark4 = null, string? Remark5 = null);
