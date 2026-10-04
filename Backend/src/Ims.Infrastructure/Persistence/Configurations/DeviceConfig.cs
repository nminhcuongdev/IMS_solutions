using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class DeviceConfig : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> b)
    {
        b.ToTable("m_device");
        b.HasKey(x => x.DeviceCode);
        b.Property(x => x.DeviceCode).HasColumnName("device_code").Code(20);
        b.Property(x => x.DeviceName).HasColumnName("device_name").Text(100);
        b.Property(x => x.DeviceType).HasColumnName("device_type")
         .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Model).HasColumnName("model").TextN(50);
        b.Property(x => x.Manufacturer).HasColumnName("manufacturer").TextN(50);
        b.Property(x => x.Spec).HasColumnName("spec").TextN(100);
        b.Property(x => x.SerialNo).HasColumnName("serial_no").CodeN(100);
        b.Property(x => x.IpAddress).HasColumnName("ip_address").CodeN(50);
        b.Property(x => x.LocationCode).HasColumnName("location_code").CodeN(20);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
        b.HasOne(x => x.Location).WithMany()
         .HasForeignKey(x => x.LocationCode)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
