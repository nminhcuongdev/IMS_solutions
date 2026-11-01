using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Shipping;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ShippingResultLotConfig : IEntityTypeConfiguration<ShippingResultLot>
{
    public void Configure(EntityTypeBuilder<ShippingResultLot> b)
    {
        b.ToTable("shipping_result_lot");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.ShippingResultNo).HasColumnName("shipping_result_no");
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.SerialNo).HasColumnName("serial_no").Code(30);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").Code(20);
        b.HasIndex(x => new { x.ItemCode, x.LotNo });
        b.HasOne(x => x.ShippingResult).WithMany().HasForeignKey(x => x.ShippingResultNo).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Ims.Domain.Masters.Item>().WithMany().HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
    }
}
