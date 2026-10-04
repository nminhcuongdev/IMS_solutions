using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.History;

namespace Ims.Infrastructure.Persistence.Configurations;

public class RfidGateHistoryConfig : IEntityTypeConfiguration<RfidGateHistory>
{
    public void Configure(EntityTypeBuilder<RfidGateHistory> b)
    {
        b.ToTable("rfid_gate_history");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").UseIdentityColumn();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.GateCode).HasColumnName("gate_code").Code(20);
        b.Property(x => x.ItemCode).HasColumnName("item_code").CodeN(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.SerialNo).HasColumnName("serial_no").CodeN(30);
        b.Property(x => x.DirectionType).HasColumnName("direction_type").CodeN(10);
        b.Property(x => x.Status).HasColumnName("status").HasDefaultValue(0);
        b.HasIndex(x => x.CreatedAt);
    }
}
