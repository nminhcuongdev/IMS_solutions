using Ims.Api.Features.Permissions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Ims.Api.Http;

namespace Ims.Api.Features.Import;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/import").WithTags("Import").RequireAuthorization();
        g.MapPost("/receiving-schedule", ImportReceivingScheduleAsync).DisableAntiforgery().RequireModule("receiving").RequirePermission("rcv_import.Import");
        app.MapGet("/schedules/receiving/template.xlsx", ReceivingTemplateAsync).WithTags("Import").RequireAuthorization().RequireModule("receiving").RequirePermission("rcv_import.Import");
        g.MapPost("/shipping-schedule", ImportShippingScheduleAsync).DisableAntiforgery().RequireModule("shipping").RequirePermission("shp_import.Import");
        app.MapGet("/schedules/shipping/template.xlsx", ShippingTemplateAsync).WithTags("Import").RequireAuthorization().RequireModule("shipping").RequirePermission("shp_import.Import");
        g.MapPost("/returns-schedule", ImportReturnsScheduleAsync).DisableAntiforgery().RequireModule("returns").RequirePermission("returns.Import");
        app.MapGet("/schedules/returns/template.xlsx", ReturnsTemplateAsync).WithTags("Import").RequireAuthorization().RequireModule("returns").RequirePermission("returns.Import");
        g.MapPost("/picking-schedule", ImportPickingScheduleAsync).DisableAntiforgery().RequireModule("picking").RequirePermission("pick_import.Import");
        g.MapPost("/production-order", ImportProductionOrderAsync).DisableAntiforgery().RequireModule("production").RequirePermission("prd_import.Import");
        app.MapGet("/schedules/picking/template.xlsx", PickingTemplateAsync).WithTags("Import").RequireAuthorization().RequireModule("picking").RequirePermission("pick_import.Import");
        return app;
    }

    private static async Task<IResult> ImportReceivingScheduleAsync(
        [FromServices] IImportService service, IFormFile? file, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ImportReceivingScheduleAsync(file: file, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ImportShippingScheduleAsync(
        [FromServices] IImportService service, IFormFile? file, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ImportShippingScheduleAsync(file: file, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ImportReturnsScheduleAsync(
        [FromServices] IImportService service, IFormFile? file, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ImportReturnsScheduleAsync(file: file, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ImportPickingScheduleAsync(
        [FromServices] IImportService service, IFormFile? file, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ImportPickingScheduleAsync(file: file, user: user, ct: ct)).ToHttpResult();

    private static async Task<IResult> ImportProductionOrderAsync(
        [FromServices] IImportService service, IFormFile? file, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ImportProductionOrderAsync(file, user, ct)).ToHttpResult();

    private static IResult ReceivingTemplateAsync([FromServices] IImportService service) =>
        service.ReceivingTemplateAsync().ToHttpResult();

    private static IResult ShippingTemplateAsync([FromServices] IImportService service) =>
        service.ShippingTemplateAsync().ToHttpResult();

    private static IResult ReturnsTemplateAsync([FromServices] IImportService service) =>
        service.ReturnsTemplateAsync().ToHttpResult();

    private static IResult PickingTemplateAsync([FromServices] IImportService service) =>
        service.PickingTemplateAsync().ToHttpResult();
}
