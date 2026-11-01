using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Inventory;

namespace Ims.Infrastructure.Persistence.Configurations;

public class InventoryHistoryConfig : IEntityTypeConfiguration<InventoryHistory>
{
    public void Configure(EntityTypeBuilder<InventoryHistory> b)
    {
        b.ToTable("inventory_history");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").CodeN(20);
        b.Property(x => x.LocationCode).HasColumnName("location_code").CodeN(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.SerialNo).HasColumnName("serial_no").CodeN(30);
        b.Property(x => x.ActionType).HasColumnName("action_type").Code(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").CodeN(20);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").CodeN(20);

        b.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_inventory_history_created_at");
        b.HasIndex(x => x.ActionType).HasDatabaseName("IX_inventory_history_action_type");
        b.HasIndex(x => new { x.ItemCode, x.LotNo });

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
