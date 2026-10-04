using Ims.Api.Contracts;

namespace Ims.Api.Features.Defaults;

internal interface IDefaultValueService
{
    Task<ServiceResult> ListAsync(string type, CancellationToken ct, bool? isActive = null);
}
