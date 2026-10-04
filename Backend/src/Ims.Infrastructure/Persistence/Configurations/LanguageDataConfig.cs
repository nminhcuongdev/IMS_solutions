using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class LanguageDataConfig : IEntityTypeConfiguration<LanguageData>
{
    public void Configure(EntityTypeBuilder<LanguageData> b)
    {
        b.ToTable("language_data");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").UseIdentityColumn();
        b.Property(x => x.LabelKey).HasColumnName("label_key").Code(100);
        b.Property(x => x.LanguageCode).HasColumnName("language_code").Code(10);
        b.Property(x => x.LabelText).HasColumnName("label_text").Text(500);
        b.Property(x => x.DeviceScope).HasColumnName("device_scope").CodeN(10);
        b.MapAudit();
        b.HasIndex(x => new { x.LabelKey, x.LanguageCode, x.DeviceScope }).IsUnique().HasFilter(null);
    }
}
