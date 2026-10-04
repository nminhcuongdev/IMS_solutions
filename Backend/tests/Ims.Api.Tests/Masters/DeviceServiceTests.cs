using Ims.Api.Contracts;
using Ims.Api.Features.Devices;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class DeviceServiceTests
{
    private static DeviceWriteDto Valid(
        string code = "DEV-NEW", string name = "New printer",
        string type = "Printer", string? ip = "192.168.10.101",
        string? locationCode = null) =>
        new(code, name, type, IpAddress: ip, LocationCode: locationCode);


    [Fact]
    public async Task Create_persists_row_and_returns_201()
    {
        using var t = TestDb.Seeded();

        var res = await new DeviceService(t.Db).CreateAsync(Valid(), default);

        var dto = ResultAssert.Created<ApiSuccess<DeviceDto>>(res).Data!;
        Assert.Equal("DEV-NEW", dto.DeviceCode);
        Assert.Equal("Printer", dto.DeviceType);
        Assert.Equal("192.168.10.101", dto.IpAddress);
        Assert.NotNull(await t.Db.Devices.FindAsync("DEV-NEW"));
    }

    [Fact]
    public async Task Create_trims_code_and_name()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "  DEV-T  ", name: "  Printer T  ");

        var res = await new DeviceService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<DeviceDto>>(res).Data!;
        Assert.Equal("DEV-T", dto.DeviceCode);
        Assert.Equal("Printer T", dto.DeviceName);
    }

    [Fact]
    public async Task Create_rejects_blank_code()
    {
        using var t = TestDb.Seeded();

        var res = await new DeviceService(t.Db).CreateAsync(Valid(code: " "), default);

        ResultAssert.BadRequest(res, "VAL-001");
    }

    [Fact]
    public async Task Create_rejects_blank_name()
    {
        using var t = TestDb.Seeded();

        var res = await new DeviceService(t.Db).CreateAsync(Valid(name: ""), default);

        ResultAssert.BadRequest(res, "VAL-002");
    }

    [Fact]
    public async Task Create_rejects_duplicate_code()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-A", DeviceName = "A", DeviceType = DeviceType.Printer,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new DeviceService(t.Db).CreateAsync(Valid(code: "DEV-A"), default);

        ResultAssert.Conflict(res);
    }

    [Fact]
    public async Task Create_allows_a_blank_ip_address()
    {
        using var t = TestDb.Seeded();

        var res = await new DeviceService(t.Db).CreateAsync(Valid(ip: null), default);

        var dto = ResultAssert.Created<ApiSuccess<DeviceDto>>(res).Data!;
        Assert.Null(dto.IpAddress);
    }

    [Theory]
    [InlineData("999.1.1.1")]
    [InlineData("1.2.3")]
    [InlineData("1.2.3.4.5")]
    [InlineData("a.b.c.d")]
    public async Task Create_rejects_a_malformed_ip_address(string ip)
    {
        using var t = TestDb.Seeded();

        var res = await new DeviceService(t.Db).CreateAsync(Valid(ip: ip), default);

        ResultAssert.BadRequest(res, "VAL-004");
    }

    [Fact]
    public async Task Create_accepts_a_location_and_rejects_an_unknown_one()
    {
        using var t = TestDb.Seeded();

        var ok = await new DeviceService(t.Db).CreateAsync(Valid(code: "DEV-LOC", locationCode: "W1_ZA"), default);
        var bad = await new DeviceService(t.Db).CreateAsync(Valid(code: "DEV-BAD", locationCode: "NOPE"), default);

        Assert.Equal("W1_ZA", ResultAssert.Created<ApiSuccess<DeviceDto>>(ok).Data!.LocationCode);
        ResultAssert.NotFound(bad);
    }


    [Fact]
    public async Task Get_returns_row_and_404_for_missing_or_deleted()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-DEL", DeviceName = "Gone", DeviceType = DeviceType.Printer,
            IsDeleted = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-A", DeviceName = "Printer A", DeviceType = DeviceType.Printer,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var live = await new DeviceService(t.Db).GetAsync("DEV-A", default);
        var missing = await new DeviceService(t.Db).GetAsync("NOPE", default);
        var deleted = await new DeviceService(t.Db).GetAsync("DEV-DEL", default);

        Assert.Equal("Printer A", ResultAssert.Ok<ApiSuccess<DeviceDto>>(live).Data!.DeviceName);
        ResultAssert.NotFound(missing);
        ResultAssert.NotFound(deleted);
    }

    [Fact]
    public async Task List_filters_by_device_type()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-PR", DeviceName = "Printer", DeviceType = DeviceType.Printer,
            IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-HT", DeviceName = "Handheld", DeviceType = DeviceType.Handheld,
            IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new DeviceService(t.Db).ListAsync(default, deviceType: "Printer");

        var body = ResultAssert.Ok<ApiListSuccess<DeviceDto>>(res);
        Assert.Equal("DEV-PR", Assert.Single(body.Data).DeviceCode);
    }


    [Fact]
    public async Task Update_changes_fields_but_never_the_code()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-A", DeviceName = "Printer A", DeviceType = DeviceType.Printer,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
        var body = new DeviceWriteDto("HACKED", "Renamed", "Printer", IpAddress: "192.168.10.200", IsActive: false);

        var res = await new DeviceService(t.Db).UpdateAsync("DEV-A", body, default);

        var dto = ResultAssert.Ok<ApiSuccess<DeviceDto>>(res).Data!;
        Assert.Equal("DEV-A", dto.DeviceCode);
        Assert.Equal("Renamed", dto.DeviceName);
        Assert.Equal("192.168.10.200", dto.IpAddress);
        Assert.False(dto.IsActive);
        Assert.NotNull(dto.UpdatedAt);
        Assert.Null(await t.Db.Devices.FindAsync("HACKED"));
    }

    [Fact]
    public async Task Update_rejects_a_malformed_ip_and_unknown_row()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-A", DeviceName = "Printer A", DeviceType = DeviceType.Printer,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var badIp = await new DeviceService(t.Db).UpdateAsync("DEV-A", Valid(ip: "bad"), default);
        var missing = await new DeviceService(t.Db).UpdateAsync("NOPE", Valid(), default);

        ResultAssert.BadRequest(badIp, "VAL-004");
        ResultAssert.NotFound(missing);
    }


    [Fact]
    public async Task Delete_soft_deletes_the_row()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-A", DeviceName = "Printer A", DeviceType = DeviceType.Printer,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new DeviceService(t.Db).DeleteAsync("DEV-A", default);

        ResultAssert.NoContent(res);
        var row = await t.Db.Devices.FindAsync("DEV-A");
        Assert.True(row!.IsDeleted);
    }

    [Fact]
    public async Task Delete_returns_404_for_unknown_row()
    {
        using var t = TestDb.Seeded();

        var res = await new DeviceService(t.Db).DeleteAsync("NOPE", default);

        ResultAssert.NotFound(res);
    }


    [Fact]
    public async Task ExportCsv_lists_live_rows_only()
    {
        using var t = TestDb.Seeded();
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-A", DeviceName = "Printer A", DeviceType = DeviceType.Printer,
            CreatedAt = DateTime.UtcNow,
        });
        t.Db.Devices.Add(new Device
        {
            DeviceCode = "DEV-DEL", DeviceName = "Gone", DeviceType = DeviceType.Printer,
            IsDeleted = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new DeviceService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        Assert.Contains("DEV-A", csv);
        Assert.DoesNotContain("DEV-DEL", csv);
    }
}
