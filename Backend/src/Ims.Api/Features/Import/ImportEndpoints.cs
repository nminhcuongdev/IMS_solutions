using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Ims.Api.Http;

namespace Ims.Api.Features.Import;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/import").WithTags("Import").RequireAuthorization();
        g.MapPost("/receiving-schedule", ImportReceivingScheduleAsync).DisableAntiforgery();
        g.MapGet("/receiving-schedule/template.xlsx", ReceivingTemplateAsync);
        g.MapPost("/shipping-schedule", ImportShippingScheduleAsync).DisableAntiforgery();
        g.MapGet("/shipping-schedule/template.xlsx", ShippingTemplateAsync);
        g.MapPost("/returns-schedule", ImportReturnsScheduleAsync).DisableAntiforgery();
        g.MapGet("/returns-schedule/template.xlsx", ReturnsTemplateAsync);
        g.MapPost("/picking-schedule", ImportPickingScheduleAsync).DisableAntiforgery();
        g.MapPost("/production-order", ImportProductionOrderAsync).DisableAntiforgery();
        g.MapGet("/picking-schedule/template.xlsx", PickingTemplateAsync);
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
