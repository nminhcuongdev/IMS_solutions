using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ims.Api.Features;
using Ims.Api.Features.Permissions;
using Ims.Api.Hosting;
using Ims.Infrastructure.Persistence;
using Ims.Infrastructure.Persistence.Catalog;
using Xunit;

namespace Ims.Api.Tests.Permissions;

public class EndpointPermissionTests
{
    private static List<RouteEndpoint> Endpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('k', 64), ["Jwt:Issuer"] = "i", ["Jwt:Audience"] = "a",
        });
        builder.Services.AddApiJson();
        builder.Services.AddDbContext<ImsDbContext>(o => o.UseSqlite("DataSource=:memory:"));
        builder.Services.AddImsAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddMlEngineClient(builder.Configuration);
        builder.Services.AddFeatureServices();
        var app = builder.Build();
        app.MapImsEndpoints();
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(d => d.Endpoints).OfType<RouteEndpoint>().ToList();
    }

    [Fact]
    public void Every_authenticated_endpoint_declares_a_permission_or_is_explicitly_open()
    {
        var missing = Endpoints()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .Where(e => e.RoutePattern.RawText != "/health")
            .Where(e => e.Metadata.GetMetadata<PermissionRequirement>() is null && e.Metadata.GetMetadata<AuthenticatedOnly>() is null)
            .Select(e => $"{string.Join(",", e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])} {e.RoutePattern.RawText}")
            .ToList();
        Assert.True(missing.Count == 0, "No permission declared: " + string.Join("; ", missing));
    }

    [Fact]
    public void Every_declared_permission_is_in_the_catalog()
    {
        var known = MenuCatalog.Functions.SelectMany(f => f.Actions.Select(a => MenuCatalog.PermissionCode(f.Code, a))).ToHashSet();
        var unknown = Endpoints()
            .SelectMany(e => e.Metadata.GetMetadata<PermissionRequirement>()?.AnyOf ?? [])
            .Where(c => c != PermissionSnapshot.AnyScreen && !known.Contains(c))
            .Distinct().ToList();
        Assert.True(unknown.Count == 0, "Not in catalog: " + string.Join(", ", unknown));
    }

    [Fact]
    public void Removed_endpoints_are_gone()
    {
        var routes = Endpoints().Select(e => (e.RoutePattern.RawText ?? "").TrimEnd((char)47)).ToHashSet();
        foreach (var gone in new[]
        {
            "/production-output/{no:long}/reverse", "/production-consumption/{no:long}/reverse", "/production-return/{no:long}/reverse",
            "/production-order/{orderNo}/lines", "/production-order/{orderNo}/works", "/production-order/{orderNo}/receipt",
            "/receiving-schedule", "/returns-schedule", "/picking-schedule", "/shipping-schedule", "/traceability/forward",
            "/traceability/backward", "/traceability/events", "/traceability/balance", "/masters/items/export.csv", "/stock/export.csv",
        })
            Assert.DoesNotContain(gone, routes);
        foreach (var kept in new[]
        {
            "/production-result/{no:long}/reverse", "/production-order/{orderNo}/results", "/schedules/receiving",
            "/results/returns", "/traceability", "/masters/roles", "/permissions", "/export/items", "/export/receiving-result", "/results/shipping/{no:long}/reprint",
        })
            Assert.Contains(kept, routes);
    }
}
