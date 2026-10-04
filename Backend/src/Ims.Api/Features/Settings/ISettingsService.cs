using Ims.Api.Contracts;

namespace Ims.Api.Features.Settings;

internal interface ISettingsService
{
    Task<ServiceResult> GetAsync(CancellationToken ct);

    Task<ServiceResult> SaveAsync(SettingsDto body, CancellationToken ct);
}
