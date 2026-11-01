using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Distribution;

namespace Ims.Infrastructure.Persistence.Configurations;

public class DistCheckConfig : IEntityTypeConfiguration<DistCheck>
{
    public void Configure(EntityTypeBuilder<DistCheck> b)
    {
        b.ToTable("dist_check_history");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.DeviceCode).HasColumnName("device_code").Code(20);
        b.Property(x => x.ReceivedAt).HasColumnName("received_at");
        b.Property(x => x.AppVersion).HasColumnName("app_version");
        b.HasIndex(x => new { x.DeviceCode, x.ReceivedAt });
    }
}
