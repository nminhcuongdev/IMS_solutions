using Ims.Api.Contracts;

namespace Ims.Api.Features.Inventory;

internal static class LotRules
{
    public static readonly string[] Placeholders = ["-", "NA", "N/A", "0"];

    public static LotCheck Check(bool isLotManaged, string? lot, bool required)
    {
        if (!isLotManaged) return new LotCheck(null, null);
        var value = string.IsNullOrWhiteSpace(lot) ? null : lot.Trim();
        if (value is null) return new LotCheck(null, required ? ErrorCatalog.Lot001() : null);
        if (Placeholders.Contains(value, StringComparer.OrdinalIgnoreCase))
            return new LotCheck(null, ErrorCatalog.Val005PlaceholderLot());
        return new LotCheck(value, null);
    }
}
