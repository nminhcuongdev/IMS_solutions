using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class PartnerConfig : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> b)
    {
        b.ToTable("partner");
        b.HasKey(x => x.PartnerCode);
        b.Property(x => x.PartnerCode).HasColumnName("partner_code").Code(20);
        b.Property(x => x.PartnerName).HasColumnName("partner_name").Text(100);
        b.Property(x => x.IsSupplier).HasColumnName("is_supplier").HasDefaultValue(false);
        b.Property(x => x.IsCustomer).HasColumnName("is_customer").HasDefaultValue(false);
        b.Property(x => x.Country).HasColumnName("country").TextN(50);
        b.Property(x => x.Address).HasColumnName("address").TextN(500);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
    }
}
