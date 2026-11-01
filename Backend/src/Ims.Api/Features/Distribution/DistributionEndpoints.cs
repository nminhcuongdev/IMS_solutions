using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Ims.Api.Contracts;
using Ims.Api.Http;

namespace Ims.Api.Features.Distribution;

public static class DistributionEndpoints
{
    private const long UploadCeiling = 1024L * 1024 * 1024;

    public static IEndpointRouteBuilder MapDistributionEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/distribution").WithTags("Distribution").RequireAuthorization();
        g.MapGet("/packages", ListPackagesAsync);
        g.MapPost("/packages", UploadAsync).DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(UploadCeiling))
            .WithFormOptions(multipartBodyLengthLimit: UploadCeiling);
        g.MapDelete("/packages/{id:int}", DeletePackageAsync);
        g.MapGet("/attributes", AttributesAsync);
        g.MapPost("/targets/preview", PreviewAsync);
        g.MapGet("/rules", ListRulesAsync);
        g.MapPost("/rules", CreateRuleAsync);
        g.MapGet("/rules/{id:int}", GetRuleAsync);
        g.MapPost("/rules/{id:int}/stop", StopAsync);
        g.MapPost("/rules/{id:int}/resume", ResumeAsync);
        g.MapPost("/rules/{id:int}/retry", RetryAsync);
        g.MapDelete("/rules/{id:int}", DeleteRuleAsync);
        g.MapGet("/devices", ListDevicesAsync);
        g.MapGet("/devices/{code}/history", HistoryAsync);
        g.MapGet("/unknown-devices", UnknownAsync);
        g.MapGet("/audit", AuditAsync);
        g.MapGet("/settings", GetSettingsAsync);
        g.MapPut("/settings", SaveSettingsAsync);
        g.MapGet("/version-json", VersionJsonAsync);
        return app;
    }

    private static async Task<IResult> UploadAsync(
        [FromServices] IDistributionService service, IFormFile? file, ClaimsPrincipal user, CancellationToken ct)
    {
        if (file is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Sys004())).ToHttpResult();
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return (await service.UploadPackageAsync(file.FileName, ms.ToArray(), user, ct)).ToHttpResult();
    }

    private static async Task<IResult> ListPackagesAsync([FromServices] IDistributionService service, CancellationToken ct) =>
        (await service.ListPackagesAsync(ct)).ToHttpResult();

    private static async Task<IResult> DeletePackageAsync(
        [FromServices] IDistributionService service, int id, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.DeletePackageAsync(id, user, ct)).ToHttpResult();

    private static async Task<IResult> AttributesAsync([FromServices] IDistributionService service, CancellationToken ct) =>
        (await service.AttributesAsync(ct)).ToHttpResult();

    private static async Task<IResult> PreviewAsync(
        [FromServices] IDistributionService service, [FromBody] TargetQueryDto body, CancellationToken ct) =>
        (await service.PreviewTargetsAsync(body, ct)).ToHttpResult();

    private static async Task<IResult> ListRulesAsync([FromServices] IDistributionService service, CancellationToken ct) =>
        (await service.ListRulesAsync(ct)).ToHttpResult();

    private static async Task<IResult> CreateRuleAsync(
        [FromServices] IDistributionService service, [FromBody] RuleWriteDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.CreateRuleAsync(body, user, ct)).ToHttpResult();

    private static async Task<IResult> GetRuleAsync([FromServices] IDistributionService service, int id, CancellationToken ct) =>
        (await service.GetRuleAsync(id, ct)).ToHttpResult();

    private static async Task<IResult> StopAsync(
        [FromServices] IDistributionService service, int id, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.StopRuleAsync(id, user, ct)).ToHttpResult();

    private static async Task<IResult> ResumeAsync(
        [FromServices] IDistributionService service, int id, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.ResumeRuleAsync(id, user, ct)).ToHttpResult();

    private static async Task<IResult> RetryAsync(
        [FromServices] IDistributionService service, int id, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.RetryRuleAsync(id, user, ct)).ToHttpResult();

    private static async Task<IResult> DeleteRuleAsync(
        [FromServices] IDistributionService service, int id, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.DeleteRuleAsync(id, user, ct)).ToHttpResult();

    private static async Task<IResult> ListDevicesAsync(
        [FromServices] IDistributionService service, CancellationToken ct, [FromQuery] string? status = null) =>
        (await service.ListDevicesAsync(status, ct)).ToHttpResult();

    private static async Task<IResult> HistoryAsync([FromServices] IDistributionService service, string code, CancellationToken ct) =>
        (await service.DeviceHistoryAsync(code, ct)).ToHttpResult();

    private static async Task<IResult> UnknownAsync([FromServices] IDistributionService service, CancellationToken ct) =>
        (await service.ListUnknownAsync(ct)).ToHttpResult();

    private static async Task<IResult> AuditAsync([FromServices] IDistributionService service, CancellationToken ct) =>
        (await service.ListAuditAsync(ct)).ToHttpResult();

    private static async Task<IResult> GetSettingsAsync([FromServices] IDistributionService service, CancellationToken ct) =>
        (await service.GetSettingsAsync(ct)).ToHttpResult();

    private static async Task<IResult> SaveSettingsAsync(
        [FromServices] IDistributionService service, [FromBody] DistSettingsDto body, ClaimsPrincipal user, CancellationToken ct) =>
        (await service.SaveSettingsAsync(body, user, ct)).ToHttpResult();

    private static async Task<IResult> VersionJsonAsync([FromServices] IDistributionService service, CancellationToken ct) =>
        (await service.VersionJsonPreviewAsync(ct)).ToHttpResult();
}
