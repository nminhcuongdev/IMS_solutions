using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.History;

internal interface IHistoryService
{
    Task<ServiceResult> ListPrintAsync(
        CancellationToken ct, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? category = null,
        int? status = null, string? q = null, string? sort = null, int? page = null, int? pageSize = null,
        string? item = null);

    Task<ServiceResult> CreatePrintAsync(PrintHistoryWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ReprintDataAsync(long no, CancellationToken ct);

    Task<ServiceResult> ResultReprintAsync(string type, long no, PrintReprintRequest body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ReprintAsync(long no, PrintReprintRequest body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ExportPrintCsvAsync(
        CancellationToken ct, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? category = null,
        int? status = null, string? q = null, string? sort = null, string? item = null);

    Task<ServiceResult> ListRfidAsync(
        CancellationToken ct, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? q = null,
        string? sort = null, int? page = null, int? pageSize = null);

    Task<ServiceResult> ExportRfidCsvAsync(
        CancellationToken ct, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? q = null,
        string? sort = null);
}
