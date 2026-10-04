using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class AppFunctionConfig : IEntityTypeConfiguration<AppFunction>
{
    public void Configure(EntityTypeBuilder<AppFunction> b)
    {
        b.ToTable("app_function");
        b.HasKey(x => x.FunctionCode);
        b.Property(x => x.FunctionCode).HasColumnName("function_code").Code(30);
        b.Property(x => x.FunctionName).HasColumnName("function_name").Text(100);
        b.Property(x => x.LargeCode).HasColumnName("large_code").CodeN(30);
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapAudit();
        b.MapSoftDelete();
        b.HasOne<FunctionLarge>().WithMany().HasForeignKey(x => x.LargeCode).OnDelete(DeleteBehavior.Restrict);
    }
}
