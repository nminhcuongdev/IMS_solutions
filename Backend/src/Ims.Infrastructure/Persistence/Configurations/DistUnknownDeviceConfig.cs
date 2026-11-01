using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Distribution;

namespace Ims.Infrastructure.Persistence.Configurations;

public class DistUnknownDeviceConfig : IEntityTypeConfiguration<DistUnknownDevice>
{
    public void Configure(EntityTypeBuilder<DistUnknownDevice> b)
    {
        b.ToTable("dist_unknown_device");
        b.HasKey(x => x.DeviceCode);
        b.Property(x => x.DeviceCode).HasColumnName("device_code").Code(50);
        b.Property(x => x.FirstAt).HasColumnName("first_at");
        b.Property(x => x.LastAt).HasColumnName("last_at");
        b.Property(x => x.RequestCount).HasColumnName("request_count");
    }
}
