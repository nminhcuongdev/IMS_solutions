using Ims.Api.Contracts;

namespace Ims.Api.Features.Locations;

internal interface ILocationService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? type = null, string? zoneCode = null);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(LocationWriteDto body, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, LocationWriteDto body, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
