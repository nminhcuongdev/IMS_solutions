namespace Ims.Api.Features.Inventory;

internal static class ExpiryRules
{
    public static bool IsExpired(DateOnly? expiry, DateTime now) =>
        expiry is { } e && e < DateOnly.FromDateTime(now);
}
