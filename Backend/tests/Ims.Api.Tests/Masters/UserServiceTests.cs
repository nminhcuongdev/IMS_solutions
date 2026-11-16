using Microsoft.AspNetCore.Identity;
using Moq;
using Ims.Api.Contracts;
using Ims.Api.Features.Users;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Masters;

public class UserServiceTests
{
    private const string Hashed = "HASHED::";

    private static Mock<IPasswordHasher<AppUser>> FakeHasher()
    {
        var m = new Mock<IPasswordHasher<AppUser>>(MockBehavior.Strict);
        m.Setup(h => h.HashPassword(It.IsAny<AppUser>(), It.IsAny<string>()))
         .Returns((AppUser _, string pwd) => Hashed + pwd);
        return m;
    }

    private static UserWriteDto Valid(
        string code = "U-NEW", string name = "New user", string role = "ADMIN",
        string? password = "secret123", string? factory = null, string? language = null) =>
        new(code, name, role, password, factory, language);

    private static async Task<AppUser> SeedUserAsync(TestDb t, string code = "U001")
    {
        var u = new AppUser
        {
            UserCode = code, UserName = "Existing", RoleCode = "ADMIN",
            PasswordHash = "ORIGINAL", IsActive = true, CreatedAt = DateTime.UtcNow,
        };
        t.Db.AppUsers.Add(u);
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return u;
    }


    [Fact]
    public async Task Create_hashes_the_submitted_password_exactly_once()
    {
        using var t = TestDb.Seeded();
        var hasher = FakeHasher();
        var body = Valid();

        var res = await new UserService(t.Db, hasher.Object).CreateAsync(body, default);

        ResultAssert.Created<ApiSuccess<UserDto>>(res);
        hasher.Verify(h => h.HashPassword(It.IsAny<AppUser>(), "secret123"), Times.Once);
        Assert.Equal(Hashed + "secret123", (await t.Db.AppUsers.FindAsync("U-NEW"))!.PasswordHash);
    }

    [Fact]
    public async Task Create_response_never_exposes_the_password_hash()
    {
        using var t = TestDb.Seeded();
        var hasher = FakeHasher();
        var body = Valid();

        var res = await new UserService(t.Db, hasher.Object).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<UserDto>>(res).Data!;
        Assert.DoesNotContain("PasswordHash", typeof(UserDto).GetProperties().Select(p => p.Name));
        Assert.Equal("U-NEW", dto.UserCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_requires_a_password(string? password)
    {
        using var t = TestDb.Seeded();
        var hasher = FakeHasher();
        var body = Valid(password: password);

        var res = await new UserService(t.Db, hasher.Object).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-001");
        hasher.Verify(h => h.HashPassword(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_rejects_blank_code_blank_name_and_duplicates()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);
        var hasher = FakeHasher().Object;

        var blankCode = await new UserService(t.Db, hasher).CreateAsync(Valid(code: " "), default);
        var blankName = await new UserService(t.Db, hasher).CreateAsync(Valid(name: " "), default);
        var duplicate = await new UserService(t.Db, hasher).CreateAsync(Valid(code: "U001"), default);
        var padded = await new UserService(t.Db, hasher).CreateAsync(Valid(code: "  U001  "), default);

        ResultAssert.BadRequest(blankCode, "VAL-001");
        ResultAssert.BadRequest(blankName, "VAL-001");

        ResultAssert.Conflict(duplicate);

        ResultAssert.Conflict(padded);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_role()
    {
        using var t = TestDb.Seeded();
        var hasher = FakeHasher().Object;
        var body = Valid(role: "Ghost");

        var res = await new UserService(t.Db, hasher).CreateAsync(body, default);

        ResultAssert.BadRequest(res, "VAL-001");
    }

    [Fact]
    public async Task Create_validates_optional_factory_and_language_only_when_supplied()
    {
        using var t = TestDb.Seeded();
        var hasher = FakeHasher().Object;

        var okRes = await new UserService(t.Db, hasher).CreateAsync(Valid(factory: "FAC-01", language: "vi"), default);
        var badFactory = await new UserService(t.Db, hasher).CreateAsync(Valid(code: "U-A", factory: "NOPE"), default);
        var badLanguage = await new UserService(t.Db, hasher).CreateAsync(Valid(code: "U-B", language: "de"), default);
        var noneRes = await new UserService(t.Db, hasher).CreateAsync(Valid(code: "U-C"), default);

        var ok = ResultAssert.Created<ApiSuccess<UserDto>>(okRes).Data!;
        Assert.Equal("FAC-01", ok.FactoryCode);
        Assert.Equal("vi", ok.DefaultLanguage);

        ResultAssert.NotFound(badFactory);
        ResultAssert.NotFound(badLanguage);

        var none = ResultAssert.Created<ApiSuccess<UserDto>>(noneRes).Data!;
        Assert.Null(none.FactoryCode);
    }

    [Fact]
    public async Task Create_treats_a_blank_factory_as_null()
    {
        using var t = TestDb.Seeded();
        var hasher = FakeHasher().Object;
        var body = Valid(factory: "");

        var res = await new UserService(t.Db, hasher).CreateAsync(body, default);

        var dto = ResultAssert.Created<ApiSuccess<UserDto>>(res).Data!;
        Assert.Null(dto.FactoryCode);
    }


    [Fact]
    public async Task Update_without_a_password_leaves_the_hash_untouched()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);
        var hasher = FakeHasher();
        var body = Valid(code: "U001", name: "Renamed", password: null);

        var res = await new UserService(t.Db, hasher.Object).UpdateAsync("U001", body, default);

        ResultAssert.Ok<ApiSuccess<UserDto>>(res);
        hasher.Verify(h => h.HashPassword(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        Assert.Equal("ORIGINAL", (await t.Db.AppUsers.FindAsync("U001"))!.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task Update_treats_a_blank_password_as_no_change(string password)
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);
        var hasher = FakeHasher();
        var body = Valid(code: "U001", password: password);

        await new UserService(t.Db, hasher.Object).UpdateAsync("U001", body, default);

        hasher.Verify(h => h.HashPassword(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        Assert.Equal("ORIGINAL", (await t.Db.AppUsers.FindAsync("U001"))!.PasswordHash);
    }

    [Fact]
    public async Task Update_with_a_password_rehashes_it()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);
        var hasher = FakeHasher();
        var body = Valid(code: "U001", password: "newpass");

        await new UserService(t.Db, hasher.Object).UpdateAsync("U001", body, default);

        hasher.Verify(h => h.HashPassword(It.IsAny<AppUser>(), "newpass"), Times.Once);
        Assert.Equal(Hashed + "newpass", (await t.Db.AppUsers.FindAsync("U001"))!.PasswordHash);
    }

    [Fact]
    public async Task Update_returns_404_for_unknown_row_and_unknown_role()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);
        var hasher = FakeHasher().Object;

        var unknownRow = await new UserService(t.Db, hasher).UpdateAsync("NOPE", Valid(), default);
        var unknownRole = await new UserService(t.Db, hasher).UpdateAsync("U001", Valid(role: "Ghost"), default);

        ResultAssert.NotFound(unknownRow);
        ResultAssert.BadRequest(unknownRole, "VAL-001");
    }

    [Fact]
    public async Task Update_rejects_a_blank_name()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);
        var hasher = FakeHasher().Object;
        var body = Valid(name: " ");

        var res = await new UserService(t.Db, hasher).UpdateAsync("U001", body, default);

        ResultAssert.BadRequest(res, "VAL-001");
    }


