using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ims.Domain.Returns;

namespace Ims.Infrastructure.Persistence.Configurations;

public class ReturnResultConfig : IEntityTypeConfiguration<ReturnResult>
{
    public void Configure(EntityTypeBuilder<ReturnResult> b)
    {
        b.ToTable("return_result");
        b.HasKey(x => x.No);
        b.Property(x => x.No).HasColumnName("no").ValueGeneratedOnAdd();

        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
        b.Property(x => x.ReturnNo).HasColumnName("return_no").CodeN(30);
        b.Property(x => x.ReturnBranchNo).HasColumnName("return_branch_no").CodeN(30);
        b.Property(x => x.PartnerCode).HasColumnName("partner_code").CodeN(20);
        b.Property(x => x.ItemCode).HasColumnName("item_code").Code(20);
        b.Property(x => x.Qty).HasColumnName("qty").HasColumnType("decimal(18,4)");
        b.Property(x => x.LocationCode).HasColumnName("location_code").CodeN(20);
        b.Property(x => x.LotNo).HasColumnName("lot_no").CodeN(20);
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").CodeN(20);
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("varchar(20)");

        b.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_return_result_created_at");
        b.HasIndex(x => x.ReturnNo).HasDatabaseName("IX_return_result_return_no");

        b.HasOne<Ims.Domain.Masters.Item>().WithMany()
         .HasForeignKey(x => x.ItemCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Partner>().WithMany()
         .HasForeignKey(x => x.PartnerCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Location>().WithMany()
         .HasForeignKey(x => x.LocationCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ims.Domain.Masters.Reason>().WithMany()
         .HasForeignKey(x => x.ReasonCode).OnDelete(DeleteBehavior.Restrict);
    }
}
