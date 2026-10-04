using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Inventory;

namespace Ims.Infrastructure.Persistence.Configurations;

public class StockConfig : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> b)
    {
        b.ToTable("stock");
        b.HasKey(x => new { x.ItemCode, x.SerialNo });

        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.SerialNo).HasColumnName("serial_no").Code(30);
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").Code(20);
        b.Property(x => x.LocationCode).HasColumnName("location_code").CodeN(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);

        b.Property(x => x.StockQty).HasColumnName("stock_qty").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
        b.Property(x => x.ReservedQty).HasColumnName("reserved_qty").HasColumnType("decimal(18,4)").HasDefaultValue(0m);
        b.Property(x => x.AvailableQty).HasColumnName("available_qty").HasColumnType("decimal(18,4)").HasDefaultValue(0m);

        b.Property(x => x.IsOnHold).HasColumnName("is_on_hold").HasDefaultValue(false);
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").CodeN(20);
        b.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").CodeN(20);

        b.HasIndex(x => x.LotNo).HasDatabaseName("IX_stock_lot_no");
        b.HasIndex(x => x.LocationCode).HasDatabaseName("IX_stock_location_code");
        b.HasIndex(x => x.IsOnHold).HasDatabaseName("IX_stock_is_on_hold");

        b.HasOne(x => x.Item).WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Location).WithMany()
         .HasForeignKey(x => x.LocationCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Factory>().WithMany()
         .HasForeignKey(x => x.FactoryCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Reason>().WithMany()
         .HasForeignKey(x => x.ReasonCode).OnDelete(DeleteBehavior.Restrict);
    }
}
