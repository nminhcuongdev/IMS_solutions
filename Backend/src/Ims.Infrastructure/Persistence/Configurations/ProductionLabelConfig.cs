using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Production;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ProductionLabelConfig : IEntityTypeConfiguration<ProductionLabel>
{
    public void Configure(EntityTypeBuilder<ProductionLabel> b)
    {
        b.ToTable("production_label");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.RequestId).HasColumnName("request_id").Code(50);
        b.Property(x => x.OutputRequestId).HasColumnName("output_request_id").Code(50);
        b.Property(x => x.ProductionOrderNo).HasColumnName("production_order_no").Code(30);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").Code(20);
        b.Property(x => x.TotalQty).HasColumnName("total_qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.StdQty).HasColumnName("std_qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.LabelCount).HasColumnName("label_count");
        b.Property(x => x.RemainderQty).HasColumnName("remainder_qty").HasColumnType("decimal(18,4)").HasDefaultValue(0);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").Code(20);
        b.HasIndex(x => x.RequestId).IsUnique();
        b.HasIndex(x => x.OutputRequestId).IsUnique();
        b.HasIndex(x => x.ProductionOrderNo);
        b.HasOne<ProductionOrder>().WithMany().HasPrincipalKey(x => x.ProductionOrderNo).HasForeignKey(x => x.ProductionOrderNo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Item>().WithMany().HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
    }
}
