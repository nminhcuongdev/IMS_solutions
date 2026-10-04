using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Returns;

internal interface IReturnsService
{
    Task<ServiceResult> ListPlansAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? returnNo = null, string? status = null);

    Task<ServiceResult> CreatePlanAsync(ReturnPlanWriteDto body, CancellationToken ct);

    Task<ServiceResult> ChangeStatusAsync(ReturnStatusChangeDto body, CancellationToken ct);

    Task<ServiceResult> RegisterShortageAsync(string returnNo, ReturnShortageDto body, CancellationToken ct);

    Task<ServiceResult> ListResultsAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, DateOnly? dateFrom = null,
        DateOnly? dateTo = null, string? itemCode = null, string? returnNo = null);

    Task<ServiceResult> CreateResultAsync(ReturnResultWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ExportPlansCsvAsync(CancellationToken ct);
}
