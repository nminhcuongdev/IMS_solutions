using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Stocktaking;

namespace Ims.Infrastructure.Persistence.Configurations;

public class StocktakingLabelConfig : IEntityTypeConfiguration<StocktakingLabel>
{
    public void Configure(EntityTypeBuilder<StocktakingLabel> b)
    {
        b.ToTable("stocktaking_label");
        b.HasKey(x => x.SerialNo);
        b.Property(x => x.SerialNo).HasColumnName("serial_no").Code(30);

        b.Property(x => x.StocktakingSessionNo).HasColumnName("stocktaking_session_no");
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.LocationCode).HasColumnName("location_code").Code(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
        b.Property(x => x.IsScanned).HasColumnName("is_scanned").HasDefaultValue(false);
        b.Property(x => x.Status).HasColumnName("status").HasDefaultValue(0);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(x => new { x.StocktakingSessionNo, x.LocationCode })
         .HasDatabaseName("IX_stocktaking_label_session_location");
        b.HasIndex(x => new { x.StocktakingSessionNo, x.ItemCode })
         .HasDatabaseName("IX_stocktaking_label_session_item");

        b.HasOne(x => x.Session).WithMany()
         .HasForeignKey(x => x.StocktakingSessionNo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Item).WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Location).WithMany()
         .HasForeignKey(x => x.LocationCode).OnDelete(DeleteBehavior.Restrict);
    }
}
