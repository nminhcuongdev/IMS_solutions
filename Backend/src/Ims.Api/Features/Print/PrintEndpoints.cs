using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Http;

namespace Ims.Api.Features.Print;

public static class PrintEndpoints
{
    public static IEndpointRouteBuilder MapPrintEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/print").WithTags("Print").RequireAuthorization();
        g.MapPost("/preview", PreviewAsync);
        g.MapPost("/label", LabelAsync);
        return app;
    }

    private static async Task<IResult> PreviewAsync(
        [FromServices] IPrintService service, [FromBody] PrintPreviewRequest body, CancellationToken ct) =>
        (await service.PreviewAsync(body: body, ct: ct)).ToHttpResult();

    private static async Task<IResult> LabelAsync(
        [FromServices] IPrintService service, [FromBody] PrintLabelRequest body, ClaimsPrincipal user,
        CancellationToken ct) =>
        (await service.LabelAsync(body: body, user: user, ct: ct)).ToHttpResult();
}
