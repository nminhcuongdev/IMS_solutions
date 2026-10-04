using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ZoneConfig : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> b)
    {
        b.ToTable("zone");
        b.HasKey(x => x.ZoneCode);
        b.Property(x => x.ZoneCode).HasColumnName("zone_code").Code(20);
        b.Property(x => x.ZoneName).HasColumnName("zone_name").Text(100);
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").Code(20);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
        b.HasOne(x => x.Factory).WithMany(f => f.Zones)
         .HasForeignKey(x => x.FactoryCode)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
