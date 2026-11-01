using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Production;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ProductionOrderLineConfig : IEntityTypeConfiguration<ProductionOrderLine>
{
    public void Configure(EntityTypeBuilder<ProductionOrderLine> b)
    {
        b.ToTable("production_order_line");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.ProductionOrderNo).HasColumnName("production_order_no").Code(30);
        b.Property(x => x.LineNo).HasColumnName("line_no");
        b.Property(x => x.ProcessCode).HasColumnName("process_code").Code(20);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.PlannedQty).HasColumnName("planned_qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.UomCode).HasColumnName("uom_code").Code(10);
        b.Property(x => x.ScheduledDate).HasColumnName("scheduled_date");
        b.Property(x => x.PickedAt).HasColumnName("picked_at");
        b.Property(x => x.PickedBy).HasColumnName("picked_by").CodeN(20);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").Code(20);
        b.HasIndex(x => new { x.ProductionOrderNo, x.LineNo }).IsUnique();
        b.HasOne<ProductionOrder>().WithMany().HasPrincipalKey(x => x.ProductionOrderNo).HasForeignKey(x => x.ProductionOrderNo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Process>().WithMany().HasForeignKey(x => x.ProcessCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Item>().WithMany().HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
    }
}
