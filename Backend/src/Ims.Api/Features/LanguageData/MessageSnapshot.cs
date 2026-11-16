namespace Ims.Api.Features.LanguageData;

internal sealed record MessageSnapshot(
    Dictionary<string, string> Texts, HashSet<string> Languages, string? DefaultLanguage, DateTimeOffset LoadedAt)
{
    public bool Supports(string? lang) => !string.IsNullOrWhiteSpace(lang) && Languages.Contains(lang);

    public string? Find(string lang, string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (Texts.TryGetValue(lang + "\u0001" + key, out var text) && !string.IsNullOrWhiteSpace(text)) return text;
        if (Texts.TryGetValue(MessageCatalog.Fallback + "\u0001" + key, out var en) && !string.IsNullOrWhiteSpace(en)) return en;
        return null;
    }
}
