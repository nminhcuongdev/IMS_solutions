using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Picking;

namespace Ims.Infrastructure.Persistence.Configurations;

public class PickingResultConfig : IEntityTypeConfiguration<PickingResult>
{
    public void Configure(EntityTypeBuilder<PickingResult> b)
    {
        b.ToTable("picking_result");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.SoNo).HasColumnName("so_no").Code(30);
        b.Property(x => x.SoBranchNo).HasColumnName("so_branch_no").Code(30);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.LocationCode).HasColumnName("location_code").Code(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("varchar(20)");

        b.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_picking_result_created_at");
        b.HasIndex(x => x.SoNo).HasDatabaseName("IX_picking_result_so_no");

        b.HasOne<Ims.Domain.Masters.Item>().WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany()
         .HasForeignKey(x => x.LocationCode).OnDelete(DeleteBehavior.Restrict);
    }
}
