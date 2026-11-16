namespace Ims.Api.Features.StockScan;

internal static class EpcRules
{
    public const int PrefixLength = 5;
    public const int MinLength = PrefixLength;
    public const int MaxLength = 24;

    public static string? Normalize(string? raw)
    {
        var s = raw?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    public static bool IsValid(string epc) =>
        epc.Length is >= MinLength and <= MaxLength && epc.All(Uri.IsHexDigit);

    public static string PrefixOf(string epc) => epc[..PrefixLength];
}
