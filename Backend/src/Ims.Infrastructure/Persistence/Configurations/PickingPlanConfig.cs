using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Picking;

namespace Ims.Infrastructure.Persistence.Configurations;

public class PickingPlanConfig : IEntityTypeConfiguration<PickingPlan>
{
    public void Configure(EntityTypeBuilder<PickingPlan> b)
    {
        b.ToTable("picking_plan");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.SoNo).HasColumnName("so_no").Code(30);
        b.Property(x => x.SoBranchNo).HasColumnName("so_branch_no").Code(30);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.LocationCode).HasColumnName("location_code").CodeN(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.PickedQty).HasColumnName("picked_qty").HasColumnType("decimal(18,4)").HasDefaultValue(0m).IsConcurrencyToken();
        b.Property(x => x.ScheduledDate).HasColumnName("scheduled_date");
        b.Property(x => x.Status).HasColumnName("status").HasDefaultValue(PickingStatus.NotPicked);
        b.MapAudit();

        b.HasIndex(x => new { x.SoNo, x.SoBranchNo }).HasDatabaseName("IX_picking_plan_so_no");
        b.HasIndex(x => x.ScheduledDate).HasDatabaseName("IX_picking_plan_scheduled_date");

        b.HasOne<Ims.Domain.Masters.Item>().WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany()
         .HasForeignKey(x => x.LocationCode).OnDelete(DeleteBehavior.Restrict);
    }
}
