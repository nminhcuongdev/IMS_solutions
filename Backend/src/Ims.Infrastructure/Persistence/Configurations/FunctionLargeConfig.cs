using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class FunctionLargeConfig : IEntityTypeConfiguration<FunctionLarge>
{
    public void Configure(EntityTypeBuilder<FunctionLarge> b)
    {
        b.ToTable("function_large");
        b.HasKey(x => x.LargeCode);
        b.Property(x => x.LargeCode).HasColumnName("large_code").Code(30);
        b.Property(x => x.LargeName).HasColumnName("large_name").Text(50);
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
