using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Inventory;

namespace Ims.Infrastructure.Persistence.Configurations;

public class LocationTransferConfig : IEntityTypeConfiguration<LocationTransfer>
{
    public void Configure(EntityTypeBuilder<LocationTransfer> b)
    {
        b.ToTable("location_transfer");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.FromFactoryCode).HasColumnName("from_factory_code").Code(20);
        b.Property(x => x.FromLocationCode).HasColumnName("from_location_code").Code(20);
        b.Property(x => x.ToFactoryCode).HasColumnName("to_factory_code").Code(20);
        b.Property(x => x.ToLocationCode).HasColumnName("to_location_code").Code(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").CodeN(20);

        b.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_location_transfer_created_at");

        b.HasOne<Ims.Domain.Masters.Item>().WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Factory>().WithMany()
         .HasForeignKey(x => x.FromFactoryCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Factory>().WithMany()
         .HasForeignKey(x => x.ToFactoryCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany()
         .HasForeignKey(x => x.FromLocationCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany()
         .HasForeignKey(x => x.ToLocationCode).OnDelete(DeleteBehavior.Restrict);
    }
}
