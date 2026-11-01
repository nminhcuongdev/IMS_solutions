using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Production;

internal interface IProductionService
{
    Task<ServiceResult> ListAsync(CancellationToken ct, string? unit = null, string? orderNo = null,
        string? itemCode = null, string? itemName = null, string? productionLotNo = null,
        string? fgLotNo = null, int? status = null, DateOnly? startDateFrom = null,
        DateOnly? startDateTo = null, int? page = null, int? pageSize = null, string? sort = null);
    Task<ServiceResult> GetAsync(string orderNo, CancellationToken ct);
    Task<ServiceResult> LinesAsync(string orderNo, CancellationToken ct);
    Task<ServiceResult> WorksAsync(string orderNo, CancellationToken ct);
    Task<ServiceResult> StatusAsync(StatusWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> PickingAsync(PickingWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> ReceiptAsync(string orderNo, ReceiptWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> ConsumptionAsync(string orderNo, ConsumptionWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> ReturnAsync(string orderNo, ReturnWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> OutputAsync(string orderNo, OutputWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> LabelAsync(string orderNo, LabelWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> ReverseConsumptionAsync(long no, ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> ReverseOutputAsync(long no, ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> ReverseReturnAsync(long no, ReverseWriteDto body, ClaimsPrincipal user, CancellationToken ct);
    Task<ServiceResult> ResultsAsync(CancellationToken ct, string? view = null, string? type = null,
        string? orderNo = null, string? itemCode = null, string? lotNo = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, int? page = null,
        int? pageSize = null, string? sort = null, string? itemName = null, bool showReversed = false);
    Task<ServiceResult> ExportOrdersAsync(CancellationToken ct);
    Task<ServiceResult> ExportResultsAsync(CancellationToken ct);
}
