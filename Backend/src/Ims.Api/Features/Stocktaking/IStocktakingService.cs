using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Stocktaking;

internal interface IStocktakingService
{
    Task<ServiceResult> GetSessionAsync(CancellationToken ct);

    Task<ServiceResult> StartAsync(StartStocktakingDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> CompleteAsync(ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> FinalizeAsync(FinalizeStocktakingDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ListItemsAtLocationAsync(CancellationToken ct, string? locationCode = null);

    Task<ServiceResult> SaveResultAsync(StocktakingResultDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ListLabelsAsync(
        CancellationToken ct, string? view = null, string? locationCode = null, string? itemCode = null,
        string? itemName = null, bool notScanned = false, bool statusDiff = false, int page = 1, int pageSize = 50,
        string? sort = null);

    Task<ServiceResult> ExportLabelsCsvAsync(
        CancellationToken ct, string? view = null, string? locationCode = null, string? itemCode = null,
        string? itemName = null, bool notScanned = false, bool statusDiff = false);

    Task<ServiceResult> ListHistoryAsync(
        CancellationToken ct, string? q = null, int page = 1, int pageSize = 50, string? sort = null);

    Task<ServiceResult> ExportHistoryCsvAsync(CancellationToken ct, string? q = null);
}
