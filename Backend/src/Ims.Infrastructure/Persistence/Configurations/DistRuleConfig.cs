using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Distribution;

namespace Ims.Infrastructure.Persistence.Configurations;

public class DistRuleConfig : IEntityTypeConfiguration<DistRule>
{
    public void Configure(EntityTypeBuilder<DistRule> b)
    {
        b.ToTable("dist_rule");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PackageId).HasColumnName("package_id");
        b.Property(x => x.Models).HasColumnName("models").HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.Locations).HasColumnName("locations").HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.IncludeDevices).HasColumnName("include_devices").HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.ExcludeDevices).HasColumnName("exclude_devices").HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.StaticTargets).HasColumnName("static_targets").HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.AutoAdd).HasColumnName("auto_add");
        b.Property(x => x.DeliverAt).HasColumnName("deliver_at");
        b.Property(x => x.EndAt).HasColumnName("end_at");
        b.Property(x => x.State).HasColumnName("state").HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Force).HasColumnName("force_update");
        b.Property(x => x.MinVersionCode).HasColumnName("min_version_code");
        b.Property(x => x.NoteJa).HasColumnName("note_ja").TextN(600);
        b.Property(x => x.NoteEn).HasColumnName("note_en").TextN(600);
        b.Property(x => x.NoteVi).HasColumnName("note_vi").TextN(600);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");
        b.Property(x => x.ActivatedAt).HasColumnName("activated_at");
        b.Property(x => x.Error).HasColumnName("error").CodeN(50);
        b.Property(x => x.StopReason).HasColumnName("stop_reason").CodeN(20);
        b.Property(x => x.DeploySteps).HasColumnName("deploy_steps").CodeN(400);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").CodeN(20);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        b.HasOne(x => x.Package).WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Restrict);
    }
}
