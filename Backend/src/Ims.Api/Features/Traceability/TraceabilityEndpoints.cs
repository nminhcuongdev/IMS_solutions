using Ims.Api.Features.Permissions;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Traceability;

public static class TraceabilityEndpoints
{
    public static IEndpointRouteBuilder MapTraceabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/traceability").WithTags("Traceability").RequireAuthorization().RequireModule("production");
        group.MapGet("/lots", LotsAsync).RequirePermission("trace_back.Open", "trace_fwd.Open");
        group.MapGet("", TraceAsync).RequirePermission("trace_back.Open", "trace_fwd.Open");
        app.MapGet("/export/traceability", ExportAsync).WithTags("Traceability").RequireAuthorization().RequireModule("production").RequirePermission("trace_back.Export", "trace_fwd.Export");
        return app;
    }

    private static async Task<IResult> LotsAsync([FromServices] ITraceabilityService service,
        [AsParameters] TraceQueryDto query, CancellationToken ct) =>
        (await service.LotsAsync(query, ct)).ToHttpResult();

    private static async Task<IResult> TraceAsync([FromServices] ITraceabilityService service, CancellationToken ct,
        [FromQuery] string? direction = null, [FromQuery] string? itemCode = null, [FromQuery] string? lotNo = null,
        [FromQuery] string? include = null) =>
        (await service.TraceAsync(direction, itemCode, lotNo, include, ct)).ToHttpResult();

    private static async Task<IResult> ExportAsync([FromServices] ITraceabilityService service,
        [FromQuery] string direction, [FromQuery] string itemCode, [FromQuery] string lotNo,
        CancellationToken ct) =>
        (await service.ExportAsync(direction, itemCode, lotNo, ct)).ToHttpResult();
}
