using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Permissions;

internal interface IPermissionService
{
    Task<ServiceResult> MatrixAsync(string? roleCode, CancellationToken ct);

    Task<ServiceResult> SaveAsync(PermissionMatrixSaveDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> MeAsync(ClaimsPrincipal user, CancellationToken ct);
}
