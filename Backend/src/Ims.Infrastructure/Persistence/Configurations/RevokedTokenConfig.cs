using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence.Configurations;

public class RevokedTokenConfig : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> b)
    {
        b.ToTable("revoked_token");
        b.HasKey(x => x.Jti);
        b.Property(x => x.Jti).HasColumnName("jti").HasColumnType("varchar(64)").IsRequired();
        b.Property(x => x.UserCode).HasColumnName("user_code").HasColumnType("varchar(20)").IsRequired();
        b.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.HasIndex(x => x.ExpiresAt);
    }
}
