using Ims.Api.Features.Permissions;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Reasons;

public static class ReasonEndpoints
{
    public static IEndpointRouteBuilder MapReasonEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/masters/reasons").WithTags("Reasons");
        g.MapGet("", ListAsync).RequireLookup();
        g.MapGet("/{code}", GetAsync).RequireLookup();
        g.MapPost("", CreateAsync).RequirePermission("m_reason.New", "m_reason.Copy");
        g.MapPut("/{code}", UpdateAsync).RequirePermission("m_reason.Edit");
        g.MapDelete("/{code}", DeleteAsync).RequirePermission("m_reason.Delete");
        app.MapGet("/export/reasons", ExportCsvAsync).WithTags("Export").RequireAuthorization().RequirePermission("m_reason.Export");
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IReasonService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? q = null, [FromQuery] string? sort = null,
        [FromQuery] bool? isActive = null, [FromQuery] string? category = null) =>
        (await service.ListAsync(ct: ct, page: page, pageSize: pageSize, q: q, sort: sort, isActive: isActive, category: category)).ToHttpResult();

    private static async Task<IResult> GetAsync(
        [FromServices] IReasonService service, string code, CancellationToken ct) =>
        (await service.GetAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync(
        [FromServices] IReasonService service, [FromBody] ReasonWriteDto body, CancellationToken ct) =>
        (await service.CreateAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        [FromServices] IReasonService service, string code, [FromBody] ReasonWriteDto body, CancellationToken ct) =>
        (await service.UpdateAsync(code: code, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> DeleteAsync(
        [FromServices] IReasonService service, string code, CancellationToken ct) =>
        (await service.DeleteAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportCsvAsync([FromServices] IReasonService service, CancellationToken ct) =>
        (await service.ExportCsvAsync(ct: ct)).ToHttpResult();
}
