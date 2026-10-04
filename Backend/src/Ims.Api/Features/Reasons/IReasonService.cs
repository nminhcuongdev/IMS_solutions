using Ims.Api.Contracts;

namespace Ims.Api.Features.Reasons;

internal interface IReasonService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? category = null);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(ReasonWriteDto body, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, ReasonWriteDto body, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
