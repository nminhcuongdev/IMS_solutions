using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Distribution;

namespace Ims.Infrastructure.Persistence.Configurations;

public class DistPackageConfig : IEntityTypeConfiguration<DistPackage>
{
    public void Configure(EntityTypeBuilder<DistPackage> b)
    {
        b.ToTable("dist_package");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PackageName).HasColumnName("package_name").Code(200);
        b.Property(x => x.VersionCode).HasColumnName("version_code");
        b.Property(x => x.VersionName).HasColumnName("version_name").Text(100);
        b.Property(x => x.FileName).HasColumnName("file_name").Code(200);
        b.Property(x => x.OriginalFileName).HasColumnName("original_file_name").Text(260);
        b.Property(x => x.SizeBytes).HasColumnName("size_bytes");
        b.Property(x => x.Sha256).HasColumnName("sha256").Code(64);
        b.Property(x => x.SignerFingerprint).HasColumnName("signer_fingerprint").Code(100);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").CodeN(20);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        b.HasIndex(x => x.FileName);
    }
}
