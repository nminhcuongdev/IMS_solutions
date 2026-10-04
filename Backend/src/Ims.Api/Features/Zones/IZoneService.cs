using Ims.Api.Contracts;

namespace Ims.Api.Features.Zones;

internal interface IZoneService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(ZoneWriteDto body, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, ZoneWriteDto body, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
