using Ims.Api.Features.Permissions;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Partners;

public static class PartnerEndpoints
{
    public static IEndpointRouteBuilder MapPartnerEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/masters/partners").WithTags("Partners");
        g.MapGet("", ListAsync).RequireLookup();
        g.MapGet("/{code}", GetAsync).RequireLookup();
        g.MapPost("", CreateAsync).RequirePermission("m_partner.New", "m_partner.Copy");
        g.MapPut("/{code}", UpdateAsync).RequirePermission("m_partner.Edit");
        g.MapDelete("/{code}", DeleteAsync).RequirePermission("m_partner.Delete");
        app.MapGet("/export/partners", ExportCsvAsync).WithTags("Export").RequireAuthorization().RequirePermission("m_partner.Export");
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IPartnerService service, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null, [FromQuery] string? q = null, [FromQuery] string? sort = null,
        [FromQuery] bool? isActive = null, [FromQuery] bool? isSupplier = null, [FromQuery] bool? isCustomer = null) =>
        (await service.ListAsync(ct: ct, page: page, pageSize: pageSize, q: q, sort: sort, isActive: isActive, isSupplier: isSupplier, isCustomer: isCustomer)).ToHttpResult();

    private static async Task<IResult> GetAsync(
        [FromServices] IPartnerService service, string code, CancellationToken ct) =>
        (await service.GetAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> CreateAsync(
        [FromServices] IPartnerService service, [FromBody] PartnerWriteDto body, CancellationToken ct) =>
        (await service.CreateAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        [FromServices] IPartnerService service, string code, [FromBody] PartnerWriteDto body, CancellationToken ct) =>
        (await service.UpdateAsync(code: code, body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> DeleteAsync(
        [FromServices] IPartnerService service, string code, CancellationToken ct) =>
        (await service.DeleteAsync(code: code, ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportCsvAsync([FromServices] IPartnerService service, CancellationToken ct) =>
        (await service.ExportCsvAsync(ct: ct)).ToHttpResult();
}
