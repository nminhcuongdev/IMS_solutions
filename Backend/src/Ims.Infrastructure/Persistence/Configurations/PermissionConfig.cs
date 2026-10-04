using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permission");
        b.HasKey(x => x.PermissionCode);
        b.Property(x => x.PermissionCode).HasColumnName("permission_code").Code(30);
        b.Property(x => x.FunctionCode).HasColumnName("function_code").Code(30);
        b.Property(x => x.Action).HasColumnName("action").Code(30);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapAudit();
        b.MapSoftDelete();
        b.HasOne<AppFunction>().WithMany().HasForeignKey(x => x.FunctionCode).OnDelete(DeleteBehavior.Restrict);
    }
}
