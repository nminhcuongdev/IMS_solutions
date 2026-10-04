using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Ims.Api.Contracts;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Hosting;

internal static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddImsAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtSection = configuration.GetSection("Jwt");
        services.Configure<JwtOptions>(jwtSection);
        var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
        if (environment.IsProduction() && !JwtKeyPolicy.IsAcceptableForProduction(jwt.Key))
            throw new InvalidOperationException(
                $"Jwt:Key is missing, shorter than {JwtKeyPolicy.MinimumLength} characters or still a placeholder. "
                + $"Put a random key in {SecretsConfigurationExtensions.FileName} next to the API before starting it in Production.");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                o.Events = new JwtBearerEvents
                {
                    OnTokenValidated = RejectRevokedTokenAsync,
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        await ApiErrorWriter.WriteAsync(ctx.Response, 401, new ApiError(ErrorCatalog.Auth005()));
                    },
                };
            });
        services.AddAuthorization();
        return services;
    }

    private static async Task RejectRevokedTokenAsync(TokenValidatedContext ctx)
    {
        var jti = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(jti))
        {
            ctx.Fail("missing jti");
            return;
        }
        var db = ctx.HttpContext.RequestServices.GetRequiredService<ImsDbContext>();
        var revoked = await db.RevokedTokens.AsNoTracking()
            .AnyAsync(r => r.Jti == jti, ctx.HttpContext.RequestAborted);
        if (revoked) ctx.Fail("token revoked");
    }
}
