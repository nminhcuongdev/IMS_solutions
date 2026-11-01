using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Distribution;

namespace Ims.Infrastructure.Persistence.Configurations;

public class DistDownloadConfig : IEntityTypeConfiguration<DistDownload>
{
    public void Configure(EntityTypeBuilder<DistDownload> b)
    {
        b.ToTable("dist_download");
        b.HasKey(x => new { x.DeviceCode, x.RuleId });
        b.Property(x => x.DeviceCode).HasColumnName("device_code").Code(20);
        b.Property(x => x.RuleId).HasColumnName("rule_id");
        b.Property(x => x.CompletedAt).HasColumnName("completed_at");
    }
}
