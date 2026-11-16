using Ims.Api.Contracts;
using Ims.Api.Features.Permissions;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.System;
using Xunit;

namespace Ims.Api.Tests.Permissions;

public class PermissionServiceTests
{
    private static PermissionService Service(TestDb t) => new(t.Db, new PermissionEvaluator(t.Db));

    [Fact]
    public async Task Matrix_of_a_new_role_is_all_disabled()
    {
        using var t = TestDb.Seeded();
        PermissionEvaluatorTests.SeedCatalog(t);

        var rows = ResultAssert.Ok<ApiSuccess<IReadOnlyList<PermissionMatrixRowDto>>>(
            await Service(t).MatrixAsync("LEADER", default)).Data;

        Assert.Equal(4, rows.Count);
        Assert.All(rows, r => Assert.False(r.IsAllowed));
        Assert.All(rows, r => Assert.False(r.Locked));
        Assert.Equal(new[] { "Open", "New" }, rows.Where(r => r.FunctionCode == "m_item").Select(r => r.ActionType));
    }

    [Fact]
    public async Task Builtin_matrix_is_all_allowed_and_locked_and_cannot_be_saved()
    {
        using var t = TestDb.Seeded();
        PermissionEvaluatorTests.SeedCatalog(t);
        var service = Service(t);

        var rows = ResultAssert.Ok<ApiSuccess<IReadOnlyList<PermissionMatrixRowDto>>>(
            await service.MatrixAsync("ADMIN", default)).Data;
        Assert.All(rows, r => Assert.True(r.IsAllowed && r.Locked));

        var save = await service.SaveAsync(new PermissionMatrixSaveDto("ADMIN",
            [new PermissionMatrixItemDto("m_item.Open", false)]), PermissionEvaluatorTests.User("x"), default);
        ResultAssert.Error(save, 403, "AUTH-006");
    }

    [Fact]
    public async Task Save_writes_only_the_given_rows_and_unknown_role_is_not_found()
    {
        using var t = TestDb.Seeded();
        PermissionEvaluatorTests.SeedCatalog(t);
        t.Db.RolePermissions.Add(new RolePermission { RoleCode = "LEADER", PermissionCode = "prd_result.Open", IsAllowed = true });
        await t.Db.SaveChangesAsync();
        var service = Service(t);

        var rows = ResultAssert.Ok<ApiSuccess<IReadOnlyList<PermissionMatrixRowDto>>>(await service.SaveAsync(
            new PermissionMatrixSaveDto("LEADER", [new PermissionMatrixItemDto("m_item.Open", true)]),
            PermissionEvaluatorTests.User("x"), default)).Data;

        Assert.True(rows.Single(r => r.PermissionCode == "m_item.Open").IsAllowed);
        Assert.True(rows.Single(r => r.PermissionCode == "prd_result.Open").IsAllowed);
        Assert.False(rows.Single(r => r.PermissionCode == "m_item.New").IsAllowed);
        ResultAssert.NotFound(await service.MatrixAsync("NOPE", default));
    }
}
