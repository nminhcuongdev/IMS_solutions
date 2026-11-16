using Ims.Api.Features.Permissions;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.ItemGroups;

public static class ItemGroupEndpoints
{
    public static IEndpointRouteBuilder MapItemGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/masters/item-groups").WithTags("ItemGroups");
        g.MapGet("", ListAsync).RequireLookup();
        g.MapGet("/{code}", GetAsync).RequireLookup();
        g.MapPost("", CreateAsync).RequirePermission("m_itemgroup.New", "m_itemgroup.Copy");
        g.MapPut("/{code}", UpdateAsync).RequirePermission("m_itemgroup.Edit");
        g.MapDelete("/{code}", DeleteAsync).RequirePermission("m_itemgroup.Delete");
        app.MapGet("/export/item-groups", ExportCsvAsync).WithTags("Export").RequireAuthorization().RequirePermission("m_itemgroup.Export");
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IItemGroupService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? q = null, [FromQuery] string? sort = null,
        [FromQuery] bool? isActive = null) =>
        (await service.ListAsync(ct: ct, page: page, pageSize: pageSize, q: q, sort: sort, isActive: isActive)).ToHttpResult();

    private static async Task<IResult> GetAsync(
        [FromServices] IItemGroupService service, string code, CancellationToken ct) =>
        (await service.GetAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync(
        [FromServices] IItemGroupService service, [FromBody] ItemGroupWriteDto body, CancellationToken ct) =>
        (await service.CreateAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        [FromServices] IItemGroupService service, string code, [FromBody] ItemGroupWriteDto body, CancellationToken ct) =>
        (await service.UpdateAsync(code: code, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> DeleteAsync(
        [FromServices] IItemGroupService service, string code, CancellationToken ct) =>
        (await service.DeleteAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportCsvAsync([FromServices] IItemGroupService service, CancellationToken ct) =>
        (await service.ExportCsvAsync(ct: ct)).ToHttpResult();
}
