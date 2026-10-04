using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permission");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").UseIdentityColumn();
        b.Property(x => x.RoleCode).HasColumnName("role_code").Code(20);
        b.Property(x => x.PermissionCode).HasColumnName("permission_code").Code(30);
        b.Property(x => x.IsAllowed).HasColumnName("is_allowed").HasDefaultValue(true);
        b.MapAudit();
        b.HasIndex(x => new { x.RoleCode, x.PermissionCode }).IsUnique();
        b.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Restrict);
    }
}
