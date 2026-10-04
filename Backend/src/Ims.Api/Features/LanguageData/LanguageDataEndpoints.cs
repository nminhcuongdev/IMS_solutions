using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.LanguageData;

public static class LanguageDataEndpoints
{
    public static IEndpointRouteBuilder MapLanguageDataEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/language-data").WithTags("Language Data").RequireAuthorization();
        g.MapGet("", ListAsync);
        g.MapPut("", SaveAsync);
        g.MapGet("/export.csv", ExportCsvAsync);
        g.MapGet("/labels", LabelsAsync).AllowAnonymous();
        app.MapGet("/languages", SlotsAsync).WithTags("Language Data").AllowAnonymous();
        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] ILanguageDataService service, CancellationToken ct, [FromQuery] string? q = null,
        [FromQuery] string? labelKey = null, [FromQuery] string? deviceScope = null, [FromQuery] string? sort = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null) =>
        (await service.ListAsync(ct: ct, q: q, labelKey: labelKey, deviceScope: deviceScope, sort: sort, page: page, pageSize: pageSize)).ToHttpResult();

    private static async Task<IResult> SaveAsync(
        [FromServices] ILanguageDataService service, LanguageSaveDto body, CancellationToken ct) =>
        (await service.SaveAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> LabelsAsync(
        [FromServices] ILanguageDataService service, CancellationToken ct, [FromQuery] string? lang = null,
        [FromQuery] string deviceScope = "PC") =>
        (await service.LabelsAsync(ct: ct, lang: lang, deviceScope: deviceScope)).ToHttpResult();

    private static async Task<IResult> SlotsAsync([FromServices] ILanguageDataService service, CancellationToken ct) =>
        (await service.SlotsAsync(ct: ct)).ToHttpResult();

    private static async Task<IResult> ExportCsvAsync(
        [FromServices] ILanguageDataService service, CancellationToken ct, [FromQuery] string? q = null,
        [FromQuery] string? labelKey = null, [FromQuery] string? deviceScope = null, [FromQuery] string? sort = null) =>
        (await service.ExportCsvAsync(ct: ct, q: q, labelKey: labelKey, deviceScope: deviceScope, sort: sort)).ToHttpResult();
}
