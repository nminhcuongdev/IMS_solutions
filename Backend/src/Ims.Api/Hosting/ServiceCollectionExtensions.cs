using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Hosting;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImsPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ImsDbContext>(opt => opt.UseSqlServer(configuration.GetConnectionString("Ims")));
        services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        return services;
    }

    public static IServiceCollection AddApiJson(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        });
        services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
        return services;
    }
}
