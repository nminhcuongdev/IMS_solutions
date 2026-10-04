using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ItemConfig : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> b)
    {
        b.ToTable("item");
        b.HasKey(x => x.ItemCode);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.ItemName).HasColumnName("item_name").Text(100);
        b.Property(x => x.ItemShortName).HasColumnName("item_short_name").TextN(50);
        b.Property(x => x.ItemGroupCode).HasColumnName("item_group_code").Code(20);
        b.Property(x => x.UomCode).HasColumnName("uom_code").CodeN(10);
        b.Property(x => x.PackingTypeCode).HasColumnName("packing_type_code").CodeN(20);
        b.Property(x => x.PackageQty).HasColumnName("package_qty").HasColumnType("decimal(18,4)").HasDefaultValue(1m);
        b.Property(x => x.IsLotManaged).HasColumnName("is_lot_managed").HasDefaultValue(false);
        b.Property(x => x.IsSerialManaged).HasColumnName("is_serial_managed").HasDefaultValue(false);
        b.Property(x => x.Barcode).HasColumnName("barcode").CodeN(50);
        b.Property(x => x.ExpirationPeriod).HasColumnName("expiration_period");
        b.Property(x => x.SurplusPoint).HasColumnName("surplus_point").HasColumnType("decimal(18,4)");
        b.Property(x => x.ReorderLevel1).HasColumnName("reorder_level_1").HasColumnType("decimal(18,4)");
        b.Property(x => x.ReorderLevel2).HasColumnName("reorder_level_2").HasColumnType("decimal(18,4)");
        b.Property(x => x.DefaultLocationCode).HasColumnName("default_location_code").CodeN(20);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
        b.HasOne(x => x.ItemGroup).WithMany(g => g.Items)
         .HasForeignKey(x => x.ItemGroupCode)
         .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DefaultLocation).WithMany()
         .HasForeignKey(x => x.DefaultLocationCode)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
