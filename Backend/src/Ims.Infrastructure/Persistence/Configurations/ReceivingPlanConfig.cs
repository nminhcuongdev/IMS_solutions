using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Receiving;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ReceivingPlanConfig : IEntityTypeConfiguration<ReceivingPlan>
{
    public void Configure(EntityTypeBuilder<ReceivingPlan> b)
    {
        b.ToTable("receiving_plan");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.PoNo).HasColumnName("po_no").Code(30);
        b.Property(x => x.PoBranchNo).HasColumnName("po_branch_no").Code(30);
        b.Property(x => x.PartnerCode).HasColumnName("partner_code").CodeN(20);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.ActualQty).HasColumnName("actual_qty").HasColumnType("decimal(18,4)").HasDefaultValue(0m).IsConcurrencyToken();
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").CodeN(20);
        b.Property(x => x.ScheduledDate).HasColumnName("scheduled_date");
        b.Property(x => x.Status).HasColumnName("status").HasDefaultValue(ReceivingStatus.NotArrived);
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").CodeN(20);
        b.Property(x => x.InvoiceNo).HasColumnName("invoice_no").CodeN(30);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.LocationCode).HasColumnName("location_code").CodeN(20);
        b.MapAudit();

        b.HasIndex(x => new { x.PoNo, x.PoBranchNo }).IsUnique().HasDatabaseName("IX_receiving_plan_po");
        b.HasIndex(x => x.ScheduledDate).HasDatabaseName("IX_receiving_plan_scheduled_date");

        b.HasOne<Ims.Domain.Masters.Item>().WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Partner>().WithMany()
         .HasForeignKey(x => x.PartnerCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Factory>().WithMany()
         .HasForeignKey(x => x.FactoryCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Reason>().WithMany()
         .HasForeignKey(x => x.ReasonCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany()
         .HasForeignKey(x => x.LocationCode).OnDelete(DeleteBehavior.Restrict);
    }
}
