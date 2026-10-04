using Ims.Api.Contracts;
using Ims.Api.Features.LanguageData;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.System;
using Xunit;

namespace Ims.Api.Tests.Auth;

public class HtLabelScopeTests
{
    private static readonly string[] Keys =
    {
        "statusNotArrived", "statusArrived", "statusPartial",
        "statusNotShipped", "statusShipped", "statusPartialShip",
        "statusPendingReturn", "statusReturned", "statusPartialReturn",
        "statusNotPicked", "statusPicked", "statusCancelled",
    };

    public static TheoryData<string> SharedStatusKeys()
    {
        var d = new TheoryData<string>();
        foreach (var k in Keys) d.Add(k);
        return d;
    }

    private static void Seed(TestDb t)
    {
        foreach (var key in Keys)
            foreach (var lang in new[] { "en", "ja", "vi" })
                t.Db.LanguageData.Add(new LanguageData
                {
                    LabelKey = key, LanguageCode = lang, LabelText = $"{key}:{lang}",
                    DeviceScope = null, CreatedAt = DateTime.UtcNow,
                });
        t.Db.SaveChanges();
    }

    [Theory]
    [MemberData(nameof(SharedStatusKeys))]
    public async Task The_handheld_receives_every_shared_status_label(string key)
    {
        using var t = TestDb.Seeded();
        Seed(t);

        var res = await new LanguageDataService(t.Db).LabelsAsync(default, lang: "vi", deviceScope: "HT");

        var map = ResultAssert.Ok<ApiSuccess<Dictionary<string, string>>>(res).Data!;
        Assert.True(map.ContainsKey(key), $"máy cầm tay không nhận được nhãn \"{key}\"");
    }

    [Fact]
    public async Task The_pc_still_receives_them_too()
    {
        using var t = TestDb.Seeded();
        Seed(t);

        var res = await new LanguageDataService(t.Db).LabelsAsync(default, lang: "vi", deviceScope: "PC");

        var map = ResultAssert.Ok<ApiSuccess<Dictionary<string, string>>>(res).Data!;
        Assert.Equal("statusArrived:vi", map["statusArrived"]);
    }

    [Fact]
    public async Task A_device_specific_label_still_wins()
    {
        using var t = TestDb.Seeded();
        Seed(t);
        t.Db.LanguageData.Add(new LanguageData
        {
            LabelKey = "statusArrived", LanguageCode = "vi", LabelText = "Đã đến (HT)",
            DeviceScope = "HT", CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new LanguageDataService(t.Db).LabelsAsync(default, lang: "vi", deviceScope: "HT");

        var map = ResultAssert.Ok<ApiSuccess<Dictionary<string, string>>>(res).Data!;
        Assert.Equal("Đã đến (HT)", map["statusArrived"]);
    }
}
