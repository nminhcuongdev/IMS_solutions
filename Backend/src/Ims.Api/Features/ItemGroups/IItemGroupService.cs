using Ims.Api.Contracts;

namespace Ims.Api.Features.ItemGroups;

internal interface IItemGroupService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(ItemGroupWriteDto body, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, ItemGroupWriteDto body, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
