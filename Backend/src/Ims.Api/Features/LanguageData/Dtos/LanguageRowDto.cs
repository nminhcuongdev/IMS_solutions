namespace Ims.Api.Features.LanguageData;

public sealed record LanguageRowDto(
    string LabelKey,
    string? DeviceScope,
    string? En,
    string? Ja,
    string? Vi);
