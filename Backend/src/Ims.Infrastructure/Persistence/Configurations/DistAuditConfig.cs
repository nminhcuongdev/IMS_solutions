using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Distribution;

namespace Ims.Infrastructure.Persistence.Configurations;

public class DistAuditConfig : IEntityTypeConfiguration<DistAudit>
{
    public void Configure(EntityTypeBuilder<DistAudit> b)
    {
        b.ToTable("dist_audit");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.At).HasColumnName("at");
        b.Property(x => x.Who).HasColumnName("who").Code(20);
        b.Property(x => x.Action).HasColumnName("action").Code(30);
        b.Property(x => x.Target).HasColumnName("target").TextN(200);
        b.HasIndex(x => x.At);
    }
}
