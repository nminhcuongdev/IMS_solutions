namespace Ims.Api.Hosting;

internal static class CorsExtensions
{
    public const string FrontendPolicy = "frontend";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:Origins").Get<string[]>()
                      ?? ["http://localhost:5173", "http://127.0.0.1:5173"];
        var allowPrivateNetwork = configuration.GetValue<bool?>("Cors:AllowPrivateNetwork") ?? true;
        var policy = new FrontendOriginPolicy(origins, allowPrivateNetwork);

        services.AddCors(o => o.AddPolicy(FrontendPolicy, p => p
            .SetIsOriginAllowed(policy.IsAllowed)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition")));
        return services;
    }
}
