using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/masters/users").WithTags("Users");
        g.MapGet("", ListAsync);
        g.MapGet("/{code}", GetAsync);
        g.MapPost("", CreateAsync);
        g.MapPut("/{code}", UpdateAsync);
        g.MapDelete("/{code}", DeleteAsync);
        g.MapGet("/export.csv", ExportCsvAsync);
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IUserService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? q = null, [FromQuery] string? sort = null,
        [FromQuery] bool? isActive = null, [FromQuery] string? roleCode = null) =>
        (await service.ListAsync(ct: ct, page: page, pageSize: pageSize, q: q, sort: sort, isActive: isActive, roleCode: roleCode)).ToHttpResult();

    private static async Task<IResult> GetAsync(
        [FromServices] IUserService service, string code, CancellationToken ct) =>
        (await service.GetAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync(
        [FromServices] IUserService service, [FromBody] UserWriteDto body, CancellationToken ct) =>
        (await service.CreateAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        [FromServices] IUserService service, string code, [FromBody] UserWriteDto body, CancellationToken ct) =>
        (await service.UpdateAsync(code: code, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> DeleteAsync(
        [FromServices] IUserService service, string code, CancellationToken ct) =>
        (await service.DeleteAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportCsvAsync([FromServices] IUserService service, CancellationToken ct) =>
        (await service.ExportCsvAsync(ct: ct)).ToHttpResult();
}
