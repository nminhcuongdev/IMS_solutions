using Ims.Api.Features.Permissions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Factories;

public static class FactoryEndpoints
{
    public static IEndpointRouteBuilder MapFactoryEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/masters/factories").WithTags("Factories");
        g.MapGet("", ListAsync).RequireLookup();
        g.MapGet("/{code}", GetAsync).RequireLookup();
        g.MapPost("", CreateAsync).RequirePermission("m_factory.New", "m_factory.Copy");
        g.MapPut("/{code}", UpdateAsync).RequirePermission("m_factory.Edit");
        g.MapDelete("/{code}", DeleteAsync).RequirePermission("m_factory.Delete");
        app.MapGet("/export/factories", ExportCsvAsync).WithTags("Export").RequireAuthorization().RequirePermission("m_factory.Export");
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IFactoryService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? q = null, [FromQuery] string? sort = null,
        [FromQuery] bool? isActive = null) =>
        (await service.ListAsync(ct: ct, page: page, pageSize: pageSize, q: q, sort: sort, isActive: isActive)).ToHttpResult();

    private static async Task<IResult> GetAsync(
        [FromServices] IFactoryService service, string code, CancellationToken ct) =>
        (await service.GetAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync(
        [FromServices] IFactoryService service, ClaimsPrincipal user, [FromBody] FactoryWriteDto body,
        CancellationToken ct) =>
        (await service.CreateAsync(user: user, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        [FromServices] IFactoryService service, string code, ClaimsPrincipal user, [FromBody] FactoryWriteDto body,
        CancellationToken ct) =>
        (await service.UpdateAsync(code: code, user: user, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> DeleteAsync(
        [FromServices] IFactoryService service, string code, CancellationToken ct) =>
        (await service.DeleteAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportCsvAsync([FromServices] IFactoryService service, CancellationToken ct) =>
        (await service.ExportCsvAsync(ct: ct)).ToHttpResult();
}
