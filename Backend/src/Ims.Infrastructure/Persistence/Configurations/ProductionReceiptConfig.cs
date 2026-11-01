using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Production;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ProductionReceiptConfig : IEntityTypeConfiguration<ProductionReceipt>
{
    public void Configure(EntityTypeBuilder<ProductionReceipt> b)
    {
        b.ToTable("production_receipt");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.RequestId).HasColumnName("request_id").Code(50);
        b.Property(x => x.ProductionOrderNo).HasColumnName("production_order_no").Code(30);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").Code(20);
        b.HasIndex(x => x.RequestId).IsUnique();
        b.HasIndex(x => x.ProductionOrderNo);
        b.HasOne<ProductionOrder>().WithMany().HasPrincipalKey(x => x.ProductionOrderNo).HasForeignKey(x => x.ProductionOrderNo).OnDelete(DeleteBehavior.Restrict);
    }
}
