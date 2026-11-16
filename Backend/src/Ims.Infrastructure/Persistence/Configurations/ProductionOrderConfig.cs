using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Production;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ProductionOrderConfig : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> b)
    {
        b.ToTable("production_order");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.ProductionOrderNo).HasColumnName("production_order_no").Code(30);
        b.Property(x => x.OrderDate).HasColumnName("order_date");
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").Code(20);
        b.Property(x => x.OutputItemCode).HasColumnName("output_item_code").Code(20);
        b.Property(x => x.PlannedQty).HasColumnName("planned_qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.UomCode).HasColumnName("uom_code").Code(10);
        b.Property(x => x.OutputLotNo).HasColumnName("output_lot_no").CodeN(20);
        b.Property(x => x.ProductionLotNo).HasColumnName("production_lot_no").CodeN(20);
        b.Property(x => x.StorageLocationCode).HasColumnName("storage_location_code").Code(20);
        b.Property(x => x.PlannedStartDate).HasColumnName("planned_start_date");
        b.Property(x => x.PlannedEndDate).HasColumnName("planned_end_date");
        b.Property(x => x.StatusCode).HasColumnName("status_code");
        b.Property(x => x.CancelReasonCode).HasColumnName("cancel_reason_code").CodeN(20);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").CodeN(20);
        b.Property(x => x.ReceivedAt).HasColumnName("received_at");
        b.Property(x => x.ReceivedBy).HasColumnName("received_by").CodeN(20);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").Code(20);
        b.HasIndex(x => x.ProductionOrderNo).IsUnique();
        b.HasIndex(x => new { x.StatusCode, x.PlannedStartDate });
        b.HasIndex(x => new { x.OutputItemCode, x.OutputLotNo });
        b.HasOne<Ims.Domain.Masters.Factory>().WithMany().HasForeignKey(x => x.FactoryCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Item>().WithMany().HasForeignKey(x => x.OutputItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany().HasForeignKey(x => x.StorageLocationCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Reason>().WithMany().HasForeignKey(x => x.CancelReasonCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.System.StatusMaster>().WithMany().HasForeignKey(x => x.StatusCode).OnDelete(DeleteBehavior.Restrict);
    }
}
