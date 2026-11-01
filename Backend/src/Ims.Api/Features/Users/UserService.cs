using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Users;

internal sealed class UserService(ImsDbContext db, IPasswordHasher<AppUser> hasher) : IUserService
{
    private static readonly SortMap<AppUser> Sort = new SortMap<AppUser>()
        .Add("user_code", u => u.UserCode)
        .Add("user_name", u => u.UserName)
        .Add("role_code", u => u.RoleCode)
        .Add("factory_code", u => u.FactoryCode)
        .Add("default_language", u => u.DefaultLanguage)
        .Add("is_active", u => u.IsActive)
        .Add("remark_1", u => u.Remark1)
        .Add("remark_2", u => u.Remark2)
        .Add("remark_3", u => u.Remark3)
        .Add("remark_4", u => u.Remark4)
        .Add("remark_5", u => u.Remark5)
        .Add("created_at", u => u.CreatedAt)
        .Add("updated_at", u => u.UpdatedAt)
        .Default("user_code")
        .ThenAlwaysBy(u => u.UserCode);

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? roleCode = null)
    {
        var query = db.AppUsers.AsNoTracking().Where(u => !u.IsDeleted);
        if (isActive ?? true) query = query.Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(roleCode)) query = query.Where(u => u.RoleCode == roleCode);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(u => EF.Functions.Like(u.UserCode, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(u.UserName, LikeContains(s), LikeEscape));
        }
        query = Sort.Apply(query, sort);
        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await query.Skip((pg - 1) * sz).Take(sz).Select(u => Map(u)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<UserDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var u = await db.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.UserCode == code && !x.IsDeleted, ct);
        return u is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("User")))
            : Outcome.Ok(new ApiSuccess<UserDto>(Map(u)));
    }

    public async Task<ServiceResult> CreateAsync(UserWriteDto body, CancellationToken ct)
    {
        if (ValidateCode(body.UserCode) is { } e1) return e1;
        if (ValidateName(body.UserName) is { } e2) return e2;
        if (string.IsNullOrWhiteSpace(body.Password))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        var code = body.UserCode.Trim();
        if (await db.AppUsers.AnyAsync(u => u.UserCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        var roleCode = body.RoleCode.Trim();
        if (!await db.Roles.AnyAsync(r => r.RoleCode == roleCode && !r.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Role")));
        if (!string.IsNullOrWhiteSpace(body.FactoryCode)
            && !await db.Factories.AnyAsync(f => f.FactoryCode == body.FactoryCode && !f.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")));
        if (!string.IsNullOrWhiteSpace(body.DefaultLanguage)
            && !await db.Languages.AnyAsync(l => l.LanguageCode == body.DefaultLanguage && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Language")));

        var u = new AppUser
        {
            UserCode = code,
            UserName = body.UserName.Trim(),
            RoleCode = roleCode,
            FactoryCode = NullIfBlank(body.FactoryCode),
            DefaultLanguage = body.DefaultLanguage,
            IsActive = body.IsActive,
            Remark1 = body.Remark1, Remark2 = body.Remark2, Remark3 = body.Remark3,
            Remark4 = body.Remark4, Remark5 = body.Remark5,
            CreatedAt = DateTime.Now,
        };
        u.PasswordHash = hasher.HashPassword(u, body.Password);
        db.AppUsers.Add(u);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/users/{u.UserCode}", new ApiSuccess<UserDto>(Map(u)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, UserWriteDto body, CancellationToken ct)
    {
        var u = await db.AppUsers.FirstOrDefaultAsync(x => x.UserCode == code && !x.IsDeleted, ct);
        if (u is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("User")));
        if (ValidateName(body.UserName) is { } e) return e;
        var roleCode = body.RoleCode.Trim();
        if (!await db.Roles.AnyAsync(r => r.RoleCode == roleCode && !r.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Role")));
        if (!string.IsNullOrWhiteSpace(body.FactoryCode)
            && !await db.Factories.AnyAsync(f => f.FactoryCode == body.FactoryCode && !f.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Factory")));
        if (!string.IsNullOrWhiteSpace(body.DefaultLanguage)
            && !await db.Languages.AnyAsync(l => l.LanguageCode == body.DefaultLanguage && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Language")));

        u.UserName = body.UserName.Trim();
        u.RoleCode = roleCode;
        u.FactoryCode = NullIfBlank(body.FactoryCode);
        u.DefaultLanguage = body.DefaultLanguage;
        u.IsActive = body.IsActive;
        u.Remark1 = body.Remark1; u.Remark2 = body.Remark2; u.Remark3 = body.Remark3;
        u.Remark4 = body.Remark4; u.Remark5 = body.Remark5;
        u.UpdatedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(body.Password))
            u.PasswordHash = hasher.HashPassword(u, body.Password);
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<UserDto>(Map(u)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct)
    {
        var u = await db.AppUsers.FirstOrDefaultAsync(x => x.UserCode == code && !x.IsDeleted, ct);
        if (u is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("User")));
        u.IsDeleted = true; u.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.AppUsers.AsNoTracking().Where(u => !u.IsDeleted).OrderBy(u => u.UserCode).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("user_code,user_name,role_code,factory_code,default_language,is_active,remark_1,remark_2,remark_3,remark_4,remark_5");
        foreach (var u in rows)
            sb.Append(Csv(u.UserCode)).Append(',').Append(Csv(u.UserName)).Append(',')
              .Append(Csv(u.RoleCode)).Append(',').Append(Csv(u.FactoryCode)).Append(',')
              .Append(Csv(u.DefaultLanguage)).Append(',').Append(u.IsActive ? "1" : "0").Append(',')
              .Append(Csv(u.Remark1)).Append(',').Append(Csv(u.Remark2)).Append(',')
              .Append(Csv(u.Remark3)).Append(',').Append(Csv(u.Remark4)).Append(',')
              .Append(Csv(u.Remark5)).AppendLine();
        return CsvFile(sb, "users.csv");
    }

    internal static UserDto Map(AppUser u) => new(
        u.UserCode, u.UserName, u.RoleCode, u.FactoryCode, u.DefaultLanguage, u.IsActive,
        u.Remark1, u.Remark2, u.Remark3, u.Remark4, u.Remark5,
        u.CreatedAt, u.UpdatedAt);
}
