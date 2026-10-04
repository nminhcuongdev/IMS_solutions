using Ims.Api.Contracts;

namespace Ims.Api.Features.Permissions;

internal interface IPermissionService
{
    Task<ServiceResult> ListAsync(CancellationToken ct);

    Task<ServiceResult> SaveAsync(PermissionSaveDto body, CancellationToken ct);
}
