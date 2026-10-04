using Ims.Api.Contracts;
using Ims.Api.Features.Ht;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Ht;

public class HtPrintersTests
{
    private static Device Dev(string code, DeviceType type = DeviceType.Printer, string? ip = "192.168.100.21",
        bool active = true, bool deleted = false) => new()
    {
        DeviceCode = code, DeviceName = $"Name {code}", DeviceType = type, IpAddress = ip,
        IsActive = active, IsDeleted = deleted, CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task Lists_only_active_printers_that_have_an_ip_address()
    {
        using var t = TestDb.Empty();
        t.Db.Devices.AddRange(
            Dev("PRN-B"),
            Dev("PRN-A"),
            Dev("PRN-NOIP", ip: null),
            Dev("PRN-BLANKIP", ip: "  "),
            Dev("PRN-OFF", active: false),
            Dev("PRN-DEL", deleted: true),
            Dev("HT-01", type: DeviceType.Handheld));
        await t.Db.SaveChangesAsync();

        var res = await new HtService(t.Db).ListPrintersAsync(default);

        var list = ResultAssert.Ok<ApiListSuccess<HtPrinterDto>>(res);
        Assert.Equal(new[] { "PRN-A", "PRN-B" }, list.Data.Select(p => p.DeviceCode));
        Assert.Equal(2, list.Meta.Total);
    }

    [Fact]
    public async Task Returns_the_name_and_trimmed_ip_the_picker_shows()
    {
        using var t = TestDb.Empty();
        t.Db.Devices.Add(Dev("PRN-LAN-01", ip: " 192.168.100.21 "));
        await t.Db.SaveChangesAsync();

        var res = await new HtService(t.Db).ListPrintersAsync(default);

        var p = Assert.Single(ResultAssert.Ok<ApiListSuccess<HtPrinterDto>>(res).Data);
        Assert.Equal("Name PRN-LAN-01", p.DeviceName);
        Assert.Equal("192.168.100.21", p.IpAddress);
    }

    [Fact]
    public async Task An_empty_device_master_is_an_empty_list_not_an_error()
    {
        using var t = TestDb.Empty();

        var res = await new HtService(t.Db).ListPrintersAsync(default);

        Assert.Empty(ResultAssert.Ok<ApiListSuccess<HtPrinterDto>>(res).Data);
    }
}
