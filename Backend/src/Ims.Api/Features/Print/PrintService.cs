using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Api.Features.History;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Print;

internal sealed class PrintService(ImsDbContext db, IMlEngineClient engine, IPrintTargetResolver targets, IHistoryService history) : IPrintService
{
    public async Task<ServiceResult> PreviewAsync(PrintPreviewRequest body, CancellationToken ct)
    {
        var (target, refused) = await targets.ResolveAsync(body.LayoutKey, body.DeviceCode, ct);
        if (refused is not null) return refused;

        try
        {
            var image = await engine.PreviewAsync(target!.Value.LayoutFile, target.Value.Setting, body.Fields, ct);
            return Outcome.Ok(new ApiSuccess<PrintPreviewResponseDto>(new PrintPreviewResponseDto(image)));
        }
        catch (MlEngineException e)
        {
            return Outcome.Json(
                new ApiError(PrintErrorCatalog.For(e.ComponentCode) ?? ErrorCatalog.Prn001()),
                statusCode: 500);
        }
    }

    public async Task<ServiceResult> LabelAsync(PrintLabelRequest body, ClaimsPrincipal user, CancellationToken ct)
    {
        var (target, refused) = await targets.ResolveAsync(body.LayoutKey, body.DeviceCode, ct);
        if (refused is not null) return refused;

        var h = body.History;
        var category = NullIfBlank(h?.Category)?.Trim();
        if (h is null || category is null) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));
        if (h.Qty is not > 0) return Outcome.BadRequest(new ApiError(ErrorCatalog.Qty001()));

        var docNo = NullIfBlank(h.DocNo)?.Trim();
        var itemCode = NullIfBlank(h.ItemCode)?.Trim();
        if (docNo is not null && itemCode is not null
            && await db.PrintHistories.AnyAsync(
                x => x.Category == category && x.DocNo == docNo && x.ItemCode == itemCode, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup002()));

        try
        {
            await engine.PrintAsync(target!.Value.LayoutFile, target.Value.Setting, body.Fields,
                Math.Max(1, body.Copies), ct);
        }
        catch (Exception e) when (e is MlEngineException or HttpRequestException or TaskCanceledException)
        {
            var known = (e as MlEngineException) is { } ml ? PrintErrorCatalog.For(ml.ComponentCode) : null;
            return Outcome.Json(new ApiError(known ?? ErrorCatalog.Prn002()), statusCode: 500);
        }

        return await history.CreatePrintAsync(h with { Spec = body.DeviceCode.Trim() }, user, ct);
    }

}
