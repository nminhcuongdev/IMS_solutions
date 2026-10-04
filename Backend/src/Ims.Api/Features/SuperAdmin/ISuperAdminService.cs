using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.SuperAdmin;

internal interface ISuperAdminService
{
    Task<ServiceResult> GetGeneralAsync(ClaimsPrincipal user, CancellationToken ct, string? category = null);

    Task<ServiceResult> SaveGeneralAsync(ClaimsPrincipal user, GeneralSaveDto body, CancellationToken ct);

    Task<ServiceResult> GetVisibilityAsync(ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> SaveVisibilityAsync(ClaimsPrincipal user, VisibilitySaveDto body, CancellationToken ct);

    Task<ServiceResult> GetDefaultMasterAsync(string type, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> SaveDefaultMasterAsync(
        string type, ClaimsPrincipal user, DefaultMasterSaveDto body, CancellationToken ct);

    ServiceResult GetDbInfoAsync(ClaimsPrincipal user, IConfiguration config);
}