    [Fact]
    public async Task List_hides_deleted_rows_and_searches_code_or_name()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);
        t.Db.AppUsers.Add(new AppUser
        {
            UserCode = "U002", UserName = "Gone", RoleCode = "ADMIN",
            PasswordHash = "x", IsActive = true, IsDeleted = true, CreatedAt = DateTime.UtcNow,
        });
        await t.Db.SaveChangesAsync();

        var allRes = await new UserService(t.Db, new Microsoft.AspNetCore.Identity.PasswordHasher<Ims.Domain.Masters.AppUser>()).ListAsync(default);
        var hitRes = await new UserService(t.Db, new Microsoft.AspNetCore.Identity.PasswordHasher<Ims.Domain.Masters.AppUser>()).ListAsync(default, q: "exist");

        var all = ResultAssert.Ok<ApiListSuccess<UserDto>>(allRes);
        Assert.Equal("U001", Assert.Single(all.Data).UserCode);

        var hit = ResultAssert.Ok<ApiListSuccess<UserDto>>(hitRes);
        Assert.Single(hit.Data);
    }

    [Fact]
    public async Task Get_returns_the_row_and_404_when_missing()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);

        var found = await new UserService(t.Db, new Microsoft.AspNetCore.Identity.PasswordHasher<Ims.Domain.Masters.AppUser>()).GetAsync("U001", default);
        var missing = await new UserService(t.Db, new Microsoft.AspNetCore.Identity.PasswordHasher<Ims.Domain.Masters.AppUser>()).GetAsync("NOPE", default);

        Assert.Equal("Existing", ResultAssert.Ok<ApiSuccess<UserDto>>(found).Data!.UserName);
        ResultAssert.NotFound(missing);
    }

    [Fact]
    public async Task Delete_soft_deletes()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);

        var first = await new UserService(t.Db, new Microsoft.AspNetCore.Identity.PasswordHasher<Ims.Domain.Masters.AppUser>()).DeleteAsync("U001", default);
        var second = await new UserService(t.Db, new Microsoft.AspNetCore.Identity.PasswordHasher<Ims.Domain.Masters.AppUser>()).DeleteAsync("U001", default);

        ResultAssert.NoContent(first);
        Assert.True((await t.Db.AppUsers.FindAsync("U001"))!.IsDeleted);

        ResultAssert.NotFound(second);
    }

    [Fact]
    public async Task ExportCsv_never_contains_the_password_hash()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t);

        var res = await new UserService(t.Db, new Microsoft.AspNetCore.Identity.PasswordHasher<Ims.Domain.Masters.AppUser>()).ExportCsvAsync(default);
        var csv = await CsvReader.ReadAsync(res);

        Assert.Contains("U001", csv);
        Assert.DoesNotContain("ORIGINAL", csv);
        Assert.DoesNotContain("password", csv, StringComparison.OrdinalIgnoreCase);
    }
}
