using Ims.Api.Contracts;

namespace Ims.Api.Features.Alerts;

internal interface IAlertService
{
    Task<ServiceResult> ListAsync(CancellationToken ct);
}
