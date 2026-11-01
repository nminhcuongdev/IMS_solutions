using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ProcessConfig : IEntityTypeConfiguration<Process>
{
    public void Configure(EntityTypeBuilder<Process> b)
    {
        b.ToTable("process");
        b.HasKey(x => x.ProcessCode);
        b.Property(x => x.ProcessCode).HasColumnName("process_code").Code(20);
        b.Property(x => x.ProcessName).HasColumnName("process_name").Text(100);
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.MapRemarks();
        b.MapAudit();
        b.MapSoftDelete();
    }
}
