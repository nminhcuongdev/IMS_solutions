using Ims.Api.Contracts;
using Ims.Api.Features.Roles;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;
using Xunit;

namespace Ims.Api.Tests.Permissions;

public class RoleServiceTests
{
    private static readonly System.Security.Claims.ClaimsPrincipal Who = PermissionEvaluatorTests.User("admin");

    [Fact]
    public async Task Create_validates_code_and_name()
    {
        using var t = TestDb.Seeded();
        var service = new RoleService(t.Db);

        ResultAssert.BadRequest(await service.CreateAsync(new RoleWriteDto("", "Leader"), Who, default), "VAL-001");
        ResultAssert.BadRequest(await service.CreateAsync(new RoleWriteDto("LE#AD", "Leader"), Who, default), "VAL-005");
        ResultAssert.BadRequest(await service.CreateAsync(new RoleWriteDto("LEADER", " "), Who, default), "VAL-001");
        ResultAssert.Conflict(await service.CreateAsync(new RoleWriteDto("  ADMIN ", "Again"), Who, default));
        var created = ResultAssert.Created<ApiSuccess<RoleDto>>(
            await service.CreateAsync(new RoleWriteDto(" LEADER ", "Leader"), Who, default)).Data;
        Assert.Equal("LEADER", created.RoleCode);
        Assert.False(created.IsBuiltin);
    }

    [Fact]
    public async Task Builtin_role_cannot_be_edited_or_deleted()
    {
        using var t = TestDb.Seeded();
        var service = new RoleService(t.Db);

        ResultAssert.Error(await service.UpdateAsync("ADMIN", new RoleWriteDto(null, "X"), Who, default), 403, "AUTH-006");
        ResultAssert.Error(await service.DeleteAsync("ADMIN", Who, default), 403, "AUTH-006");
    }

    [Fact]
    public async Task Edit_changes_the_name_only_and_delete_is_refused_while_users_hold_it()
    {
        using var t = TestDb.Seeded();
        var service = new RoleService(t.Db);
        ResultAssert.Created<ApiSuccess<RoleDto>>(await service.CreateAsync(new RoleWriteDto("LEADER", "Leader"), Who, default));
        t.Db.AppUsers.Add(new AppUser { UserCode = "u1", UserName = "U1", RoleCode = "LEADER", PasswordHash = "x" });
        await t.Db.SaveChangesAsync();

        var edited = ResultAssert.Ok<ApiSuccess<RoleDto>>(
            await service.UpdateAsync("LEADER", new RoleWriteDto("OTHER", "Team leader"), Who, default)).Data;
        Assert.Equal("LEADER", edited.RoleCode);
        Assert.Equal("Team leader", edited.RoleName);
        Assert.Equal(1, edited.UserCount);

        ResultAssert.InUse(await service.DeleteAsync("LEADER", Who, default));
        t.Db.AppUsers.Single(u => u.UserCode == "u1").IsDeleted = true;
        await t.Db.SaveChangesAsync();
        ResultAssert.NoContent(await service.DeleteAsync("LEADER", Who, default));
        Assert.True(t.Db.Roles.Single(r => r.RoleCode == "LEADER").IsDeleted);
    }
}
