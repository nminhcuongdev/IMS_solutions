using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Picking;

internal interface IPickingService
{
    Task<ServiceResult> ListPlansAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? soNo = null, string? status = null);

    Task<ServiceResult> CreatePlanAsync(PickingPlanWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ChangeStatusAsync(PickingStatusChangeDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ListResultsAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, DateOnly? dateFrom = null,
        DateOnly? dateTo = null, string? itemCode = null, string? itemName = null, string? soNo = null);

    Task<ServiceResult> CreateResultAsync(PickingResultWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ExportPlansCsvAsync(CancellationToken ct);

    Task<ServiceResult> ExportResultsCsvAsync(CancellationToken ct);
}
