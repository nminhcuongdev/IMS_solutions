using Ims.Api.Contracts;

namespace Ims.Api.Features.Ht;

internal interface IHtService
{
    Task<ServiceResult> GetConfigAsync(CancellationToken ct);

    Task<ServiceResult> ListPrintersAsync(CancellationToken ct);

    Task<ServiceResult> ListLotsAsync(CancellationToken ct, string? q = null, string? itemCode = null);
}
