using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ReasonConfig : IEntityTypeConfiguration<Reason>
{
    public void Configure(EntityTypeBuilder<Reason> b)
    {
        b.ToTable("reason");
        b.HasKey(x => x.ReasonCode);
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").Code(20);
        b.Property(x => x.ReasonName).HasColumnName("reason_name").Text(200);
        b.Property(x => x.ReasonCategoryValue).HasColumnName("reason_category").HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
    }
}
