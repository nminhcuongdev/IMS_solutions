using Ims.Api.Contracts;
using Ims.Api.Features.Zones;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class ZoneServiceTests
{
    private static ZoneWriteDto Valid(
        string code = "ZONE-NEW", string name = "New zone", string factory = "FAC-01") =>
        new(code, name, factory);


    [Fact]
    public async Task Create_persists_row_and_returns_201()
    {
        using var t = TestDb.Seeded();
        var body = Valid();

        var res = await new ZoneService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ZoneDto>>(res).Data!;
        Assert.Equal("ZONE-NEW", dto.ZoneCode);
        Assert.Equal("FAC-01", dto.FactoryCode);
        Assert.NotNull(await t.Db.Zones.FindAsync("ZONE-NEW"));
    }

    [Fact]
    public async Task Create_trims_code_name_and_factory()
    {
        using var t = TestDb.Seeded();
        var body = new ZoneWriteDto("  Z-T  ", "  Zone T  ", "  FAC-01  ");

        var res = await new ZoneService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ZoneDto>>(res).Data!;
        Assert.Equal("Z-T", dto.ZoneCode);
        Assert.Equal("Zone T", dto.ZoneName);
        Assert.Equal("FAC-01", dto.FactoryCode);
    }

    [Fact]
    public async Task Create_rejects_blank_code()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: " ");

        var res = await new ZoneService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-001");
    }

    [Fact]
    public async Task Create_rejects_forbidden_code_chars()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "Z/1");

        var res = await new ZoneService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-005");
    }

    [Fact]
    public async Task Create_rejects_blank_name()
    {
        using var t = TestDb.Seeded();
        var body = Valid(name: "");

        var res = await new ZoneService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-002");
    }

    [Fact]
    public async Task Create_rejects_duplicate_code()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "ZONE-A");

        var res = await new ZoneService(t.Db).CreateAsync(body, default);

        ResultAssert.Conflict(res);
    }

    [Fact]
    public async Task Create_detects_a_duplicate_even_when_the_code_is_padded()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "  ZONE-A  ");

        var res = await new ZoneService(t.Db).CreateAsync(body, default);

        ResultAssert.Conflict(res);
    }

    [Fact]
    public async Task Create_rejects_unknown_factory()
    {
        using var t = TestDb.Seeded();
        var body = Valid(factory: "NOPE");

        var res = await new ZoneService(t.Db).CreateAsync(body, default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task Create_rejects_soft_deleted_factory()
    {
        using var t = TestDb.Seeded();
        var body = Valid(factory: "FAC-DEL");

        var res = await new ZoneService(t.Db).CreateAsync(body, default);

        ResultAssert.NotFound(res);
    }


    [Fact]
    public async Task Get_returns_row_and_404_for_missing_or_deleted()
    {
        using var t = TestDb.Seeded();
        t.Db.Zones.Add(new Zone
        {
            ZoneCode = "Z-DEL", ZoneName = "Gone", FactoryCode = "FAC-01",
            IsDeleted = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var live = await new ZoneService(t.Db).GetAsync("ZONE-A", default);
        var missing = await new ZoneService(t.Db).GetAsync("NOPE", default);
        var deleted = await new ZoneService(t.Db).GetAsync("Z-DEL", default);

        Assert.Equal("Zone A", ResultAssert.Ok<ApiSuccess<ZoneDto>>(live).Data!.ZoneName);

        ResultAssert.NotFound(missing);

        ResultAssert.NotFound(deleted);
    }

    [Fact]
    public async Task List_filters_soft_deleted_and_searches_code_or_name()
    {
        using var t = TestDb.Seeded();
        t.Db.Zones.Add(new Zone { ZoneCode = "Z-B", ZoneName = "Beta", FactoryCode = "FAC-01", IsActive = true, CreatedAt = DateTime.UtcNow });
        t.Db.Zones.Add(new Zone { ZoneCode = "Z-DEL", ZoneName = "Gone", FactoryCode = "FAC-01", IsActive = true, IsDeleted = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var allRes = await new ZoneService(t.Db).ListAsync(default);
        var hitRes = await new ZoneService(t.Db).ListAsync(default, q: "beta");

        var all = ResultAssert.Ok<ApiListSuccess<ZoneDto>>(allRes);
        Assert.Equal(2, all.Meta.Total);

        var hit = ResultAssert.Ok<ApiListSuccess<ZoneDto>>(hitRes);
        Assert.Equal("Z-B", Assert.Single(hit.Data).ZoneCode);
    }

    [Fact]
    public async Task List_sorts_by_zone_name_descending()
    {
        using var t = TestDb.Seeded();
        t.Db.Zones.Add(new Zone { ZoneCode = "Z-B", ZoneName = "Beta", FactoryCode = "FAC-01", IsActive = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var res = await new ZoneService(t.Db).ListAsync(default, sort: "-zone_name");

        var body = ResultAssert.Ok<ApiListSuccess<ZoneDto>>(res);
        Assert.Equal(new[] { "Zone A", "Beta" }, body.Data.Select(z => z.ZoneName));
    }


    [Fact]
    public async Task Update_changes_fields_but_never_the_code()
    {
        using var t = TestDb.Seeded();
        var body = new ZoneWriteDto("HACKED", "Renamed", "FAC-01", IsActive: false);

        var res = await new ZoneService(t.Db).UpdateAsync("ZONE-A", body, default);

        var dto = ResultAssert.Ok<ApiSuccess<ZoneDto>>(res).Data!;
        Assert.Equal("ZONE-A", dto.ZoneCode);
        Assert.Equal("Renamed", dto.ZoneName);
        Assert.False(dto.IsActive);
        Assert.NotNull(dto.UpdatedAt);
        Assert.Null(await t.Db.Zones.FindAsync("HACKED"));
    }

    [Fact]
    public async Task Update_writes_all_five_remarks()
    {
        using var t = TestDb.Seeded();
        var body = new ZoneWriteDto("ZONE-A", "Zone A", "FAC-01",
            Remark1: "r1", Remark2: "r2", Remark3: "r3", Remark4: "r4", Remark5: "r5");

        var res = await new ZoneService(t.Db).UpdateAsync("ZONE-A", body, default);

        var dto = ResultAssert.Ok<ApiSuccess<ZoneDto>>(res).Data!;
        Assert.Equal(new[] { "r1", "r2", "r3", "r4", "r5" },
            new[] { dto.Remark1, dto.Remark2, dto.Remark3, dto.Remark4, dto.Remark5 });
    }

    [Fact]
    public async Task Update_rejects_unknown_factory_and_blank_name()
    {
        using var t = TestDb.Seeded();

        var badFactory = await new ZoneService(t.Db).UpdateAsync("ZONE-A", Valid(factory: "NOPE"), default);
        var blankName = await new ZoneService(t.Db).UpdateAsync("ZONE-A", Valid(name: " "), default);

        ResultAssert.NotFound(badFactory);

        ResultAssert.BadRequest(blankName, "VAL-002");
    }

    [Fact]
    public async Task Update_returns_404_for_unknown_row()
    {
        using var t = TestDb.Seeded();

        var res = await new ZoneService(t.Db).UpdateAsync("NOPE", Valid(), default);

        ResultAssert.NotFound(res);
    }


    [Fact]
    public async Task Delete_is_refused_while_a_location_references_the_zone()
    {
        using var t = TestDb.Seeded();

        var res = await new ZoneService(t.Db).DeleteAsync("ZONE-A", default);

        ResultAssert.Conflict(res);
    }

    [Fact]
    public async Task Delete_soft_deletes_once_no_location_references_it()
    {
        using var t = TestDb.Seeded();
        t.Db.Locations.RemoveRange(t.Db.Locations);
        await t.Db.SaveChangesAsync();

        var res = await new ZoneService(t.Db).DeleteAsync("ZONE-A", default);

        ResultAssert.NoContent(res);
        var row = await t.Db.Zones.FindAsync("ZONE-A");
        Assert.True(row!.IsDeleted);
    }

    [Fact]
    public async Task Delete_returns_404_for_unknown_row()
    {
        using var t = TestDb.Seeded();

        var res = await new ZoneService(t.Db).DeleteAsync("NOPE", default);

        ResultAssert.NotFound(res);
    }


    [Fact]
    public async Task ExportCsv_lists_live_rows_only()
    {
        using var t = TestDb.Seeded();
        t.Db.Zones.Add(new Zone { ZoneCode = "Z-DEL", ZoneName = "Gone", FactoryCode = "FAC-01", IsDeleted = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var res = await new ZoneService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        Assert.Contains("ZONE-A", csv);
        Assert.DoesNotContain("Z-DEL", csv);
    }
}
