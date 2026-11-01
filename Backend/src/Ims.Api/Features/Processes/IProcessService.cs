using Ims.Api.Contracts;
using System.Security.Claims;

namespace Ims.Api.Features.Processes;

internal interface IProcessService
{
    Task<ServiceResult> ListAsync(CancellationToken ct, int? page = null, int? pageSize = null,
        string? q = null, string? sort = null, bool? isActive = null);
    Task<ServiceResult> GetAsync(string code, CancellationToken ct);
    Task<ServiceResult> CreateAsync(ProcessWriteDto body, CancellationToken ct, ClaimsPrincipal? user = null);
    Task<ServiceResult> UpdateAsync(string code, ProcessWriteDto body, CancellationToken ct, ClaimsPrincipal? user = null);
    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct, ClaimsPrincipal? user = null);
    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
