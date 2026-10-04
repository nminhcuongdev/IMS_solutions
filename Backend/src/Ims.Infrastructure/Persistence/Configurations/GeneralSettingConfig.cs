using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class GeneralSettingConfig : IEntityTypeConfiguration<GeneralSetting>
{
    public void Configure(EntityTypeBuilder<GeneralSetting> b)
    {
        b.ToTable("general");
        b.HasKey(x => x.ConfigCode);
        b.Property(x => x.ConfigCode).HasColumnName("config_code").Code(50);
        b.Property(x => x.ConfigValue).HasColumnName("config_value").HasColumnType("nvarchar(MAX)");
        b.Property(x => x.Category).HasColumnName("category").CodeN(30);
        b.Property(x => x.Description).HasColumnName("description").HasColumnType("nvarchar(200)");
        b.MapAudit();
    }
}
