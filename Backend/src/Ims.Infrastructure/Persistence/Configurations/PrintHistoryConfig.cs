using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.History;

namespace Ims.Infrastructure.Persistence.Configurations;

public class PrintHistoryConfig : IEntityTypeConfiguration<PrintHistory>
{
    public void Configure(EntityTypeBuilder<PrintHistory> b)
    {
        b.ToTable("print_history");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").UseIdentityColumn();
        b.Property(x => x.Category).HasColumnName("category").Code(30);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.DocNo).HasColumnName("doc_no").CodeN(30);
        b.Property(x => x.ScheduledDate).HasColumnName("scheduled_date");
        b.Property(x => x.ItemCode).HasColumnName("item_code").CodeN(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasPrecision(18, 4);
        b.Property(x => x.UomCode).HasColumnName("uom_code").CodeN(10);
        b.Property(x => x.PackageCount).HasColumnName("package_count");
        b.Property(x => x.Spec).HasColumnName("spec").TextN(100);
        b.Property(x => x.Status).HasColumnName("status").HasDefaultValue(0);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").CodeN(20);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.ItemCode);
    }
}
