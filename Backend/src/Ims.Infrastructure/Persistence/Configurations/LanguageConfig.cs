using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class LanguageConfig : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> b)
    {
        b.ToTable("language");
        b.HasKey(x => x.LanguageCode);
        b.Property(x => x.LanguageCode).HasColumnName("language_code").Code(10);
        b.Property(x => x.LanguageName).HasColumnName("language_name").Text(50);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapAudit();
        b.MapSoftDelete();
    }
}
