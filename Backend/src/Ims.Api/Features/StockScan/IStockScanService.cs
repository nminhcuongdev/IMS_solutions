using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.StockScan;

internal interface IStockScanService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, string? itemCode = null, string? itemName = null, string? scanned = null);

    Task<ServiceResult> SummaryAsync(CancellationToken ct);

    Task<ServiceResult> ReadAsync(StockScanReadDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ResetAsync(CancellationToken ct);
}
