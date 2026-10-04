using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("app_user");
        b.HasKey(x => x.UserCode);
        b.Property(x => x.UserCode).HasColumnName("user_code").Code(20);
        b.Property(x => x.UserName).HasColumnName("user_name").Text(100);
        b.Property(x => x.PasswordHash).HasColumnName("password").HasColumnType("varchar(256)").IsRequired();
        b.Property(x => x.RoleCode).HasColumnName("role_code").Code(20);
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").CodeN(20);
        b.Property(x => x.DefaultLanguage).HasColumnName("default_language").CodeN(10);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
    }
}
