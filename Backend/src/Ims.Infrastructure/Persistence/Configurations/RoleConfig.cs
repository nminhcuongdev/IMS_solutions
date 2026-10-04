using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("role");
        b.HasKey(x => x.RoleCode);
        b.Property(x => x.RoleCode).HasColumnName("role_code").Code(20);
        b.Property(x => x.RoleName).HasColumnName("role_name").Text(50);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapAudit();
        b.MapSoftDelete();
    }
}
