using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Shipping;

internal interface IShippingService
{
    Task<ServiceResult> ListPlansAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? slipNo = null, string? status = null);

    Task<ServiceResult> CreatePlanAsync(ShippingPlanWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ChangeStatusAsync(ShippingStatusChangeDto body, CancellationToken ct);

    Task<ServiceResult> RegisterShortageAsync(string soNo, ShippingShortageDto body, CancellationToken ct);

    Task<ServiceResult> ListResultsAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, DateOnly? dateFrom = null,
        DateOnly? dateTo = null, string? itemCode = null, string? itemName = null, string? slipNo = null,
        string? slipNoPresence = null);

    Task<ServiceResult> CreateResultAsync(ShippingResultWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ExportPlansCsvAsync(CancellationToken ct);

    Task<ServiceResult> ExportResultsCsvAsync(CancellationToken ct);
}
