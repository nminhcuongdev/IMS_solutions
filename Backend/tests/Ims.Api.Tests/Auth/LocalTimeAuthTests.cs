using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ims.Api.Contracts;
using Ims.Api.Features.Auth;
using Ims.Api.Tests.Infrastructure;
using Xunit;

namespace Ims.Api.Tests.Auth;

public class LocalTimeAuthTests
{
    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime Now => now;
    }

    [Fact]
    public async Task Logout_stores_revocation_and_expiry_in_local_time()
    {
        using var t = TestDb.Seeded();
        var now = new DateTime(2026, 11, 5, 8, 30, 0, DateTimeKind.Local);
        var exp = new DateTimeOffset(now.AddHours(1)).ToUnixTimeSeconds();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, "j1"),
                new Claim(JwtRegisteredClaimNames.Sub, "admin"),
                new Claim(JwtRegisteredClaimNames.Exp, exp.ToString()),
            }, "test")),
        };
        var options = Options.Create(new JwtOptions { Key = new string('k', 64), Issuer = "i", Audience = "a", ExpiresSeconds = 3600 });

        ResultAssert.NoContent(await new AuthService(t.Db, options, new FixedClock(now)).LogoutAsync(http, default));

        var row = t.Db.RevokedTokens.Single();
        Assert.Equal(now, row.RevokedAt);
        Assert.Equal(now.AddHours(1), row.ExpiresAt);
    }
}
