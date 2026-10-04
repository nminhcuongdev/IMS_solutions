using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ItemGroupConfig : IEntityTypeConfiguration<ItemGroup>
{
    public void Configure(EntityTypeBuilder<ItemGroup> b)
    {
        b.ToTable("item_group");
        b.HasKey(x => x.ItemGroupCode);
        b.Property(x => x.ItemGroupCode).HasColumnName("item_group_code").Code(20);
        b.Property(x => x.ItemGroupName).HasColumnName("item_group_name").Text(100);
        b.Property(x => x.Category).HasColumnName("category").HasConversion<byte>();
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
    }
}
