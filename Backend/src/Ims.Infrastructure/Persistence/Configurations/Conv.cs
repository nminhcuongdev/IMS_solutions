using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ims.Infrastructure.Persistence.Configurations;

internal static class Conv
{
    public static PropertyBuilder<string> Code(this PropertyBuilder<string> p, int len)
        => p.IsUnicode(false).HasMaxLength(len).IsRequired();

    public static PropertyBuilder<string?> CodeN(this PropertyBuilder<string?> p, int len)
        => p.IsUnicode(false).HasMaxLength(len);

    public static PropertyBuilder<string> Text(this PropertyBuilder<string> p, int len)
        => p.IsUnicode(true).HasMaxLength(len).IsRequired();

    public static PropertyBuilder<string?> TextN(this PropertyBuilder<string?> p, int len)
        => p.IsUnicode(true).HasMaxLength(len);

    public static void MapAudit<TEntity>(this EntityTypeBuilder<TEntity> b) where TEntity : class
    {
        b.Property("CreatedAt").HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property("CreatedBy").HasColumnName("created_by").HasColumnType("varchar(20)");
        b.Property("UpdatedAt").HasColumnName("updated_at");
        b.Property("UpdatedBy").HasColumnName("updated_by").HasColumnType("varchar(20)");
    }

    public static void MapSoftDelete<TEntity>(this EntityTypeBuilder<TEntity> b) where TEntity : class
    {
        b.Property("IsDeleted").HasColumnName("is_deleted").HasDefaultValue(false);
    }

    public static void MapRemarks<TEntity>(this EntityTypeBuilder<TEntity> b) where TEntity : class
    {
        for (int i = 1; i <= 5; i++)
            b.Property("Remark" + i).HasColumnName("remark_" + i).HasColumnType("nvarchar(200)");
    }
}
