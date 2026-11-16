using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class RequestReplayConfig : IEntityTypeConfiguration<RequestReplay>
{
    public void Configure(EntityTypeBuilder<RequestReplay> b)
    {
        b.ToTable("request_replay");
        b.HasKey(x => x.RequestId);
        b.Property(x => x.RequestId).HasColumnName("request_id").HasColumnType("varchar(50)").IsRequired();
        b.Property(x => x.Kind).HasColumnName("kind").HasColumnType("varchar(30)").IsRequired();
        b.Property(x => x.StatusCode).HasColumnName("status_code");
        b.Property(x => x.Location).HasColumnName("location").HasColumnType("nvarchar(400)");
        b.Property(x => x.ResponseJson).HasColumnName("response_json").HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("varchar(20)");
        b.HasIndex(x => x.CreatedAt);
    }
}
