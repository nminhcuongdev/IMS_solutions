using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class DefaultValueConfig : IEntityTypeConfiguration<DefaultValue>
{
    public void Configure(EntityTypeBuilder<DefaultValue> b)
    {
        b.ToTable("default_value");
        b.HasKey(x => new { x.ValueType, x.ValueCode });
        b.Property(x => x.ValueType).HasColumnName("value_type").Code(30);
        b.Property(x => x.ValueCode).HasColumnName("value_code").Code(30);
        b.Property(x => x.NameLang1).HasColumnName("name_lang1").Text(50);
        b.Property(x => x.NameLang2).HasColumnName("name_lang2").Text(50);
        b.Property(x => x.NameLang3).HasColumnName("name_lang3").Text(50);
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapAudit();
    }
}
