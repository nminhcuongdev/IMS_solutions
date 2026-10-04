namespace Ims.Api.Hosting;

internal static class JwtKeyPolicy
{
    public const int MinimumLength = 64;

    private static readonly string[] PlaceholderMarkers = ["change-me", "dev-only"];

    public static bool IsAcceptableForProduction(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length < MinimumLength) return false;
        return !PlaceholderMarkers.Any(m => key.Contains(m, StringComparison.OrdinalIgnoreCase));
    }
}
