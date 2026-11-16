using Ims.Api.Contracts;
using Ims.Api.Features.Items;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class ItemServiceTests
{
    private static ItemWriteDto Valid(
        string code = "IT-NEW", string name = "New item", string group = "IG-01",
        string? packing = null, string? location = null, string? uom = null) =>
        new(code, name, group, UomCode: uom, PackingTypeCode: packing,
            PackageQty: 10, ExpirationPeriod: 30, Barcode: "4901234567894",
            SurplusPoint: 100, ReorderLevel1: 20, ReorderLevel2: 10,
            DefaultLocationCode: location);


    [Fact]
    public async Task Create_persists_every_numeric_field()
    {
        using var t = TestDb.Seeded();
        var body = Valid();

        var res = await new ItemService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ItemDto>>(res).Data!;
        Assert.Equal(10m, dto.PackageQty);
        Assert.Equal(30, dto.ExpirationPeriod);
        Assert.Equal(100m, dto.SurplusPoint);
        Assert.Equal(20m, dto.ReorderLevel1);
        Assert.Equal(10m, dto.ReorderLevel2);
        Assert.Equal("4901234567894", dto.Barcode);
    }

    [Fact]
    public async Task Lot_and_scan_flags_round_trip_through_the_item_master()
    {
        using var t = TestDb.Seeded();
        var created = await new ItemService(t.Db).CreateAsync(
            Valid() with { IsLotManaged = true, HtScanRequired = false }, default);
        var dto = ResultAssert.Created<ApiSuccess<ItemDto>>(created).Data!;
        Assert.True(dto.IsLotManaged);
        Assert.False(dto.HtScanRequired);

        t.Db.ChangeTracker.Clear();
        var loaded = ResultAssert.Ok<ApiSuccess<ItemDto>>(
            await new ItemService(t.Db).GetAsync("IT-NEW", default)).Data!;
        Assert.True(loaded.IsLotManaged);
        Assert.False(loaded.HtScanRequired);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_or_deleted_item_group()
    {
        using var t = TestDb.Seeded();

        var unknown = await new ItemService(t.Db).CreateAsync(Valid(group: "NOPE"), default);

        ResultAssert.NotFound(unknown);

        var g = await t.Db.ItemGroups.FindAsync("IG-01");
        g!.IsDeleted = true;
        await t.Db.SaveChangesAsync();

        var deleted = await new ItemService(t.Db).CreateAsync(Valid(), default);

        ResultAssert.NotFound(deleted);
    }

    [Fact]
    public async Task Create_accepts_a_known_packing_type_and_default_location()
    {
        using var t = TestDb.Seeded();
        var body = Valid(packing: "BOX", location: "W1_ZA");

        var res = await new ItemService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ItemDto>>(res).Data!;
        Assert.Equal("BOX", dto.PackingTypeCode);
        Assert.Equal("W1_ZA", dto.DefaultLocationCode);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_packing_type_or_location()
    {
        using var t = TestDb.Seeded();

        var badPacking = await new ItemService(t.Db).CreateAsync(Valid(packing: "NOPE"), default);
        var badLocation = await new ItemService(t.Db).CreateAsync(Valid(location: "NOPE"), default);

        ResultAssert.NotFound(badPacking);
        ResultAssert.NotFound(badLocation);
    }

    [Fact]
    public async Task Create_rejects_an_inactive_packing_type()
    {
        using var t = TestDb.Seeded();
        var pt = t.Db.DefaultValues.Single(d => d.ValueCode == "BOX");
        pt.IsActive = false;
        await t.Db.SaveChangesAsync();
        var body = Valid(packing: "BOX");

        var res = await new ItemService(t.Db).CreateAsync(body, default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task Create_treats_blank_optional_FKs_as_null()
    {
        using var t = TestDb.Seeded();
        var body = Valid(packing: "", location: "");

        var res = await new ItemService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ItemDto>>(res).Data!;
        Assert.Null(dto.PackingTypeCode);
        Assert.Null(dto.DefaultLocationCode);
    }

    [Fact]
    public async Task Create_only_trims_the_uom()
    {
        using var t = TestDb.Seeded();
        var body = Valid(uom: "  rolls  ");

        var res = await new ItemService(t.Db).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<ItemDto>>(res).Data!;
        Assert.Equal("rolls", dto.UomCode);
    }

    [Fact]
    public async Task Create_rejects_blank_code_blank_name_and_duplicates()
    {
        using var t = TestDb.Seeded();

        var blankCode = await new ItemService(t.Db).CreateAsync(Valid(code: " "), default);
        var blankName = await new ItemService(t.Db).CreateAsync(Valid(name: " "), default);

        ResultAssert.BadRequest(blankCode, "VAL-001");
        ResultAssert.BadRequest(blankName, "VAL-001");

        var created = await new ItemService(t.Db).CreateAsync(Valid(), default);
        ResultAssert.Created<ApiSuccess<ItemDto>>(created);

        var padded = await new ItemService(t.Db).CreateAsync(Valid(code: "  IT-NEW  "), default);

        ResultAssert.Conflict(padded);
    }


    [Fact]
    public async Task List_hides_deleted_rows_and_searches_code_or_name()
    {
        using var t = TestDb.Seeded();
        t.Db.Items.AddRange(
            new Item { ItemCode = "IT-1", ItemName = "Thermal label", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Item { ItemCode = "IT-2", ItemName = "Gone", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, IsDeleted = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var allRes = await new ItemService(t.Db).ListAsync(default);
        var hitRes = await new ItemService(t.Db).ListAsync(default, q: "thermal");

        var all = ResultAssert.Ok<ApiListSuccess<ItemDto>>(allRes);
        Assert.Equal("IT-1", Assert.Single(all.Data).ItemCode);

        var hit = ResultAssert.Ok<ApiListSuccess<ItemDto>>(hitRes);
        Assert.Single(hit.Data);
    }

    [Fact]
    public async Task Get_returns_404_for_unknown_code()
    {
        using var t = TestDb.Seeded();

        var res = await new ItemService(t.Db).GetAsync("NOPE", default);

        ResultAssert.NotFound(res);
    }


    [Fact]
    public async Task Update_replaces_the_numeric_fields_and_can_clear_optional_FKs()
    {
        using var t = TestDb.Seeded();
        var created = await new ItemService(t.Db).CreateAsync(Valid(packing: "BOX", location: "W1_ZA"), default);
        ResultAssert.Created<ApiSuccess<ItemDto>>(created);
        t.Db.ChangeTracker.Clear();
        var body = new ItemWriteDto("IT-NEW", "Renamed", "IG-01", PackageQty: 5, PackingTypeCode: "", DefaultLocationCode: "");

        var res = await new ItemService(t.Db).UpdateAsync("IT-NEW", body, default);

        var dto = ResultAssert.Ok<ApiSuccess<ItemDto>>(res).Data!;
        Assert.Equal("Renamed", dto.ItemName);
        Assert.Equal(5m, dto.PackageQty);
        Assert.Null(dto.PackingTypeCode);
        Assert.Null(dto.DefaultLocationCode);
        Assert.Null(dto.ExpirationPeriod);
        Assert.NotNull(dto.UpdatedAt);
    }

    [Fact]
    public async Task Update_returns_404_for_unknown_row_and_unknown_group()
    {
        using var t = TestDb.Seeded();
        t.Db.Items.Add(new Item { ItemCode = "IT-1", ItemName = "X", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var unknownRow = await new ItemService(t.Db).UpdateAsync("NOPE", Valid(), default);
        var unknownGroup = await new ItemService(t.Db).UpdateAsync("IT-1", Valid(group: "NOPE"), default);

        ResultAssert.NotFound(unknownRow);
        ResultAssert.NotFound(unknownGroup);
    }


    [Fact]
    public async Task Delete_soft_deletes_because_phase1_has_no_stock_table()
    {
        using var t = TestDb.Seeded();
        t.Db.Items.Add(new Item { ItemCode = "IT-1", ItemName = "X", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var first = await new ItemService(t.Db).DeleteAsync("IT-1", default);
        var second = await new ItemService(t.Db).DeleteAsync("IT-1", default);

        ResultAssert.NoContent(first);
        Assert.True((await t.Db.Items.FindAsync("IT-1"))!.IsDeleted);

        ResultAssert.NotFound(second);
    }


    [Fact]
    public async Task ExportCsv_writes_decimals_with_a_dot_separator()
    {
        using var t = TestDb.Seeded();
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-1", ItemName = "X", ItemGroupCode = "IG-01",
            PackageQty = 12.5m, SurplusPoint = 3.25m, IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new ItemService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        Assert.Contains("12.5", csv);
        Assert.Contains("3.25", csv);
        Assert.DoesNotContain("12,5", csv);
    }

    [Fact]
    public async Task ExportCsv_leaves_null_numbers_empty()
    {
        using var t = TestDb.Seeded();
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-1", ItemName = "X", ItemGroupCode = "IG-01",
            PackageQty = 1, ExpirationPeriod = null, SurplusPoint = null,
            IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new ItemService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        Assert.Contains(",,", csv);
    }

    public static TheoryData<ItemWriteDto> NegativeNumbers => new()
    {
        Valid() with { PackageQty = -1 },
        Valid() with { ExpirationPeriod = -1 },
        Valid() with { SurplusPoint = -1 },
        Valid() with { ReorderLevel1 = -1 },
        Valid() with { ReorderLevel2 = -1 },
    };

    [Theory]
    [MemberData(nameof(NegativeNumbers))]
    public async Task Create_rejects_a_negative_number(ItemWriteDto body)
    {
        using var t = TestDb.Seeded();

        var res = await new ItemService(t.Db).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Theory]
    [MemberData(nameof(NegativeNumbers))]
    public async Task Update_rejects_a_negative_number(ItemWriteDto body)
    {
        using var t = TestDb.Seeded();
        ResultAssert.Created<ApiSuccess<ItemDto>>(await new ItemService(t.Db).CreateAsync(Valid(), default));

        var res = await new ItemService(t.Db).UpdateAsync("IT-NEW", body, default);

        ResultAssert.BadRequest(res, "QTY-001");
    }

    [Theory]
    [InlineData("HL_PW", "HL_PW209")]
    [InlineData("100%", "IT-PCT")]
    [InlineData("[A]", "IT-BRK")]
    public async Task Search_treats_wildcard_characters_literally(string q, string expected)
    {
        using var t = TestDb.Seeded();
        t.Db.Items.AddRange(
            new Item { ItemCode = "HL-HND208", ItemName = "Head", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Item { ItemCode = "HL_PW209", ItemName = "Head", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Item { ItemCode = "IT-PCT", ItemName = "Cotton 100%", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Item { ItemCode = "IT-100", ItemName = "Cotton 1000", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Item { ItemCode = "IT-BRK", ItemName = "Type [A]", ItemGroupCode = "IG-01", PackageQty = 1, IsActive = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var res = await new ItemService(t.Db).ListAsync(default, q: q);

        Assert.Equal(expected, Assert.Single(ResultAssert.Ok<ApiListSuccess<ItemDto>>(res).Data).ItemCode);
    }
}
