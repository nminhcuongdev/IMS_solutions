using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class FactoryConfig : IEntityTypeConfiguration<Factory>
{
    public void Configure(EntityTypeBuilder<Factory> b)
    {
        b.ToTable("factory");
        b.HasKey(x => x.FactoryCode);
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").Code(20);
        b.Property(x => x.FactoryName).HasColumnName("factory_name").Text(100);
        b.Property(x => x.Country).HasColumnName("country").TextN(50);
        b.Property(x => x.Address).HasColumnName("address").TextN(500);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
    }
}
