using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Receiving;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ReceivingResultConfig : IEntityTypeConfiguration<ReceivingResult>
{
    public void Configure(EntityTypeBuilder<ReceivingResult> b)
    {
        b.ToTable("receiving_result");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.PoNo).HasColumnName("po_no").CodeN(30);
        b.Property(x => x.PoBranchNo).HasColumnName("po_branch_no").CodeN(30);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.LocationCode).HasColumnName("location_code").CodeN(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.IsOnHold).HasColumnName("is_on_hold").HasDefaultValue(false);
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").CodeN(20);
        b.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
        b.Property(x => x.InvoiceNo).HasColumnName("invoice_no").CodeN(30);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("varchar(20)");
        b.Property(x => x.RequestId).HasColumnName("request_id").HasColumnType("varchar(40)");

        b.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_receiving_result_created_at");
        b.HasIndex(x => x.PoNo).HasDatabaseName("IX_receiving_result_po_no");
        b.HasIndex(x => new { x.ItemCode, x.LotNo });
        b.HasIndex(x => x.RequestId).IsUnique().HasFilter("[request_id] IS NOT NULL")
         .HasDatabaseName("UX_receiving_result_request_id");

        b.HasOne<Ims.Domain.Masters.Item>().WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany()
         .HasForeignKey(x => x.LocationCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Reason>().WithMany()
         .HasForeignKey(x => x.ReasonCode).OnDelete(DeleteBehavior.Restrict);
    }
}
