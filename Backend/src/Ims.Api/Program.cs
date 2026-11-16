using Ims.Api.Features;
using Ims.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddImsSecrets();

builder.Services.AddOpenApi();
builder.Services.AddApiJson();
builder.Services.AddImsPersistence(builder.Configuration);
builder.Services.AddImsAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddMlEngineClient(builder.Configuration);
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddFeatureServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseApiExceptionHandler();
app.UseCors(CorsExtensions.FrontendPolicy);
app.UseAuthentication();
app.UseMiddleware<AuthenticatedAreaMiddleware>();
app.UseAuthorization();
app.UseMiddleware<Ims.Api.Features.Permissions.AccessMiddleware>();

app.MapImsEndpoints();

await app.MigrateAndSeedIfEnabledAsync();

app.Run();

public partial class Program { }
