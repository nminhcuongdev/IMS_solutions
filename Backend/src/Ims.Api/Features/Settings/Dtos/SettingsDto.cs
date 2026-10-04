namespace Ims.Api.Features.Settings;

public sealed record SettingsDto(
    string InvAggDefault,
    string? MasterPrinterDefault,
    string? SchedulePrinterDefault);
