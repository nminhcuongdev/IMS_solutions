using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Auth;

internal sealed class AuthService(ImsDbContext db, IOptions<JwtOptions> jwtOpts, IClock? clock = null) : IAuthService
{
    public const string SuperAdminUserCode = "superadmin";

    public async Task<ServiceResult> LoginAsync(LoginRequest body, CancellationToken ct)
    {
        var noUser = string.IsNullOrWhiteSpace(body.UserId);
        var noPassword = string.IsNullOrWhiteSpace(body.Password);
        if (noUser && noPassword) return Outcome.Json(new ApiError(ErrorCatalog.Auth002()), statusCode: 401);
        if (noUser) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("fldUserId", "User ID")));
        if (noPassword) return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001("password", "Password")));

        var user = await db.AppUsers
            .FirstOrDefaultAsync(u => u.UserCode == body.UserId && !u.IsDeleted, ct);
        if (user is null)
            return Outcome.Json(new ApiError(ErrorCatalog.Auth003()), statusCode: 401);
        if (!user.IsActive)
            return Outcome.Json(new ApiError(ErrorCatalog.Auth005Disabled()), statusCode: 403);

        var hasher = new PasswordHasher<AppUser>();
        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, body.Password);
        if (verify == PasswordVerificationResult.Failed)
            return Outcome.Json(new ApiError(ErrorCatalog.Auth004()), statusCode: 401);

        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, body.Password);
            await db.SaveChangesAsync(ct);
        }

        var isSuperAdmin = user.UserCode == SuperAdminUserCode;

        var opts = jwtOpts.Value;
        var jti = Guid.NewGuid().ToString("N");
        var now = (clock ?? new LocalClock()).Now;
        var expiresAt = now.AddSeconds(opts.ExpiresSeconds);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserCode),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.NameIdentifier, user.UserCode),
            new(ClaimTypes.Name, user.UserName),
            new("role_code", user.RoleCode),
            new("is_super_admin", isSuperAdmin ? "1" : "0"),
        };
        if (!string.IsNullOrEmpty(user.FactoryCode))
            claims.Add(new Claim("factory_code", user.FactoryCode));
        if (!string.IsNullOrEmpty(user.DefaultLanguage))
            claims.Add(new Claim("lang", user.DefaultLanguage));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: opts.Issuer,
            audience: opts.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Outcome.Ok(new ApiSuccess<LoginResponseData>(new LoginResponseData(
            Token: jwt,
            ExpiresIn: opts.ExpiresSeconds,
            TokenType: "Bearer",
            UserCode: user.UserCode,
            UserName: user.UserName,
            RoleCode: user.RoleCode,
            IsSuperAdmin: isSuperAdmin,
            DefaultLanguage: user.DefaultLanguage)));
    }

    public async Task<ServiceResult> LogoutAsync(HttpContext http, CancellationToken ct)
    {
        var jti = http.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var sub = http.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var expClaim = http.User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(sub))
            return Outcome.Json(new ApiError(ErrorCatalog.Auth005()), statusCode: 401);

        long expUnix = long.TryParse(expClaim, out var v) ? v : 0;
        var now = (clock ?? new LocalClock()).Now;
        var expiresAt = expUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(expUnix).LocalDateTime : default;

        if (!await db.RevokedTokens.AnyAsync(r => r.Jti == jti, ct))
        {
            db.RevokedTokens.Add(new RevokedToken
            {
                Jti = jti,
                UserCode = sub,
                RevokedAt = now,
                ExpiresAt = expiresAt == default ? now.AddDays(1) : expiresAt,
            });
            await db.SaveChangesAsync(ct);
        }
        return Outcome.NoContent();
    }
}
