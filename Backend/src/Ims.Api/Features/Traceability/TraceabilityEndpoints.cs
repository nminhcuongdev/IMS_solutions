using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Traceability;

public static class TraceabilityEndpoints
{
    public static IEndpointRouteBuilder MapTraceabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/traceability").WithTags("Traceability").RequireAuthorization();
        group.MapGet("/lots", LotsAsync);
        group.MapGet("/backward", BackwardAsync);
        group.MapGet("/forward", ForwardAsync);
        group.MapGet("/events", EventsAsync);
        group.MapGet("/balance", BalanceAsync);
        app.MapGet("/export/traceability", ExportAsync).WithTags("Traceability").RequireAuthorization();
        return app;
    }

    private static async Task<IResult> LotsAsync([FromServices] ITraceabilityService service,
        [AsParameters] TraceQueryDto query, CancellationToken ct) =>
        (await service.LotsAsync(query, ct)).ToHttpResult();

    private static async Task<IResult> BackwardAsync([FromServices] ITraceabilityService service,
        [FromQuery] string itemCode, [FromQuery] string lotNo, CancellationToken ct) =>
        (await service.BackwardAsync(itemCode, lotNo, ct)).ToHttpResult();

    private static async Task<IResult> ForwardAsync([FromServices] ITraceabilityService service,
        [FromQuery] string itemCode, [FromQuery] string lotNo, CancellationToken ct) =>
        (await service.ForwardAsync(itemCode, lotNo, ct)).ToHttpResult();

    private static async Task<IResult> EventsAsync([FromServices] ITraceabilityService service,
        [FromQuery] string itemCode, [FromQuery] string lotNo, CancellationToken ct) =>
        (await service.EventsAsync(itemCode, lotNo, ct)).ToHttpResult();

    private static async Task<IResult> BalanceAsync([FromServices] ITraceabilityService service,
        [FromQuery] string itemCode, [FromQuery] string lotNo, CancellationToken ct) =>
        (await service.BalanceAsync(itemCode, lotNo, ct)).ToHttpResult();

    private static async Task<IResult> ExportAsync([FromServices] ITraceabilityService service,
        [FromQuery] string direction, [FromQuery] string itemCode, [FromQuery] string lotNo,
        CancellationToken ct) =>
        (await service.ExportAsync(direction, itemCode, lotNo, ct)).ToHttpResult();
}
