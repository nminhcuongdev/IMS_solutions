using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Roles;

internal interface IRoleService
{
    Task<ServiceResult> ListAsync(CancellationToken ct);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(RoleWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, RoleWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, ClaimsPrincipal user, CancellationToken ct);
}
