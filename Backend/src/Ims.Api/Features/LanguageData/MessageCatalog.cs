using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.LanguageData;

internal sealed class MessageCatalog(IServiceScopeFactory scopes, TimeProvider clock) : IMessageCatalog
{
    public const string Fallback = "en";
    public const string DefaultLanguageSetting = "default_language";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim gate = new(1, 1);
    private MessageSnapshot? current;

    public async Task<ApiErrorBody> LocalizeAsync(ApiErrorBody body, HttpContext http, CancellationToken ct)
    {
        MessageSnapshot snapshot;
        try
        {
            snapshot = await LoadAsync(ct);
        }
        catch (Exception)
        {
            return body;
        }

        var lang = Resolve(http, snapshot);
        var template = snapshot.Find(lang, body.MessageKey) ?? snapshot.Find(lang, body.Code);
        if (template is null) return body;
        var args = (body.Args ?? []).Select(a => snapshot.Find(lang, a) ?? a).ToArray();
        return body with { Message = Fill(template, args) };
    }

    public static string Fill(string template, IReadOnlyList<string> args)
    {
        var text = template;
        for (var i = 0; i < args.Count; i++)
            text = text.Replace("{" + i.ToString(CultureInfo.InvariantCulture) + "}", args[i], StringComparison.Ordinal);
        return text;
    }

    public static string Resolve(HttpContext http, MessageSnapshot snapshot)
    {
        var claim = http.User.FindFirst("lang")?.Value;
        if (snapshot.Supports(claim)) return claim!.ToLowerInvariant();
        foreach (var part in http.Request.Headers.AcceptLanguage.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var tag = part.Split(';')[0].Trim();
            var primary = tag.Split('-')[0];
            if (snapshot.Supports(primary)) return primary.ToLowerInvariant();
        }
        if (snapshot.Supports(snapshot.DefaultLanguage)) return snapshot.DefaultLanguage!.ToLowerInvariant();
        return Fallback;
    }

    private async Task<MessageSnapshot> LoadAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (current is { } fresh && now - fresh.LoadedAt < Lifetime) return fresh;
        await gate.WaitAsync(ct);
        try
        {
            if (current is { } again && now - again.LoadedAt < Lifetime) return again;
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ImsDbContext>();
            var rows = await db.LanguageData.AsNoTracking()
                .Select(x => new { x.LabelKey, x.LanguageCode, x.LabelText, x.DeviceScope })
                .ToListAsync(ct);
            var languages = await db.Languages.AsNoTracking().Where(l => l.IsActive)
                .Select(l => l.LanguageCode).ToListAsync(ct);
            var setting = await db.GeneralSettings.AsNoTracking()
                .Where(s => s.ConfigCode == DefaultLanguageSetting).Select(s => s.ConfigValue).FirstOrDefaultAsync(ct);
            var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows.OrderBy(r => r.DeviceScope is null ? 1 : 0))
                texts[r.LanguageCode + "\u0001" + r.LabelKey] = r.LabelText;
            current = new MessageSnapshot(texts, languages.Append(Fallback).ToHashSet(StringComparer.OrdinalIgnoreCase), setting, now);
            return current;
        }
        finally
        {
            gate.Release();
        }
    }
}
