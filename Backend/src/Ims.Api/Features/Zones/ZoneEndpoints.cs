using Ims.Api.Features.Permissions;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Zones;

public static class ZoneEndpoints
{
    public static IEndpointRouteBuilder MapZoneEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/masters/zones").WithTags("Zones");
        g.MapGet("", ListAsync).RequireLookup();
        g.MapGet("/{code}", GetAsync).RequireLookup();
        g.MapPost("", CreateAsync).RequirePermission("m_zone.New", "m_zone.Copy");
        g.MapPut("/{code}", UpdateAsync).RequirePermission("m_zone.Edit");
        g.MapDelete("/{code}", DeleteAsync).RequirePermission("m_zone.Delete");
        app.MapGet("/export/zones", ExportCsvAsync).WithTags("Export").RequireAuthorization().RequirePermission("m_zone.Export");
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IZoneService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? q = null, [FromQuery] string? sort = null,
        [FromQuery] bool? isActive = null) =>
        (await service.ListAsync(ct: ct, page: page, pageSize: pageSize, q: q, sort: sort, isActive: isActive)).ToHttpResult();

    private static async Task<IResult> GetAsync(
        [FromServices] IZoneService service, string code, CancellationToken ct) =>
        (await service.GetAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync(
        [FromServices] IZoneService service, [FromBody] ZoneWriteDto body, CancellationToken ct) =>
        (await service.CreateAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        [FromServices] IZoneService service, string code, [FromBody] ZoneWriteDto body, CancellationToken ct) =>
        (await service.UpdateAsync(code: code, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> DeleteAsync(
        [FromServices] IZoneService service, string code, CancellationToken ct) =>
        (await service.DeleteAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportCsvAsync([FromServices] IZoneService service, CancellationToken ct) =>
        (await service.ExportCsvAsync(ct: ct)).ToHttpResult();
}
