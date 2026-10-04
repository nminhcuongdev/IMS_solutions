using Ims.Api.Features.Print;

namespace Ims.Api.Hosting;

internal static class MlEngineClientExtensions
{
    public static IServiceCollection AddMlEngineClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IMlEngineClient, HttpMlEngineClient>(http =>
        {
            http.BaseAddress = new Uri(configuration["MlEngine:BaseUrl"] ?? "http://localhost:8080/");
            http.Timeout = TimeSpan.FromSeconds(15);
        });
        return services;
    }
}
