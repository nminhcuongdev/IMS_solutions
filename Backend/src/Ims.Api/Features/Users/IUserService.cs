using Ims.Api.Contracts;

namespace Ims.Api.Features.Users;

internal interface IUserService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? roleCode = null);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(UserWriteDto body, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, UserWriteDto body, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
