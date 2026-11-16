using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Stocktaking;

namespace Ims.Infrastructure.Persistence.Configurations;

public class StockScanConfig : IEntityTypeConfiguration<StockScan>
{
    public void Configure(EntityTypeBuilder<StockScan> b)
    {
        b.ToTable("stock_scan");
        b.HasKey(x => x.Epc).HasName("PK_stock_scan");
        b.Property(x => x.Epc).HasColumnName("epc").Code(24);
        b.Property(x => x.ItemCode).HasColumnName("item_code").CodeN(50);
        b.Property(x => x.ItemName).HasColumnName("item_name").TextN(255);
        b.Property(x => x.ItemCategory).HasColumnName("item_category").TextN(100);
        b.Property(x => x.IsScanned).HasColumnName("is_scanned").HasDefaultValue(false);
        b.Property(x => x.IsTarget).HasColumnName("is_target").HasDefaultValue(true).HasSentinel(true);
        b.Property(x => x.WorkerCode).HasColumnName("worker_code").CodeN(50);
        b.Property(x => x.WorkerName).HasColumnName("worker_name").TextN(255);
        b.HasIndex(x => x.ItemCode).HasDatabaseName("IX_stock_scan_item_code");
    }
}
