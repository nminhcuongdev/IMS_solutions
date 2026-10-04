using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Locations;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/masters/locations").WithTags("Locations");
        g.MapGet("", ListAsync);
        g.MapGet("/{code}", GetAsync);
        g.MapPost("", CreateAsync);
        g.MapPut("/{code}", UpdateAsync);
        g.MapDelete("/{code}", DeleteAsync);
        g.MapGet("/export.csv", ExportCsvAsync);
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] ILocationService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? q = null, [FromQuery] string? sort = null,
        [FromQuery] bool? isActive = null, [FromQuery] string? type = null, [FromQuery] string? zoneCode = null) =>
        (await service.ListAsync(ct: ct, page: page, pageSize: pageSize, q: q, sort: sort, isActive: isActive, type: type, zoneCode: zoneCode)).ToHttpResult();

    private static async Task<IResult> GetAsync(
        [FromServices] ILocationService service, string code, CancellationToken ct) =>
        (await service.GetAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync(
        [FromServices] ILocationService service, [FromBody] LocationWriteDto body, CancellationToken ct) =>
        (await service.CreateAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        [FromServices] ILocationService service, string code, [FromBody] LocationWriteDto body, CancellationToken ct) =>
        (await service.UpdateAsync(code: code, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> DeleteAsync(
        [FromServices] ILocationService service, string code, CancellationToken ct) =>
        (await service.DeleteAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportCsvAsync([FromServices] ILocationService service, CancellationToken ct) =>
        (await service.ExportCsvAsync(ct: ct)).ToHttpResult();
}
