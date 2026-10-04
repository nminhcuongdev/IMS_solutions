using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Inventory;

internal interface IInventoryService
{
    Task<ServiceResult> ListStockAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        string? warehouse = null, string? itemCode = null, string? itemName = null, string? category = null,
        string? lot = null, string? expiration = null, string? stockLevel = null, string? hold = null,
        string? groupBy = null);

    Task<ServiceResult> AdjustStockAsync(StockAdjustDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> HoldStockAsync(StockHoldDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ReleaseHoldAsync(StockKeysDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> DisposeStockAsync(StockDisposalDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> TransferStockAsync(StockTransferDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ListHistoryAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? sort = null, string? q = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? itemCode = null, string? itemName = null,
        string? action = null);

    Task<ServiceResult> RestoreDisposalAsync(RestoreDisposalDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ExportStockCsvAsync(
        CancellationToken ct, string? q = null, string? warehouse = null, string? itemCode = null,
        string? itemName = null, string? category = null, string? lot = null, string? expiration = null,
        string? stockLevel = null, string? hold = null);

    Task<ServiceResult> ExportHistoryCsvAsync(
        CancellationToken ct, string? q = null, DateOnly? dateFrom = null, DateOnly? dateTo = null,
        string? itemCode = null, string? itemName = null, string? action = null);
}
