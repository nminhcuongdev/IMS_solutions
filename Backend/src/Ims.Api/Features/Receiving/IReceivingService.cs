using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Receiving;

internal interface IReceivingService
{
    Task<ServiceResult> ListPlansAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? poNo = null, string? status = null);

    Task<ServiceResult> CreatePlanAsync(ReceivingPlanWriteDto body, CancellationToken ct);

    Task<ServiceResult> ChangeStatusAsync(ReceivingStatusChangeDto body, CancellationToken ct, ClaimsPrincipal? user = null);

    Task<ServiceResult> RegisterShortageAsync(string poNo, ReceivingShortageDto body, CancellationToken ct);

    Task<ServiceResult> ListResultsAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, DateOnly? dateFrom = null,
        DateOnly? dateTo = null, string? itemCode = null, string? itemName = null, string? poNo = null,
        string? poNoPresence = null);

    Task<ServiceResult> CreateResultAsync(ReceivingResultWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ExportPlansCsvAsync(CancellationToken ct);

    Task<ServiceResult> ExportResultsCsvAsync(CancellationToken ct);
}
