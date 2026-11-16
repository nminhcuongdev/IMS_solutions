using System.Security.Claims;
using Ims.Api.Contracts;
using Ims.Api.Features.Permissions;

namespace Ims.Api.Features.Production;

internal sealed partial class ProductionService
{
    private static readonly Dictionary<string, string> TypePermission = new(StringComparer.OrdinalIgnoreCase)
    {
        ["receipt"] = "ht_prd_receipt",
        ["consume"] = "ht_prd_input",
        ["return"] = "ht_prd_return",
        ["output"] = "ht_prd_output",
        ["label"] = "ht_prd_output",
    };

    public async Task<ServiceResult> RegisterAsync(string orderNo, ProductionResultWriteDto body, ClaimsPrincipal user, CancellationToken ct)
    {
        var type = (body?.Type ?? "").Trim().ToLowerInvariant();
        if (!TypePermission.TryGetValue(type, out var function))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldType", "Type")));
        if (!(await new PermissionEvaluator(db).ReadAsync(user, ct)).Allows(function, "Open"))
            return Outcome.Json(new ApiError(ErrorCatalog.Auth006()), 403);
        var requestId = body!.RequestId ?? "";
        return type switch
        {
            "receipt" => await ReceiptAsync(orderNo, new ReceiptWriteDto(requestId), user, ct),
            "consume" => await ConsumptionAsync(orderNo, new ConsumptionWriteDto(requestId, body.ProcessCode ?? "",
                body.ItemCode ?? "", body.LotNo, body.Qty ?? 0, body.LineNo), user, ct),
            "return" => await ReturnAsync(orderNo, new ReturnWriteDto(requestId, body.ItemCode ?? "", body.LotNo, body.Qty ?? 0), user, ct),
            "output" => await OutputAsync(orderNo, new OutputWriteDto(requestId, body.Qty ?? 0, body.Complete, body.ExpiryDate, body.LotNo), user, ct),
            _ => await LabelAsync(orderNo, new LabelWriteDto(requestId, body.OutputRequestId), user, ct),
        };
    }
}
