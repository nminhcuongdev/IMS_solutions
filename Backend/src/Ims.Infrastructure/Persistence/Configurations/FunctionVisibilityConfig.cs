using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class FunctionVisibilityConfig : IEntityTypeConfiguration<FunctionVisibility>
{
    public void Configure(EntityTypeBuilder<FunctionVisibility> b)
    {
        b.ToTable("function_visibility");
        b.HasKey(x => new { x.TargetType, x.TargetCode });
        b.Property(x => x.TargetType).HasColumnName("target_type").Code(20);
        b.Property(x => x.TargetCode).HasColumnName("target_code").Code(30);
        b.Property(x => x.IsVisible).HasColumnName("is_visible").HasDefaultValue(true);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasColumnType("varchar(20)");
    }
}
