using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Ims.Api.Contracts;
using Ims.Api.Features.Auth;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Auth;

public class LoginCodeTests
{
    private static IOptions<JwtOptions> Jwt() => Options.Create(new JwtOptions
    {
        Key = new string('k', 64), Issuer = "i", Audience = "a", ExpiresSeconds = 3600,
    });

    [Fact]
    public async Task Both_fields_missing_is_AUTH_002_and_one_missing_is_VAL_001()
    {
        using var t = TestDb.Seeded();
        var service = new AuthService(t.Db, Jwt());

        ResultAssert.Error(await service.LoginAsync(new LoginRequest("", ""), default), 401, "AUTH-002");
        ResultAssert.BadRequest(await service.LoginAsync(new LoginRequest("", "pw"), default), "VAL-001");
        ResultAssert.BadRequest(await service.LoginAsync(new LoginRequest("U1", " "), default), "VAL-001");
    }

    [Fact]
    public async Task A_disabled_account_is_AUTH_005()
    {
        using var t = TestDb.Seeded();
        var u = new AppUser { UserCode = "OFF", UserName = "Off", RoleCode = "ADMIN", IsActive = false };
        u.PasswordHash = new PasswordHasher<AppUser>().HashPassword(u, "pw");
        t.Db.AppUsers.Add(u);
        await t.Db.SaveChangesAsync();

        ResultAssert.Error(await new AuthService(t.Db, Jwt()).LoginAsync(new LoginRequest("OFF", "pw"), default), 403, "AUTH-005");
    }
}
