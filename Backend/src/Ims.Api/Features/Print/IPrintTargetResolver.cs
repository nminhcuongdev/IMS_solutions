using Ims.Api.Contracts;

namespace Ims.Api.Features.Print;

internal interface IPrintTargetResolver
{
    string LayoutKeyFor(string category);

    Task<(PrintTarget? Target, ServiceResult? Refused)> ResolveAsync(string? layoutKey, string? deviceCode, CancellationToken ct);
}
