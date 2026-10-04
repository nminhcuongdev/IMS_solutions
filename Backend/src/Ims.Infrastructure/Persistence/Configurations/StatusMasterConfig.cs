using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class StatusMasterConfig : IEntityTypeConfiguration<StatusMaster>
{
    public void Configure(EntityTypeBuilder<StatusMaster> b)
    {
        b.ToTable("status");
        b.HasKey(x => x.StatusCode);
        b.Property(x => x.StatusCode).HasColumnName("status_code").ValueGeneratedNever();
        b.Property(x => x.StatusNameLang1).HasColumnName("status_name_lang1").Text(30);
        b.Property(x => x.StatusNameLang2).HasColumnName("status_name_lang2").Text(30);
        b.Property(x => x.StatusNameLang3).HasColumnName("status_name_lang3").Text(30);
        b.Property(x => x.Category).HasColumnName("category").CodeN(30);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
