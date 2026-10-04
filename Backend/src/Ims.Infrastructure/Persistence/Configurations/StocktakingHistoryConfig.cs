using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Stocktaking;

namespace Ims.Infrastructure.Persistence.Configurations;

public class StocktakingHistoryConfig : IEntityTypeConfiguration<StocktakingHistory>
{
    public void Configure(EntityTypeBuilder<StocktakingHistory> b)
    {
        b.ToTable("stocktaking_history");
        b.HasKey(x => new { x.ItemCode, x.LotNo });

        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").Code(20);
        b.Property(x => x.StocktakingName).HasColumnName("stocktaking_name").Text(100);
        b.Property(x => x.StocktakingDate).HasColumnName("stocktaking_date").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.PlannedQty).HasColumnName("planned_qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.ResultQty).HasColumnName("result_qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.MissingQty).HasColumnName("missing_qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(x => x.StocktakingDate).HasDatabaseName("IX_stocktaking_history_date");

        b.HasOne(x => x.Item).WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
    }
}
