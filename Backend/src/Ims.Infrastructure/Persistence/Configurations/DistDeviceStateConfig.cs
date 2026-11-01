using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Distribution;

namespace Ims.Infrastructure.Persistence.Configurations;

public class DistDeviceStateConfig : IEntityTypeConfiguration<DistDeviceState>
{
    public void Configure(EntityTypeBuilder<DistDeviceState> b)
    {
        b.ToTable("dist_device_state");
        b.HasKey(x => x.DeviceCode);
        b.Property(x => x.DeviceCode).HasColumnName("device_code").Code(20);
        b.Property(x => x.VersionCode).HasColumnName("version_code");
        b.Property(x => x.LastCheckAt).HasColumnName("last_check_at");
        b.Property(x => x.LastUpdateAt).HasColumnName("last_update_at");
    }
}
