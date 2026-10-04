using System.Net;
using System.Net.Sockets;

namespace Ims.Api.Hosting;

internal sealed class FrontendOriginPolicy(IReadOnlyCollection<string> origins, bool allowPrivateNetwork)
{
    public bool IsAllowed(string origin)
    {
        if (origins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
        if (!allowPrivateNetwork) return false;
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (!IPAddress.TryParse(host, out var ip)) return false;
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;

        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168);
    }
}
