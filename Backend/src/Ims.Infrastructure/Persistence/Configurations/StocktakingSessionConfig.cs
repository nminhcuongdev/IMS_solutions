using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Stocktaking;

namespace Ims.Infrastructure.Persistence.Configurations;

public class StocktakingSessionConfig : IEntityTypeConfiguration<StocktakingSession>
{
    public void Configure(EntityTypeBuilder<StocktakingSession> b)
    {
        b.ToTable("stocktaking_session");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.SessionName).HasColumnName("session_name").Text(100);
        b.Property(x => x.Status).HasColumnName("status").HasDefaultValue(0);
        b.Property(x => x.StartedAt).HasColumnName("started_at");
        b.Property(x => x.CompletedAt).HasColumnName("completed_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").CodeN(20);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").CodeN(20);

        b.Ignore(x => x.AcceptsResults);

        b.HasIndex(x => x.Status).HasDatabaseName("IX_stocktaking_session_status");

    }
}
