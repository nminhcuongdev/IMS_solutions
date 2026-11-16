using Ims.Api.Contracts;

namespace Ims.Api.Features.Traceability;

internal interface ITraceabilityService
{
    Task<ServiceResult> LotsAsync(TraceQueryDto query, CancellationToken ct);
    Task<ServiceResult> TraceAsync(string? direction, string? itemCode, string? lotNo, string? include, CancellationToken ct);
    Task<ServiceResult> BackwardAsync(string itemCode, string lotNo, CancellationToken ct);
    Task<ServiceResult> ForwardAsync(string itemCode, string lotNo, CancellationToken ct);
    Task<ServiceResult> EventsAsync(string itemCode, string lotNo, CancellationToken ct);
    Task<ServiceResult> BalanceAsync(string itemCode, string lotNo, CancellationToken ct);
    Task<ServiceResult> ExportAsync(string direction, string itemCode, string lotNo, CancellationToken ct);
}
