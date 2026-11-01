using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Picking;

namespace Ims.Infrastructure.Persistence.Configurations;

public class PickingResultLotConfig : IEntityTypeConfiguration<PickingResultLot>
{
    public void Configure(EntityTypeBuilder<PickingResultLot> b)
    {
        b.ToTable("picking_result_lot");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.PickingResultNo).HasColumnName("picking_result_no");
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.SerialNo).HasColumnName("serial_no").Code(30);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").Code(20);
        b.HasIndex(x => new { x.ItemCode, x.LotNo });
        b.HasOne(x => x.PickingResult).WithMany().HasForeignKey(x => x.PickingResultNo).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Ims.Domain.Masters.Item>().WithMany().HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
    }
}
