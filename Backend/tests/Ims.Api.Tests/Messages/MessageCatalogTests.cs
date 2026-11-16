using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Ims.Api.Features.LanguageData;
using Xunit;

namespace Ims.Api.Tests.Messages;

public class MessageCatalogTests
{
    private static readonly MessageSnapshot Snapshot = new(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["en\u0001VAL-001"] = "{0} is required.",
            ["ja\u0001VAL-001"] = "{0}ã¯å¿…é ˆã§ã™ã€‚",
            ["en\u0001fldFactoryCode"] = "Factory Code",
            ["ja\u0001fldFactoryCode"] = "å·¥å ´ã‚³ãƒ¼ãƒ‰",
        },
        new HashSet<string>(["en", "ja", "vi"], StringComparer.OrdinalIgnoreCase), "ja", DateTimeOffset.Now);

    private static HttpContext Http(string? lang = null, string? acceptLanguage = null)
    {
        var http = new DefaultHttpContext();
        if (lang is not null) http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("lang", lang)], "test"));
        if (acceptLanguage is not null) http.Request.Headers.AcceptLanguage = acceptLanguage;
        return http;
    }

    [Fact]
    public void The_token_language_wins_over_the_header()
    {
        Assert.Equal("vi", MessageCatalog.Resolve(Http("vi", "en-US"), Snapshot));
    }

    [Fact]
    public void Without_a_token_the_header_then_the_system_default_decide()
    {
        Assert.Equal("en", MessageCatalog.Resolve(Http(null, "en-US,ja;q=0.5"), Snapshot));
        Assert.Equal("ja", MessageCatalog.Resolve(Http(null, "fr-FR"), Snapshot));
    }

    [Fact]
    public void A_missing_translation_falls_back_to_English_and_placeholders_are_filled()
    {
        var template = Snapshot.Find("vi", "VAL-001")!;
        var field = Snapshot.Find("vi", "fldFactoryCode")!;

        Assert.Equal("Factory Code is required.", MessageCatalog.Fill(template, [field]));
        Assert.Equal("å·¥å ´ã‚³ãƒ¼ãƒ‰ã¯å¿…é ˆã§ã™ã€‚", MessageCatalog.Fill(Snapshot.Find("ja", "VAL-001")!, [Snapshot.Find("ja", "fldFactoryCode")!]));
    }
}
