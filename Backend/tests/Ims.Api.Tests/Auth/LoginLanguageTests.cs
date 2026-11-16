using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Ims.Api.Contracts;
using Ims.Api.Features.Auth;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Auth;

public class LoginLanguageTests
{
    private static IOptions<JwtOptions> Jwt() => Options.Create(new JwtOptions
    {
        Issuer = "ims-test",
        Audience = "ims-test",
        Key = "test-only-signing-key-with-at-least-32-bytes",
        ExpiresSeconds = 60,
    });

    private static async Task SeedUserAsync(TestDb t, string code, string? lang)
    {
        var u = new AppUser
        {
            UserCode = code, UserName = "Tester", RoleCode = "ADMIN",
            DefaultLanguage = lang, IsActive = true, CreatedAt = DateTime.UtcNow,
            PasswordHash = "",
        };
        u.PasswordHash = new PasswordHasher<AppUser>().HashPassword(u, "pw");
        t.Db.AppUsers.Add(u);
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Login_returns_the_users_default_language()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t, "U001", "ja");

        var res = await new AuthService(t.Db, Jwt()).LoginAsync(new LoginRequest("U001", "pw"), default);

        var body = ResultAssert.Ok<ApiSuccess<LoginResponseData>>(res);
        Assert.Equal("ja", body.Data!.DefaultLanguage);
    }

    [Fact]
    public async Task Login_returns_null_when_the_user_never_picked_one()
    {
        using var t = TestDb.Seeded();
        await SeedUserAsync(t, "U002", null);

        var res = await new AuthService(t.Db, Jwt()).LoginAsync(new LoginRequest("U002", "pw"), default);

        Assert.Null(ResultAssert.Ok<ApiSuccess<LoginResponseData>>(res).Data!.DefaultLanguage);
    }
}
