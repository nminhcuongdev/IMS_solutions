using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Print;

internal sealed class PrintTargetResolver(ImsDbContext db) : IPrintTargetResolver
{
    private static readonly Dictionary<string, string> Layouts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ReceivingQR"] = "Layout_IMS/ReceivingQR_24dpmm.mllayx",
        ["ReturnsQR"] = "Layout_IMS/ReturnsQR_24dpmm.mllayx",
        ["ShippingQR"] = "Layout_IMS/ShippingQR_24dpmm.mllayx",
    };

    public string LayoutKeyFor(string category) => category switch
    {
        "Receiving" => "ReceivingQR",
        "Returns" => "ReturnsQR",
        "Shipping" => "ShippingQR",
        _ => "",
    };

    public async Task<(PrintTarget? Target, ServiceResult? Refused)> ResolveAsync(
        string? layoutKey, string? deviceCode, CancellationToken ct)
    {
        if (!Layouts.TryGetValue(layoutKey ?? "", out var layoutFile))
            return (null, Outcome.Json(new ApiError(ErrorCatalog.NotFound(layoutKey ?? "")), statusCode: 404));

        var code = (deviceCode ?? "").Trim();
        var device = await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DeviceCode == code && !d.IsDeleted, ct);
        if (device is null)
            return (null, Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Device"))));

        if (string.IsNullOrWhiteSpace(device.IpAddress))
            return (null, Outcome.BadRequest(new ApiError(ErrorCatalog.Val004())));

        return (new PrintTarget(layoutFile, $"LAN:{device.IpAddress.Trim()}"), null);
    }
}
