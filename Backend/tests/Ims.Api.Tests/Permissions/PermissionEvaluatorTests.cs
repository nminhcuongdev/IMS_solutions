using System.Security.Claims;
using Ims.Api.Features.Permissions;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Ims.Domain.System;
using Xunit;

namespace Ims.Api.Tests.Permissions;

public class PermissionEvaluatorTests
{
    internal static void SeedCatalog(TestDb t)
    {
        t.Db.Roles.Add(new Role { RoleCode = "LEADER", RoleName = "Leader" });
        t.Db.FunctionLarges.Add(new FunctionLarge { LargeCode = "masterMgmt", LargeName = "Master Data" });
        t.Db.FunctionLarges.Add(new FunctionLarge { LargeCode = "productionMgmt", LargeName = "Production" });
        t.Db.AppFunctions.Add(new AppFunction { FunctionCode = "m_item", FunctionName = "Item Master", LargeCode = "masterMgmt" });
        t.Db.AppFunctions.Add(new AppFunction { FunctionCode = "prd_result", FunctionName = "Production Result", LargeCode = "productionMgmt" });
        foreach (var (f, a) in new[] { ("m_item", "Open"), ("m_item", "New"), ("prd_result", "Open"), ("prd_result", "Reverse") })
            t.Db.Permissions.Add(new Permission { PermissionCode = f + "." + a, FunctionCode = f, ActionType = a });
        t.Db.AppUsers.Add(new AppUser { UserCode = "lead", UserName = "Lead", RoleCode = "LEADER", PasswordHash = "x" });
        t.Db.SaveChanges();
    }

    internal static ClaimsPrincipal User(string code, bool super = false) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, code), new Claim("is_super_admin", super ? "1" : "0"),
    }, "test"));

    [Fact]
    public async Task Role_is_read_from_the_user_master_and_buttons_need_their_row()
    {
        using var t = TestDb.Seeded();
        SeedCatalog(t);
        t.Db.RolePermissions.Add(new RolePermission { RoleCode = "LEADER", PermissionCode = "m_item.Open", IsAllowed = true });
        await t.Db.SaveChangesAsync();

        var rights = await new PermissionEvaluator(t.Db).ReadAsync(User("lead"), default);

        Assert.Equal("LEADER", rights.RoleCode);
        Assert.True(rights.Allows("m_item", "Open"));
        Assert.False(rights.Allows("m_item", "New"));
        Assert.False(rights.Allows("prd_result", "Open"));
    }

    [Fact]
    public async Task A_button_without_its_screen_is_not_allowed()
    {
        using var t = TestDb.Seeded();
        SeedCatalog(t);
        t.Db.RolePermissions.Add(new RolePermission { RoleCode = "LEADER", PermissionCode = "m_item.New", IsAllowed = true });
        await t.Db.SaveChangesAsync();

        var rights = await new PermissionEvaluator(t.Db).ReadAsync(User("lead"), default);

        Assert.False(rights.Allows("m_item", "New"));
    }

    [Fact]
    public async Task Builtin_role_and_super_admin_get_every_row()
    {
        using var t = TestDb.Seeded();
        SeedCatalog(t);
        t.Db.AppUsers.Add(new AppUser { UserCode = "boss", UserName = "Boss", RoleCode = "ADMIN", PasswordHash = "x" });
        await t.Db.SaveChangesAsync();
        var evaluator = new PermissionEvaluator(t.Db);

        Assert.True((await evaluator.ReadAsync(User("boss"), default)).Allows("prd_result", "Reverse"));
        Assert.True((await evaluator.ReadAsync(User("lead", super: true), default)).Allows("prd_result", "Reverse"));
    }

    [Fact]
    public async Task Disabled_module_and_hidden_function_drop_their_rows()
    {
        using var t = TestDb.Seeded();
        SeedCatalog(t);
        t.Db.GeneralSettings.Add(new GeneralSetting { ConfigCode = "module_production", ConfigValue = "0" });
        t.Db.AppFunctions.Find("m_item")!.IsVisible = false;
        await t.Db.SaveChangesAsync();

        var rights = await new PermissionEvaluator(t.Db).ReadAsync(User("lead", super: true), default);

        Assert.Contains("production", rights.DisabledModules);
        Assert.False(rights.Allows("prd_result", "Open"));
        Assert.False(rights.Allows("m_item", "Open"));
    }
}
