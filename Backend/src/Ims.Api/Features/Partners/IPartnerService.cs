using Ims.Api.Contracts;

namespace Ims.Api.Features.Partners;

internal interface IPartnerService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, bool? isSupplier = null, bool? isCustomer = null);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(PartnerWriteDto body, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, PartnerWriteDto body, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
