using System.Security.Claims;
using Ims.Api.Features.Permissions;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.System;
using Xunit;

namespace Ims.Api.Tests.Permissions;

public class PermissionEvaluatorTests
{
    [Fact]
    public async Task Grants_require_view_and_super_admin_gets_every_action()
    {
        using var t = TestDb.Seeded();
        t.Db.Roles.Add(new Role { RoleCode = "General", RoleName = "General" });
        t.Db.AppFunctions.Add(new AppFunction { FunctionCode = "prd_result", FunctionName = "Production Result" });
        t.Db.Permissions.Add(new Permission { PermissionCode = "prd_result.view", FunctionCode = "prd_result", Action = "view" });
        t.Db.Permissions.Add(new Permission { PermissionCode = "prd_result.reverse", FunctionCode = "prd_result", Action = "reverse" });
        t.Db.RolePermissions.Add(new RolePermission { RoleCode = "General", PermissionCode = "prd_result.view", IsAllowed = true });
        t.Db.RolePermissions.Add(new RolePermission { RoleCode = "General", PermissionCode = "prd_result.reverse", IsAllowed = false });
        await t.Db.SaveChangesAsync();
        var service = new PermissionEvaluator(t.Db);
        var general = await service.ReadAsync(User("General"), default);
        Assert.True(general.Allows("prd_result", "view"));
        Assert.False(general.Allows("prd_result", "reverse"));
        var super = await service.ReadAsync(User("Worker", true), default);
        Assert.True(super.Allows("prd_result", "reverse"));
    }

    private static ClaimsPrincipal User(string role, bool super = false) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "u"), new Claim("role_code", role),
        new Claim("is_super_admin", super ? "1" : "0")
    }, "test"));
}
