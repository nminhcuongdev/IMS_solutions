namespace Ims.Api.Features.LanguageData;

public sealed record LanguageItemDto(string LabelKey, string LanguageCode, string LabelText, string? DeviceScope);
