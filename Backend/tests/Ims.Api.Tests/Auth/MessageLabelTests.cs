using Ims.Api.Contracts;
using Ims.Api.Features.LanguageData;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.System;
using Xunit;

namespace Ims.Api.Tests.Auth;

public class MessageLabelTests
{
    private static readonly string[] EmittedCodes =
    {
        "VAL-001", "VAL-002", "VAL-005", "SEL-002", "SEL-006", "QTY-001", "RSN-001",
        "DUP-001", "DUP-002", "LOT-001", "LOT-002", "LOC-001", "SCN-006",
        "AUTH-001", "AUTH-003", "AUTH-004", "AUTH-005", "SYS-004", "SYS-404",
    };

    private static readonly string[] DeviceCodes =
    {
        "AUTH-001", "AUTH-002", "NET-002", "PRN-001", "SCN-001", "SCN-005", "SCN-007",
        "STK-005", "VAL-004", "QTY-001", "RSN-001", "LOC-001", "LOT-001", "SYS-404",
    };

    public static TheoryData<string> AllCodes()
    {
        var d = new TheoryData<string>();
        foreach (var c in EmittedCodes.Union(DeviceCodes).Distinct()) d.Add(c);
        return d;
    }

    private static void Seed(TestDb t)
    {
        foreach (var code in EmittedCodes.Union(DeviceCodes).Distinct())
            foreach (var lang in new[] { "en", "ja", "vi" })
                t.Db.LanguageData.Add(new LanguageData
                {
                    LabelKey = code, LanguageCode = lang, LabelText = $"{code}:{lang}",
                    DeviceScope = null, CreatedAt = DateTime.UtcNow,
                });
        t.Db.SaveChanges();
    }

    [Theory]
    [MemberData(nameof(AllCodes))]
    public async Task Every_error_code_reaches_the_handheld(string code)
    {
        using var t = TestDb.Seeded();
        Seed(t);

        var res = await new LanguageDataService(t.Db).LabelsAsync(default, lang: "vi", deviceScope: "HT");

        var map = ResultAssert.Ok<ApiSuccess<Dictionary<string, string>>>(res).Data!;
        Assert.True(map.ContainsKey(code), $"máy cầm tay không có chữ cho mã \"{code}\"");
    }

    [Fact]
    public async Task The_pc_receives_them_too()
    {
        using var t = TestDb.Seeded();
        Seed(t);

        var res = await new LanguageDataService(t.Db).LabelsAsync(default, lang: "vi", deviceScope: "PC");

        var map = ResultAssert.Ok<ApiSuccess<Dictionary<string, string>>>(res).Data!;
        Assert.Equal("AUTH-005:vi", map["AUTH-005"]);
    }

    [Fact]
    public void The_catalog_still_emits_the_codes_the_labels_are_keyed_on()
    {
        Assert.Equal("QTY-001", ErrorCatalog.Qty001().Code);
        Assert.Equal("RSN-001", ErrorCatalog.Rsn001().Code);
        Assert.Equal("LOC-001", ErrorCatalog.Loc001().Code);
        Assert.Equal("DUP-001", ErrorCatalog.Dup001().Code);
        Assert.Equal("VAL-001", ErrorCatalog.Val001().Code);
        Assert.Equal("SYS-404", ErrorCatalog.NotFound("Stock").Code);
    }
}
