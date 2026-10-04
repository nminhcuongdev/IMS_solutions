using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class LocationConfig : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> b)
    {
        b.ToTable("location");
        b.HasKey(x => x.LocationCode);
        b.Property(x => x.LocationCode).HasColumnName("location_code").Code(20);
        b.Property(x => x.LocationType).HasColumnName("location_type").HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.LargeCode).HasColumnName("large_code").Code(20);
        b.Property(x => x.MiddleCode).HasColumnName("middle_code").CodeN(20);
        b.Property(x => x.SmallCode).HasColumnName("small_code").CodeN(20);
        b.Property(x => x.LocationName).HasColumnName("location_name").Text(100);
        b.Property(x => x.ZoneCode).HasColumnName("zone_code").CodeN(20);
        b.Property(x => x.ParentLocationCode).HasColumnName("parent_location_code").CodeN(20);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
        b.HasOne(x => x.Zone).WithMany(z => z.Locations)
         .HasForeignKey(x => x.ZoneCode)
         .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Parent).WithMany(p => p.Children)
         .HasForeignKey(x => x.ParentLocationCode)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
