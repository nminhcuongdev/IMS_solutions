namespace Ims.Api.Features.Auth;

public sealed record LoginResponseData(
    string Token,
    int ExpiresIn,
    string TokenType,
    string UserCode,
    string UserName,
    string RoleCode,
    bool IsSuperAdmin,
    string? DefaultLanguage = null);
