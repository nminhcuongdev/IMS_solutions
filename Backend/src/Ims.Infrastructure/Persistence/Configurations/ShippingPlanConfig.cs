using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Shipping;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ShippingPlanConfig : IEntityTypeConfiguration<ShippingPlan>
{
    public void Configure(EntityTypeBuilder<ShippingPlan> b)
    {
        b.ToTable("shipping_plan");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.SoNo).HasColumnName("so_no").Code(30);
        b.Property(x => x.SoBranchNo).HasColumnName("so_branch_no").Code(30);
        b.Property(x => x.PartnerCode).HasColumnName("partner_code").CodeN(20);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.ActualQty).HasColumnName("actual_qty").HasColumnType("decimal(18,4)").HasDefaultValue(0m).IsConcurrencyToken();
        b.Property(x => x.FactoryCode).HasColumnName("factory_code").CodeN(20);
        b.Property(x => x.GateCode).HasColumnName("gate_code").CodeN(20);
        b.Property(x => x.ScheduledDate).HasColumnName("scheduled_date");
        b.Property(x => x.Status).HasColumnName("status").HasDefaultValue(ShippingStatus.Pending);
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").CodeN(20);
        b.Property(x => x.InvoiceNo).HasColumnName("invoice_no").CodeN(30);
        b.MapAudit();

        b.HasIndex(x => new { x.SoNo, x.SoBranchNo }).HasDatabaseName("IX_shipping_plan_so_no");
        b.HasIndex(x => x.ScheduledDate).HasDatabaseName("IX_shipping_plan_scheduled_date");

        b.HasOne<Ims.Domain.Masters.Item>().WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Partner>().WithMany()
         .HasForeignKey(x => x.PartnerCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Factory>().WithMany()
         .HasForeignKey(x => x.FactoryCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Reason>().WithMany()
         .HasForeignKey(x => x.ReasonCode).OnDelete(DeleteBehavior.Restrict);
    }
}
