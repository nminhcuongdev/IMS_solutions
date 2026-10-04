using Ims.Api.Contracts;

namespace Ims.Api.Features.Auth;

internal interface IAuthService
{
    Task<ServiceResult> LoginAsync(LoginRequest body, CancellationToken ct);

    Task<ServiceResult> LogoutAsync(HttpContext http, CancellationToken ct);
}
