using Ims.Api.Contracts;
using Ims.Api.Features.Locations;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class LocationServiceTests
{
    private static LocationWriteDto Large(string input = "W2", string name = "Warehouse 2", string? zone = "ZONE-A") =>
        new("Large", input, name, ParentLocationCode: null, ZoneCode: zone);

    private static LocationWriteDto Middle(string input, string parent, string name = "Aisle") =>
        new("Middle", input, name, ParentLocationCode: parent);

    private static LocationWriteDto Small(string input, string parent, string name = "Shelf") =>
        new("Small", input, name, ParentLocationCode: parent);


    [Fact]
    public async Task Create_Large_uses_the_input_code_verbatim()
    {
        using var t = TestDb.Seeded();
        var body = Large();

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<LocationDto>>(res).Data!;
        Assert.Equal("W2", dto.LocationCode);
        Assert.Equal("W2", dto.LargeCode);
        Assert.Null(dto.MiddleCode);
        Assert.Null(dto.SmallCode);
        Assert.Null(dto.ParentLocationCode);
    }

    [Fact]
    public async Task Create_Middle_composes_parent_underscore_input()
    {
        using var t = TestDb.Seeded();
        var body = Middle("ZB", "W1");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<LocationDto>>(res).Data!;
        Assert.Equal("W1_ZB", dto.LocationCode);
        Assert.Equal("W1", dto.LargeCode);
        Assert.Equal("ZB", dto.MiddleCode);
        Assert.Null(dto.SmallCode);
        Assert.Equal("W1", dto.ParentLocationCode);
    }

    [Fact]
    public async Task Create_Small_inherits_large_and_middle_from_the_parent()
    {
        using var t = TestDb.Seeded();
        var body = Small("S1", "W1_ZA");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<LocationDto>>(res).Data!;
        Assert.Equal("W1_ZA_S1", dto.LocationCode);
        Assert.Equal("W1", dto.LargeCode);
        Assert.Equal("ZA", dto.MiddleCode);
        Assert.Equal("S1", dto.SmallCode);
        Assert.Equal("W1_ZA", dto.ParentLocationCode);
    }

    [Fact]
    public async Task Create_rejects_a_composed_code_longer_than_the_column()
    {
        using var t = TestDb.Seeded();
        var body = Middle(new string('X', 18), "W1");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-005");
    }


    [Fact]
    public async Task Create_Middle_requires_a_parent()
    {
        using var t = TestDb.Seeded();
        var body = new LocationWriteDto("Middle", "ZB", "Aisle");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-002");
    }

    [Fact]
    public async Task Create_Middle_requires_the_parent_to_be_Large()
    {
        using var t = TestDb.Seeded();
        var body = Middle("ZB", "W1_ZA");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-005");
    }

    [Fact]
    public async Task Create_Small_requires_the_parent_to_be_Middle()
    {
        using var t = TestDb.Seeded();
        var body = Small("S1", "W1");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-005");
    }

    [Fact]
    public async Task Create_rejects_an_unknown_parent()
    {
        using var t = TestDb.Seeded();
        var body = Middle("ZB", "NOPE");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_location_type()
    {
        using var t = TestDb.Seeded();
        var body = new LocationWriteDto("Gigantic", "X", "X");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-002");
    }

    [Theory]
    [InlineData("large")]
    [InlineData("MIDDLE")]
    public async Task Create_accepts_the_type_in_any_casing(string type)
    {
        using var t = TestDb.Seeded();
        var body = type.Equals("large", StringComparison.OrdinalIgnoreCase)
            ? new LocationWriteDto(type, "W9", "Warehouse 9")
            : new LocationWriteDto(type, "ZB", "Aisle", ParentLocationCode: "W1");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        ResultAssert.Created<ApiSuccess<LocationDto>>(res);
    }


    [Fact]
    public async Task Create_inherits_the_zone_from_the_parent_when_none_is_given()
    {
        using var t = TestDb.Seeded();
        var body = Middle("ZB", "W1");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<LocationDto>>(res).Data!;
        Assert.Equal("ZONE-A", dto.ZoneCode);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_zone()
    {
        using var t = TestDb.Seeded();
        var body = Large(zone: "NOPE");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task Create_treats_a_blank_zone_and_blank_parent_as_null()
    {
        using var t = TestDb.Seeded();
        var body = new LocationWriteDto("Large", "W3", "Warehouse 3", ParentLocationCode: "", ZoneCode: "");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<LocationDto>>(res).Data!;
        Assert.Null(dto.ZoneCode);
        Assert.Null(dto.ParentLocationCode);
    }

    [Fact]
    public async Task Update_clears_the_zone_when_the_body_leaves_it_blank()
    {
        using var t = TestDb.Seeded();
        Assert.Equal("ZONE-A", (await t.Db.Locations.FindAsync("W1"))!.ZoneCode);
        t.Db.ChangeTracker.Clear();
        var body = new LocationWriteDto("Large", "W1", "Warehouse 1", ZoneCode: "");

        var res = await new LocationService(t.Db).UpdateAsync("W1", body, default);

        var dto = ResultAssert.Ok<ApiSuccess<LocationDto>>(res).Data!;
        Assert.Null(dto.ZoneCode);
    }

    [Fact]
    public async Task Update_rejects_an_unknown_zone()
    {
        using var t = TestDb.Seeded();
        var body = new LocationWriteDto("Large", "W1", "Warehouse 1", ZoneCode: "NOPE");

        var res = await new LocationService(t.Db).UpdateAsync("W1", body, default);

        ResultAssert.NotFound(res);
    }


    [Fact]
    public async Task Create_rejects_a_duplicate_composed_code()
    {
        using var t = TestDb.Seeded();
        var body = Middle("ZA", "W1");

        var res = await new LocationService(t.Db).CreateAsync(body, default);

        ResultAssert.Conflict(res);
    }

    [Fact]
    public async Task Get_returns_the_row_and_404_when_missing()
    {
        using var t = TestDb.Seeded();

        var found = await new LocationService(t.Db).GetAsync("W1", default);
        var missing = await new LocationService(t.Db).GetAsync("NOPE", default);

        Assert.Equal("Warehouse 1", ResultAssert.Ok<ApiSuccess<LocationDto>>(found).Data!.LocationName);
        ResultAssert.NotFound(missing);
    }

    [Fact]
    public async Task List_sorts_by_the_decomposed_segments()
    {
        using var t = TestDb.Seeded();

        var res = await new LocationService(t.Db).ListAsync(default, sort: "-middle_code");

        var body = ResultAssert.Ok<ApiListSuccess<LocationDto>>(res);
        Assert.Equal("W1_ZA", body.Data[0].LocationCode);
    }

    [Fact]
    public async Task List_filters_by_zone()
    {
        using var t = TestDb.Seeded();
        t.Db.Zones.Add(new Zone { ZoneCode = "ZONE-B", ZoneName = "Zone B", FactoryCode = "FAC-01", IsActive = true });
        t.Db.Locations.Add(new Location
        {
            LocationCode = "W2", LocationType = LocationType.Large, LargeCode = "W2",
            LocationName = "Warehouse 2", ZoneCode = "ZONE-B", IsActive = true,
        });
        await t.Db.SaveChangesAsync();

        var res = await new LocationService(t.Db).ListAsync(default, zoneCode: "ZONE-B");

        var body = ResultAssert.Ok<ApiListSuccess<LocationDto>>(res);
        Assert.Equal(new[] { "W2" }, body.Data.Select(x => x.LocationCode).ToArray());
    }

    [Fact]
    public async Task List_ignores_a_blank_zone()
    {
        using var t = TestDb.Seeded();

        var res = await new LocationService(t.Db).ListAsync(default, zoneCode: "   ");

        Assert.Equal(2, ResultAssert.Ok<ApiListSuccess<LocationDto>>(res).Data.Count);
    }

    [Fact]
    public async Task Update_never_moves_a_row_in_the_hierarchy()
    {
        using var t = TestDb.Seeded();
        var body = new LocationWriteDto("Small", "OTHER", "Renamed", ParentLocationCode: "W1_ZA", ZoneCode: "ZONE-A");

        var res = await new LocationService(t.Db).UpdateAsync("W1_ZA", body, default);

        var dto = ResultAssert.Ok<ApiSuccess<LocationDto>>(res).Data!;
        Assert.Equal("W1_ZA", dto.LocationCode);
        Assert.Equal("Middle", dto.LocationType);
        Assert.Equal("W1", dto.ParentLocationCode);
        Assert.Equal("Renamed", dto.LocationName);
    }


    [Fact]
    public async Task Delete_is_refused_while_a_child_location_exists()
    {
        using var t = TestDb.Seeded();

        var res = await new LocationService(t.Db).DeleteAsync("W1", default);

        ResultAssert.InUse(res);
    }

    [Fact]
    public async Task Delete_is_refused_while_an_item_uses_it_as_default_location()
    {
        using var t = TestDb.Seeded();
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-1", ItemName = "Item 1", ItemGroupCode = "IG-01",
            DefaultLocationCode = "W1_ZA", PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new LocationService(t.Db).DeleteAsync("W1_ZA", default);

        ResultAssert.InUse(res);
    }

    [Fact]
    public async Task Delete_soft_deletes_a_leaf()
    {
        using var t = TestDb.Seeded();

        var res = await new LocationService(t.Db).DeleteAsync("W1_ZA", default);

        ResultAssert.NoContent(res);
        Assert.True((await t.Db.Locations.FindAsync("W1_ZA"))!.IsDeleted);
    }

    [Fact]
    public async Task Delete_of_the_parent_succeeds_after_the_child_is_deleted()
    {
        using var t = TestDb.Seeded();

        var child = await new LocationService(t.Db).DeleteAsync("W1_ZA", default);
        var parent = await new LocationService(t.Db).DeleteAsync("W1", default);

        ResultAssert.NoContent(child);
        ResultAssert.NoContent(parent);
    }


    [Fact]
    public async Task ExportCsv_includes_the_decomposed_segments()
    {
        using var t = TestDb.Seeded();

        var res = await new LocationService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("large_code", lines[0]);
        Assert.Contains("middle_code", lines[0]);
        Assert.Contains("small_code", lines[0]);
        Assert.Contains(lines, l => l.Contains("W1_ZA"));
    }
}
