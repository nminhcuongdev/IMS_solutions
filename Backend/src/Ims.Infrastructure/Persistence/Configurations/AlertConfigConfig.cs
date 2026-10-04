using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class AlertConfigConfig : IEntityTypeConfiguration<AlertConfig>
{
    public void Configure(EntityTypeBuilder<AlertConfig> b)
    {
        b.ToTable("alert_config");
        b.HasKey(x => x.AlertTypeCode);

        b.Property(x => x.AlertTypeCode).HasColumnName("alert_type_code").Code(30);
        b.Property(x => x.AlertTypeName).HasColumnName("alert_type_name").Text(100);
        b.Property(x => x.MaxThreshold).HasColumnName("max_threshold").HasColumnType("decimal(18,4)");
        b.Property(x => x.MinThreshold).HasColumnName("min_threshold").HasColumnType("decimal(18,4)");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.Remark1).HasColumnName("remark_1").TextN(200);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").CodeN(20);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").CodeN(20);
    }
}
