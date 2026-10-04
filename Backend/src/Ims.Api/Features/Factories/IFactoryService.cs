using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Factories;

internal interface IFactoryService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(ClaimsPrincipal user, FactoryWriteDto body, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, ClaimsPrincipal user, FactoryWriteDto body, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
