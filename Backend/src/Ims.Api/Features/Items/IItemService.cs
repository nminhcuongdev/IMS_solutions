using Ims.Api.Contracts;

namespace Ims.Api.Features.Items;

internal interface IItemService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? itemGroupCode = null);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(ItemWriteDto body, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, ItemWriteDto body, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
