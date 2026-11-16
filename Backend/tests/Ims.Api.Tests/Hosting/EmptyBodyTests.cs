using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ims.Api.Contracts;
using Ims.Api.Features;
using Ims.Api.Hosting;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using Xunit;

namespace Ims.Api.Tests.Hosting;

public class EmptyBodyTests
{
    private const string Filled = "X1";

    private static readonly ClaimsPrincipal Admin = new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "U001"), new Claim(ClaimTypes.Role, "ADMIN")], "test"));

    private static readonly Lazy<WebApplication> App = new(() =>
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('k', 64), ["Jwt:Issuer"] = "i", ["Jwt:Audience"] = "a",
        });
        builder.Services.AddApiJson();
        builder.Services.AddImsAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddMlEngineClient(builder.Configuration);
        builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        builder.Services.AddFeatureServices();
        builder.Services.AddScoped<ImsDbContext>(_ => CurrentDb.Value!);
        return builder.Build();
    });

    private static readonly AsyncLocal<ImsDbContext?> CurrentDb = new();

    private static IEnumerable<(Type Service, MethodInfo Method)> Writes() =>
        typeof(FeatureServiceRegistration).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.StartsWith('I') && t.Name.EndsWith("Service")
                        && t.Namespace?.StartsWith("Ims.Api.Features") == true)
            .SelectMany(t => t.GetMethods().Select(m => (t, m)))
            .Where(x => x.m.ReturnType == typeof(Task<ServiceResult>) && x.m.GetParameters().Any(IsBody));

    public static IEnumerable<object[]> Cases()
    {
        foreach (var (service, method) in Writes())
        {
            var body = method.GetParameters().First(IsBody).ParameterType;
            yield return [service.Name, method.Name, method.GetParameters().Length, ""];
            foreach (var p in body.GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanWrite))
                yield return [service.Name, method.Name, method.GetParameters().Length, p.Name];
        }
    }

    private static bool IsBody(ParameterInfo p) =>
        p.ParameterType.IsClass && p.ParameterType != typeof(string) && !p.ParameterType.IsInterface
        && p.ParameterType.Namespace?.StartsWith("Ims.Api.Features") == true;

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task A_missing_field_is_never_a_server_error(string service, string method, int arity, string missing)
    {
        using var t = TestDb.Seeded();
        var type = typeof(FeatureServiceRegistration).Assembly.GetTypes().Single(x => x.IsInterface && x.Name == service);
        CurrentDb.Value = t.Db;
        using var scope = App.Value.Services.CreateScope();
        var target = scope.ServiceProvider.GetRequiredService(type);
        var m = type.GetMethods().Single(x => x.Name == method && x.GetParameters().Length == arity && x.GetParameters().Any(IsBody));
        var args = m.GetParameters().Select(p => Argument(p, missing)).ToArray();

        ServiceResult result;
        try
        {
            result = await (Task<ServiceResult>)m.Invoke(target, args)!;
        }
        catch (Exception ex)
        {
            var inner = ex is TargetInvocationException { InnerException: { } i } ? i : ex;
            var (status, _) = ExceptionErrors.For(inner);
            Assert.True(status < 500, $"{service}.{method} without {missing}: {inner.GetType().Name} {inner.Message}");
            return;
        }
        Assert.True(result.StatusCode < 500, $"{service}.{method} without {missing}: {result.StatusCode}");
    }

    private static object? Argument(ParameterInfo p, string missing)
    {
        if (IsBody(p))
        {
            var body = RuntimeHelpers.GetUninitializedObject(p.ParameterType);
            if (missing.Length == 0) return body;
            foreach (var prop in p.ParameterType.GetProperties().Where(x => x.PropertyType == typeof(string) && x.CanWrite))
                prop.SetValue(body, prop.Name == missing ? null : Filled);
            return body;
        }
        if (p.ParameterType == typeof(ClaimsPrincipal)) return Admin;
        if (p.ParameterType == typeof(CancellationToken)) return CancellationToken.None;
        if (p.ParameterType == typeof(string)) return Filled;
        if (p.HasDefaultValue) return p.DefaultValue;
        return p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
    }
}
