using System.Security.Claims;
using Ims.Api.Contracts;
using Ims.Api.Features.Factories;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class FactoryServiceTests
{
    private static ClaimsPrincipal SuperAdmin() =>
        new(new ClaimsIdentity(new[] { new Claim("is_super_admin", "1") }, "test"));

    private static ClaimsPrincipal NormalUser() =>
        new(new ClaimsIdentity(new[] { new Claim("is_super_admin", "0") }, "test"));

    private static FactoryWriteDto Valid(
        string code = "FAC-NEW", string name = "New factory",
        bool isDefault = false, bool isActive = true) =>
        new(code, name, Country: "Vietnam", Address: "Somewhere",
            IsActive: isActive, IsDefault: isDefault);


    [Fact]
    public async Task Create_persists_row_and_returns_201()
    {
        using var t = TestDb.Seeded();
        var body = Valid();

        var res = await new FactoryService(t.Db).CreateAsync(NormalUser(), body, default);

        var dto = ResultAssert.Created<ApiSuccess<FactoryDto>>(res).Data!;
        Assert.Equal("FAC-NEW", dto.FactoryCode);
        Assert.Equal("New factory", dto.FactoryName);
        Assert.NotNull(await t.Db.Factories.FindAsync("FAC-NEW"));
    }

    [Fact]
    public async Task Create_trims_code_and_name()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "  FAC-T  ", name: "  Padded  ");

        var res = await new FactoryService(t.Db).CreateAsync(NormalUser(), body, default);

        var dto = ResultAssert.Created<ApiSuccess<FactoryDto>>(res).Data!;
        Assert.Equal("FAC-T", dto.FactoryCode);
        Assert.Equal("Padded", dto.FactoryName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_rejects_blank_code_with_VAL001(string code)
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: code);

        var res = await new FactoryService(t.Db).CreateAsync(NormalUser(), body, default);

        ResultAssert.BadRequest(res, "VAL-001");
    }

    [Theory]
    [InlineData("A/B")]
    [InlineData("A?B")]
    [InlineData("A&B")]
    [InlineData("A=B")]
    [InlineData("A#B")]
    [InlineData("A%B")]
    public async Task Create_rejects_forbidden_code_chars_with_VAL005(string code)
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: code);

        var res = await new FactoryService(t.Db).CreateAsync(NormalUser(), body, default);

        ResultAssert.BadRequest(res, "VAL-005");
    }

    [Fact]
    public async Task Create_rejects_code_longer_than_column()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: new string('X', FieldHelpers.MaxCodeLength + 1));

        var res = await new FactoryService(t.Db).CreateAsync(NormalUser(), body, default);

        ResultAssert.BadRequest(res, "VAL-005");
    }

    [Fact]
    public async Task Create_rejects_blank_name_with_VAL002()
    {
        using var t = TestDb.Seeded();
        var body = Valid(name: "  ");

        var res = await new FactoryService(t.Db).CreateAsync(NormalUser(), body, default);

        ResultAssert.BadRequest(res, "VAL-002");
    }

    [Fact]
    public async Task Create_rejects_duplicate_code()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "FAC-01");

        var res = await new FactoryService(t.Db).CreateAsync(NormalUser(), body, default);

        ResultAssert.Conflict(res);
    }

    [Fact]
    public async Task Create_rejects_code_of_soft_deleted_row()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "FAC-DEL");

        var res = await new FactoryService(t.Db).CreateAsync(NormalUser(), body, default);

        ResultAssert.Conflict(res);
    }

    [Fact]
    public async Task Create_with_isDefault_is_forbidden_for_normal_user()
    {
        using var t = TestDb.Seeded();
        var body = Valid(isDefault: true);

        var res = await new FactoryService(t.Db).CreateAsync(NormalUser(), body, default);

        ResultAssert.Forbidden(res);
        Assert.Null(await t.Db.Factories.FindAsync("FAC-NEW"));
    }

    [Fact]
    public async Task Create_with_isDefault_clears_the_previous_default_for_super_admin()
    {
        using var t = TestDb.Seeded();
        var old = await t.Db.Factories.FindAsync("FAC-01");
        old!.IsDefault = true;
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        var body = Valid(isDefault: true);

        var res = await new FactoryService(t.Db).CreateAsync(SuperAdmin(), body, default);

        ResultAssert.Created<ApiSuccess<FactoryDto>>(res);
        var defaults = t.Db.Factories.Where(f => f.IsDefault && !f.IsDeleted).Select(f => f.FactoryCode).ToList();
        Assert.Equal(new[] { "FAC-NEW" }, defaults);
    }


    [Fact]
    public async Task Get_returns_the_row()
    {
        using var t = TestDb.Seeded();

        var res = await new FactoryService(t.Db).GetAsync("FAC-01", default);

        var dto = ResultAssert.Ok<ApiSuccess<FactoryDto>>(res).Data!;
        Assert.Equal("Main factory", dto.FactoryName);
    }

    [Fact]
    public async Task Get_returns_404_for_unknown_code()
    {
        using var t = TestDb.Seeded();

        var res = await new FactoryService(t.Db).GetAsync("NOPE", default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task Get_hides_soft_deleted_rows()
    {
        using var t = TestDb.Seeded();

        var res = await new FactoryService(t.Db).GetAsync("FAC-DEL", default);

        ResultAssert.NotFound(res);
    }

    [Fact]
    public async Task List_excludes_soft_deleted_and_inactive_by_default()
    {
        using var t = TestDb.Seeded();
        t.Db.Factories.Add(new Factory
        {
            FactoryCode = "FAC-OFF", FactoryName = "Disabled", IsActive = false,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new FactoryService(t.Db).ListAsync(default);

        var body = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(res);
        Assert.Equal(new[] { "FAC-01" }, body.Data.Select(f => f.FactoryCode));
        Assert.Equal(1, body.Meta.Total);
    }

    [Fact]
    public async Task List_with_isActive_false_returns_active_and_inactive()
    {
        using var t = TestDb.Seeded();
        t.Db.Factories.Add(new Factory
        {
            FactoryCode = "FAC-OFF", FactoryName = "Disabled", IsActive = false,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new FactoryService(t.Db).ListAsync(default, isActive: false);

        var body = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(res);
        Assert.Equal(new[] { "FAC-01", "FAC-OFF" }, body.Data.Select(f => f.FactoryCode).OrderBy(x => x));
    }

    [Fact]
    public async Task List_search_matches_code_or_name_case_insensitively()
    {
        using var t = TestDb.Seeded();

        var byNameRes = await new FactoryService(t.Db).ListAsync(default, q: "main");
        var byCodeRes = await new FactoryService(t.Db).ListAsync(default, q: "fac-0");
        var noHitRes = await new FactoryService(t.Db).ListAsync(default, q: "zzz");

        var byName = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(byNameRes);
        Assert.Single(byName.Data);

        var byCode = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(byCodeRes);
        Assert.Single(byCode.Data);

        var noHit = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(noHitRes);
        Assert.Empty(noHit.Data);
        Assert.Equal(0, noHit.Meta.Total);
    }

    [Fact]
    public async Task List_sorts_by_requested_column_in_both_directions()
    {
        using var t = TestDb.Seeded();
        t.Db.Factories.Add(new Factory { FactoryCode = "FAC-02", FactoryName = "Alpha", IsActive = true, CreatedAt = DateTime.UtcNow });
        await t.Db.SaveChangesAsync();

        var ascRes = await new FactoryService(t.Db).ListAsync(default, sort: "factory_name");
        var descRes = await new FactoryService(t.Db).ListAsync(default, sort: "-factory_name");

        var asc = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(ascRes);
        Assert.Equal(new[] { "Alpha", "Main factory" }, asc.Data.Select(f => f.FactoryName));

        var desc = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(descRes);
        Assert.Equal(new[] { "Main factory", "Alpha" }, desc.Data.Select(f => f.FactoryName));
    }

    [Fact]
    public async Task List_falls_back_to_default_sort_for_unknown_column()
    {
        using var t = TestDb.Seeded();

        var res = await new FactoryService(t.Db).ListAsync(default, sort: "drop_table");

        ResultAssert.Ok<ApiListSuccess<FactoryDto>>(res);
    }

    [Theory]
    [InlineData(null, 50)]
    [InlineData(0, 50)]
    [InlineData(37, 50)]
    [InlineData(20, 20)]
    [InlineData(200, 200)]
    public async Task List_normalises_pageSize(int? requested, int expected)
    {
        using var t = TestDb.Seeded();

        var res = await new FactoryService(t.Db).ListAsync(default, pageSize: requested);

        var body = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(res);
        Assert.Equal(expected, body.Meta.PageSize);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public async Task List_normalises_page(int? requested, int expected)
    {
        using var t = TestDb.Seeded();

        var res = await new FactoryService(t.Db).ListAsync(default, page: requested);

        var body = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(res);
        Assert.Equal(expected, body.Meta.Page);
    }

    [Fact]
    public async Task List_paginates_while_reporting_the_full_total()
    {
        using var t = TestDb.Empty();
        for (var i = 1; i <= 45; i++)
            t.Db.Factories.Add(new Factory
            {
                FactoryCode = $"F{i:D3}", FactoryName = $"Factory {i:D3}",
                IsActive = true, CreatedAt = DateTime.UtcNow,
            });
        await t.Db.SaveChangesAsync();

        var res = await new FactoryService(t.Db).ListAsync(default, page: 2, pageSize: 20);

        var page2 = ResultAssert.Ok<ApiListSuccess<FactoryDto>>(res);
        Assert.Equal(45, page2.Meta.Total);
        Assert.Equal(20, page2.Data.Count);
        Assert.Equal("F021", page2.Data[0].FactoryCode);
    }


    [Fact]
    public async Task Update_changes_fields_and_stamps_UpdatedAt()
    {
        using var t = TestDb.Seeded();
        var body = new FactoryWriteDto("FAC-01", "Renamed", Country: "Japan", Address: null, IsActive: false);

        var res = await new FactoryService(t.Db).UpdateAsync("FAC-01", NormalUser(), body, default);

        var dto = ResultAssert.Ok<ApiSuccess<FactoryDto>>(res).Data!;
        Assert.Equal("Renamed", dto.FactoryName);
        Assert.Equal("Japan", dto.Country);
        Assert.False(dto.IsActive);
        Assert.NotNull(dto.UpdatedAt);
    }

    [Fact]
    public async Task Update_ignores_the_code_in_the_body()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "HACKED", name: "Renamed");

        var res = await new FactoryService(t.Db).UpdateAsync("FAC-01", NormalUser(), body, default);

        var dto = ResultAssert.Ok<ApiSuccess<FactoryDto>>(res).Data!;
        Assert.Equal("FAC-01", dto.FactoryCode);
        Assert.Null(await t.Db.Factories.FindAsync("HACKED"));
    }

    [Fact]
    public async Task Update_returns_404_for_unknown_or_deleted_row()
    {
        using var t = TestDb.Seeded();

        var unknown = await new FactoryService(t.Db).UpdateAsync("NOPE", NormalUser(), Valid(), default);
        var deleted = await new FactoryService(t.Db).UpdateAsync("FAC-DEL", NormalUser(), Valid(), default);

        ResultAssert.NotFound(unknown);
        ResultAssert.NotFound(deleted);
    }

    [Fact]
    public async Task Update_rejects_blank_name()
    {
        using var t = TestDb.Seeded();
        var body = Valid(name: "");

        var res = await new FactoryService(t.Db).UpdateAsync("FAC-01", NormalUser(), body, default);

        ResultAssert.BadRequest(res, "VAL-002");
    }

    [Fact]
    public async Task Update_forbids_a_normal_user_from_turning_isDefault_on()
    {
        using var t = TestDb.Seeded();
        var body = Valid(code: "FAC-01", isDefault: true);

        var res = await new FactoryService(t.Db).UpdateAsync("FAC-01", NormalUser(), body, default);

        ResultAssert.Forbidden(res);
    }

    [Fact]
    public async Task Update_allows_a_normal_user_to_resend_the_unchanged_isDefault()
    {
        using var t = TestDb.Seeded();
        var f = await t.Db.Factories.FindAsync("FAC-01");
        f!.IsDefault = true;
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        var body = Valid(code: "FAC-01", name: "Renamed", isDefault: true);

        var res = await new FactoryService(t.Db).UpdateAsync("FAC-01", NormalUser(), body, default);

        var dto = ResultAssert.Ok<ApiSuccess<FactoryDto>>(res).Data!;
        Assert.Equal("Renamed", dto.FactoryName);
        Assert.True(dto.IsDefault);
    }

    [Fact]
    public async Task Update_forbids_a_normal_user_from_turning_isDefault_off()
    {
        using var t = TestDb.Seeded();
        var f = await t.Db.Factories.FindAsync("FAC-01");
        f!.IsDefault = true;
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        var body = Valid(code: "FAC-01", isDefault: false);

        var res = await new FactoryService(t.Db).UpdateAsync("FAC-01", NormalUser(), body, default);

        ResultAssert.Forbidden(res);
    }

    [Fact]
    public async Task Update_by_super_admin_moves_the_default_flag()
    {
        using var t = TestDb.Seeded();
        t.Db.Factories.Add(new Factory
        {
            FactoryCode = "FAC-02", FactoryName = "Second", IsActive = true,
            IsDefault = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        var body = Valid(code: "FAC-01", name: "Main factory", isDefault: true);

        var res = await new FactoryService(t.Db).UpdateAsync("FAC-01", SuperAdmin(), body, default);

        ResultAssert.Ok<ApiSuccess<FactoryDto>>(res);
        var defaults = t.Db.Factories.Where(f => f.IsDefault && !f.IsDeleted).Select(f => f.FactoryCode).ToList();
        Assert.Equal(new[] { "FAC-01" }, defaults);
    }


    [Fact]
    public async Task Delete_is_soft_and_keeps_the_row()
    {
        using var t = TestDb.Seeded();
        t.Db.Zones.RemoveRange(t.Db.Zones);
        t.Db.Locations.RemoveRange(t.Db.Locations);
        await t.Db.SaveChangesAsync();

        var res = await new FactoryService(t.Db).DeleteAsync("FAC-01", default);

        ResultAssert.NoContent(res);
        var row = await t.Db.Factories.FindAsync("FAC-01");
        Assert.NotNull(row);
        Assert.True(row!.IsDeleted);
        Assert.NotNull(row.UpdatedAt);
    }

    [Fact]
    public async Task Delete_returns_404_for_unknown_or_already_deleted()
    {
        using var t = TestDb.Seeded();

        var unknown = await new FactoryService(t.Db).DeleteAsync("NOPE", default);
        var alreadyDeleted = await new FactoryService(t.Db).DeleteAsync("FAC-DEL", default);

        ResultAssert.NotFound(unknown);
        ResultAssert.NotFound(alreadyDeleted);
    }

    [Fact]
    public async Task Delete_is_refused_while_a_zone_still_references_the_factory()
    {
        using var t = TestDb.Seeded();

        var res = await new FactoryService(t.Db).DeleteAsync("FAC-01", default);

        ResultAssert.Conflict(res);
        Assert.False((await t.Db.Factories.FindAsync("FAC-01"))!.IsDeleted);
    }

    [Fact]
    public async Task Delete_is_refused_while_a_user_still_references_the_factory()
    {
        using var t = TestDb.Seeded();
        t.Db.Zones.RemoveRange(t.Db.Zones);
        t.Db.Locations.RemoveRange(t.Db.Locations);
        t.Db.AppUsers.Add(new AppUser
        {
            UserCode = "U1", UserName = "User 1", RoleCode = "Admin",
            FactoryCode = "FAC-01", PasswordHash = "x", IsActive = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new FactoryService(t.Db).DeleteAsync("FAC-01", default);

        ResultAssert.Conflict(res);
    }

    [Fact]
    public async Task Delete_ignores_soft_deleted_children()
    {
        using var t = TestDb.Seeded();
        foreach (var z in t.Db.Zones) z.IsDeleted = true;
        t.Db.Locations.RemoveRange(t.Db.Locations);
        await t.Db.SaveChangesAsync();

        var res = await new FactoryService(t.Db).DeleteAsync("FAC-01", default);

        ResultAssert.NoContent(res);
    }


    [Fact]
    public async Task ExportCsv_writes_a_header_and_one_line_per_live_row()
    {
        using var t = TestDb.Seeded();

        var res = await new FactoryService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("factory_code,factory_name,country,address,", lines[0]);
        Assert.DoesNotContain("company_code", lines[0]);
        Assert.Equal(2, lines.Length);
        Assert.Contains("FAC-01", lines[1]);
    }

    [Fact]
    public async Task ExportCsv_quotes_fields_containing_a_comma()
    {
        using var t = TestDb.Empty();
        t.Db.Factories.Add(new Factory
        {
            FactoryCode = "F1", FactoryName = "Northwind Vietnam, Ltd.", IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var res = await new FactoryService(t.Db).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        Assert.Contains("\"Northwind Vietnam, Ltd.\"", csv);
    }
}
