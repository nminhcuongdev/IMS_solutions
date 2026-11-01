using Ims.Api.Contracts;
using Ims.Api.Features.LanguageData;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.System;
using Xunit;
using LanguageDataRow = Ims.Domain.System.LanguageData;

namespace Ims.Api.Tests.Languages;

public class LanguageListTests
{
    private const string KoIcon = "data:image/png;base64,iVBORw0KGgo=";

    private static void AddLanguage(TestDb t, string code, string name, string? icon = null,
        bool active = true, bool deleted = false, int minutesAfter = 1)
    {
        t.Db.Languages.Add(new Language
        {
            LanguageCode = code, LanguageName = name, IconImage = icon,
            IsActive = active, IsDeleted = deleted,
            CreatedAt = new DateTime(2026, 1, 1).AddMinutes(minutesAfter),
        });
        t.Db.SaveChanges();
    }

    private static List<LanguageOptionDto> Options(ServiceResult r) =>
        ResultAssert.Ok<ApiSuccess<IReadOnlyList<LanguageOptionDto>>>(r).Data!.ToList();

    [Fact]
    public async Task Every_active_language_is_offered_with_its_icon_in_the_order_it_was_added()
    {
        using var t = TestDb.Empty();
        AddLanguage(t, "vi", "Tiếng Việt", "data:image/png;base64,VI", minutesAfter: 3);
        AddLanguage(t, "en", "English", "data:image/png;base64,EN", minutesAfter: 1);
        AddLanguage(t, "ko", "한국어", KoIcon, minutesAfter: 9);

        var list = Options(await new LanguageDataService(t.Db).LanguagesAsync(default));

        Assert.Equal(new[] { "en", "vi", "ko" }, list.Select(l => l.LanguageCode));
        Assert.Equal("한국어", list[2].LanguageName);
        Assert.Equal(KoIcon, list[2].IconImage);
    }

    [Fact]
    public async Task Inactive_and_deleted_languages_are_not_offered()
    {
        using var t = TestDb.Empty();
        AddLanguage(t, "en", "English");
        AddLanguage(t, "ko", "한국어", active: false);
        AddLanguage(t, "zh", "中文", deleted: true);

        var list = Options(await new LanguageDataService(t.Db).LanguagesAsync(default));

        Assert.Equal(new[] { "en" }, list.Select(l => l.LanguageCode));
    }

    [Fact]
    public async Task A_language_without_an_icon_is_still_offered()
    {
        using var t = TestDb.Empty();
        AddLanguage(t, "th", "ไทย");

        var list = Options(await new LanguageDataService(t.Db).LanguagesAsync(default));

        Assert.Null(Assert.Single(list).IconImage);
    }

    [Fact]
    public async Task Labels_can_be_read_for_a_language_added_later()
    {
        using var t = TestDb.Seeded();
        AddLanguage(t, "ko", "한국어", KoIcon);
        t.Db.LanguageData.Add(new LanguageDataRow
        {
            LabelKey = "htLogin", LanguageCode = "ko", LabelText = "로그인", CreatedAt = DateTime.UtcNow,
        });
        t.Db.SaveChanges();

        var res = await new LanguageDataService(t.Db).LabelsAsync(default, lang: "KO", deviceScope: "HT");

        var map = ResultAssert.Ok<ApiSuccess<Dictionary<string, string>>>(res).Data!;
        Assert.Equal("로그인", map["htLogin"]);
    }

    [Fact]
    public async Task Labels_for_a_language_not_in_the_table_are_refused()
    {
        using var t = TestDb.Seeded();

        var res = await new LanguageDataService(t.Db).LabelsAsync(default, lang: "ko", deviceScope: "PC");

        ResultAssert.BadRequest(res, "VAL-005");
    }

    [Fact]
    public async Task Labels_for_an_inactive_language_are_refused()
    {
        using var t = TestDb.Seeded();
        AddLanguage(t, "ko", "한국어", active: false);

        var res = await new LanguageDataService(t.Db).LabelsAsync(default, lang: "ko", deviceScope: "PC");

        ResultAssert.BadRequest(res, "VAL-005");
    }

    [Fact]
    public async Task Text_for_a_language_added_later_can_be_saved()
    {
        using var t = TestDb.Seeded();
        AddLanguage(t, "ko", "한국어");

        var res = await new LanguageDataService(t.Db).SaveAsync(
            new LanguageSaveDto(new List<LanguageItemDto> { new("htLogin", "ko", "로그인", "HT") }), default);

        Assert.Equal(200, res.StatusCode);
        Assert.Contains(t.Db.LanguageData, d => d.LanguageCode == "ko" && d.LabelText == "로그인");
    }

    [Fact]
    public async Task Text_for_a_language_not_in_the_table_is_refused()
    {
        using var t = TestDb.Seeded();

        var res = await new LanguageDataService(t.Db).SaveAsync(
            new LanguageSaveDto(new List<LanguageItemDto> { new("htLogin", "ko", "로그인", "HT") }), default);

        ResultAssert.BadRequest(res, "VAL-005");
    }
}
