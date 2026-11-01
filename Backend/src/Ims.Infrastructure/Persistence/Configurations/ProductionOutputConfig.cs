using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Production;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ProductionOutputConfig : IEntityTypeConfiguration<ProductionOutput>
{
    public void Configure(EntityTypeBuilder<ProductionOutput> b)
    {
        b.ToTable("production_output");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.RequestId).HasColumnName("request_id").Code(50);
        b.Property(x => x.ProductionOrderNo).HasColumnName("production_order_no").Code(30);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").Code(20);
        b.Property(x => x.SerialNo).HasColumnName("serial_no").Code(30);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.LocationCode).HasColumnName("location_code").Code(20);
        b.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
        b.Property(x => x.ReversesNo).HasColumnName("reverses_no");
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").CodeN(20);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").Code(20);
        b.HasIndex(x => new { x.RequestId, x.SerialNo }).IsUnique().HasFilter("[reverses_no] IS NULL");
        b.HasIndex(x => x.ReversesNo).IsUnique().HasFilter("[reverses_no] IS NOT NULL");
        b.HasIndex(x => new { x.ItemCode, x.LotNo });
        b.HasIndex(x => x.ProductionOrderNo);
        b.HasOne<ProductionOrder>().WithMany().HasPrincipalKey(x => x.ProductionOrderNo).HasForeignKey(x => x.ProductionOrderNo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductionOutput>().WithMany().HasForeignKey(x => x.ReversesNo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Item>().WithMany().HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany().HasForeignKey(x => x.LocationCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Reason>().WithMany().HasForeignKey(x => x.ReasonCode).OnDelete(DeleteBehavior.Restrict);
    }
}
