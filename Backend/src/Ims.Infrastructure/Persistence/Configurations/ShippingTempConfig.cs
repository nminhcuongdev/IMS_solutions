using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Shipping;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ShippingTempConfig : IEntityTypeConfiguration<ShippingTemp>
{
    public void Configure(EntityTypeBuilder<ShippingTemp> b)
    {
        b.ToTable("shipping_temp");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.SoNo).HasColumnName("so_no").CodeN(30);
        b.Property(x => x.SoBranchNo).HasColumnName("so_branch_no").CodeN(30);
        b.Property(x => x.PartnerCode).HasColumnName("partner_code").CodeN(20);
        b.Property(x => x.ItemCode).HasColumnName("item_code").CodeN(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").CodeN(20);
        b.Property(x => x.ScheduledDate).HasColumnName("scheduled_date");
        b.Property(x => x.Msg).HasColumnName("msg").TextN(500);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").CodeN(20);

        b.HasIndex(x => x.CreatedBy).HasDatabaseName("IX_shipping_temp_created_by");
    }
}
