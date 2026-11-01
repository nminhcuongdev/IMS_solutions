using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Ims.Api.Http;

namespace Ims.Api.Features.Processes;

public static class ProcessEndpoints
{
    public static IEndpointRouteBuilder MapProcessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/masters/processes").WithTags("Processes");
        group.MapGet("", ListAsync);
        group.MapGet("/{code}", GetAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{code}", UpdateAsync);
        group.MapDelete("/{code}", DeleteAsync);
        group.MapGet("/export.csv", ExportCsvAsync);
        app.MapGet("/export/processes", ExportCsvAsync).WithTags("Processes");
        return app;
    }

    private static async Task<IResult> ListAsync([FromServices] IProcessService service, CancellationToken ct,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null, [FromQuery] string? q = null,
        [FromQuery] string? sort = null, [FromQuery] bool? isActive = null) =>
        (await service.ListAsync(ct, page, pageSize, q, sort, isActive)).ToHttpResult();

    private static async Task<IResult> GetAsync([FromServices] IProcessService service, string code, CancellationToken ct) =>
        (await service.GetAsync(code, ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync([FromServices] IProcessService service, [FromBody] ProcessWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.CreateAsync(body, ct, user)).ToHttpResult();

    private static async Task<IResult> UpdateAsync([FromServices] IProcessService service, string code, [FromBody] ProcessWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.UpdateAsync(code, body, ct, user)).ToHttpResult();

    private static async Task<IResult> DeleteAsync([FromServices] IProcessService service, string code, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.DeleteAsync(code, ct, user)).ToHttpResult();

    private static async Task<IResult> ExportCsvAsync([FromServices] IProcessService service, CancellationToken ct) =>
        (await service.ExportCsvAsync(ct)).ToHttpResult();
}
