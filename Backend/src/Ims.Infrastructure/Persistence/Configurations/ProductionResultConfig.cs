using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Production;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ProductionResultConfig : IEntityTypeConfiguration<ProductionResult>
{
    public void Configure(EntityTypeBuilder<ProductionResult> b)
    {
        b.ToTable("production_result", t =>
        {
            t.HasCheckConstraint("CK_production_result_qty", "[qty] > 0");
            t.HasCheckConstraint("CK_production_result_line",
                "([result_type] = 'Output' AND [line_no] IS NULL) OR ([result_type] <> 'Output' AND [line_no] IS NOT NULL)");
            t.HasCheckConstraint("CK_production_result_type", "[result_type] IN ('Consume', 'Return', 'Output')");
        });
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.RequestId).HasColumnName("request_id").Code(50);
        b.Property(x => x.ProductionOrderNo).HasColumnName("production_order_no").Code(30);
        b.Property(x => x.ResultType).HasColumnName("result_type").Code(10);
        b.Property(x => x.LineNo).HasColumnName("line_no");
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.SerialNo).HasColumnName("serial_no").Code(30);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.LocationCode).HasColumnName("location_code").Code(20);
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").Code(20);
        b.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
        b.Property(x => x.ReversesNo).HasColumnName("reverses_no");
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").CodeN(20);
        b.Property(x => x.LabelIssuedAt).HasColumnName("label_issued_at");
        b.Property(x => x.LabelIssuedBy).HasColumnName("label_issued_by").CodeN(20);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").Code(20);
        b.HasIndex(x => new { x.RequestId, x.SerialNo }).IsUnique().HasFilter("[reverses_no] IS NULL");
        b.HasIndex(x => x.ReversesNo).IsUnique().HasFilter("[reverses_no] IS NOT NULL");
        b.HasIndex(x => new { x.ItemCode, x.LotNo });
        b.HasIndex(x => new { x.ProductionOrderNo, x.ResultType, x.LineNo });
        b.HasOne<ProductionOrder>().WithMany().HasPrincipalKey(x => x.ProductionOrderNo).HasForeignKey(x => x.ProductionOrderNo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductionOrderLine>().WithMany().HasPrincipalKey(x => new { x.ProductionOrderNo, x.LineNo }).HasForeignKey(x => new { x.ProductionOrderNo, x.LineNo }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductionResult>().WithMany().HasForeignKey(x => x.ReversesNo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Item>().WithMany().HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany().HasForeignKey(x => x.LocationCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Factory>().WithMany().HasForeignKey(x => x.FactoryCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Reason>().WithMany().HasForeignKey(x => x.ReasonCode).OnDelete(DeleteBehavior.Restrict);
    }
}
