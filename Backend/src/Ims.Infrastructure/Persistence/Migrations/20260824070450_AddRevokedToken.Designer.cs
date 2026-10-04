using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ims.Infrastructure.Persistence;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ImsDbContext))]
    [Migration("20260824070450_AddRevokedToken")]
    partial class AddRevokedToken
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Ims.Domain.Masters.AppUser", b =>
                {
                    b.Property<string>("UserCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("user_code");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<string>("DefaultLanguage")
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("varchar(10)")
                        .HasColumnName("default_language");

                    b.Property<string>("FactoryCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("factory_code");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("varchar(256)")
                        .HasColumnName("password");

                    b.Property<string>("Remark1")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_1");

                    b.Property<string>("Remark2")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_2");

                    b.Property<string>("Remark3")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_3");

                    b.Property<string>("Remark4")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_4");

                    b.Property<string>("Remark5")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_5");

                    b.Property<string>("RoleCode")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("role_code");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UserCode");

                    b.ToTable("app_user", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.Masters.Company", b =>
                {
                    b.Property<string>("CompanyCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("company_code");

                    b.Property<string>("Address")
                        .HasMaxLength(500)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(500)")
                        .HasColumnName("address");

                    b.Property<string>("CompanyName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("company_name");

                    b.Property<string>("Country")
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("country");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("Remark1")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_1");

                    b.Property<string>("Remark2")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_2");

                    b.Property<string>("Remark3")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_3");

                    b.Property<string>("Remark4")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_4");

                    b.Property<string>("Remark5")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_5");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("CompanyCode");

                    b.ToTable("company", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.Masters.Factory", b =>
                {
                    b.Property<string>("FactoryCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("factory_code");

                    b.Property<string>("Address")
                        .HasMaxLength(500)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(500)")
                        .HasColumnName("address");

                    b.Property<string>("CompanyCode")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("company_code");

                    b.Property<string>("Country")
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("country");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<string>("FactoryName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("factory_name");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDefault")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_default");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("Remark1")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_1");

                    b.Property<string>("Remark2")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_2");

                    b.Property<string>("Remark3")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_3");

                    b.Property<string>("Remark4")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_4");

                    b.Property<string>("Remark5")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_5");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("FactoryCode");

                    b.HasIndex("CompanyCode");

                    b.ToTable("factory", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.Masters.Item", b =>
                {
                    b.Property<string>("ItemCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("item_code");

                    b.Property<string>("Barcode")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("barcode");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<string>("DefaultLocationCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("default_location_code");

                    b.Property<int?>("ExpirationPeriod")
                        .HasColumnType("int")
                        .HasColumnName("expiration_period");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<bool>("IsLotManaged")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_lot_managed");

                    b.Property<bool>("IsSerialManaged")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_serial_managed");

                    b.Property<string>("ItemGroupCode")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("item_group_code");

                    b.Property<string>("ItemName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("item_name");

                    b.Property<string>("ItemShortName")
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("item_short_name");

                    b.Property<decimal>("PackageQty")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,4)")
                        .HasDefaultValue(1m)
                        .HasColumnName("package_qty");

                    b.Property<string>("PackingTypeCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("packing_type_code");

                    b.Property<string>("Remark1")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_1");

                    b.Property<string>("Remark2")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_2");

                    b.Property<string>("Remark3")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_3");

                    b.Property<string>("Remark4")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_4");

                    b.Property<string>("Remark5")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_5");

                    b.Property<decimal?>("ReorderLevel1")
                        .HasColumnType("decimal(18,4)")
                        .HasColumnName("reorder_level_1");

                    b.Property<decimal?>("ReorderLevel2")
                        .HasColumnType("decimal(18,4)")
                        .HasColumnName("reorder_level_2");

                    b.Property<decimal?>("SurplusPoint")
                        .HasColumnType("decimal(18,4)")
                        .HasColumnName("surplus_point");

                    b.Property<string>("UomCode")
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("varchar(10)")
                        .HasColumnName("uom_code");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("ItemCode");

                    b.HasIndex("DefaultLocationCode");

                    b.HasIndex("ItemGroupCode");

                    b.ToTable("item", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.Masters.ItemGroup", b =>
                {
                    b.Property<string>("ItemGroupCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("item_group_code");

                    b.Property<byte>("Category")
                        .HasColumnType("tinyint")
                        .HasColumnName("category");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("ItemGroupName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("item_group_name");

                    b.Property<string>("Remark1")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_1");

                    b.Property<string>("Remark2")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_2");

                    b.Property<string>("Remark3")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_3");

                    b.Property<string>("Remark4")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_4");

                    b.Property<string>("Remark5")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_5");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("ItemGroupCode");

                    b.ToTable("item_group", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.Masters.Location", b =>
                {
                    b.Property<string>("LocationCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("location_code");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("LargeCode")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("large_code");

                    b.Property<string>("LocationName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("location_name");

                    b.Property<string>("LocationType")
                        .IsRequired()
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("varchar(10)")
                        .HasColumnName("location_type");

                    b.Property<string>("MiddleCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("middle_code");

                    b.Property<string>("ParentLocationCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("parent_location_code");

                    b.Property<string>("Remark1")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_1");

                    b.Property<string>("Remark2")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_2");

                    b.Property<string>("Remark3")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_3");

                    b.Property<string>("Remark4")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_4");

                    b.Property<string>("Remark5")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_5");

                    b.Property<string>("SmallCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("small_code");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.Property<string>("ZoneCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("zone_code");

                    b.HasKey("LocationCode");

                    b.HasIndex("ParentLocationCode");

                    b.HasIndex("ZoneCode");

                    b.ToTable("location", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.Masters.Partner", b =>
                {
                    b.Property<string>("PartnerCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("partner_code");

                    b.Property<string>("Address")
                        .HasMaxLength(500)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(500)")
                        .HasColumnName("address");

                    b.Property<string>("Country")
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("country");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsCustomer")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_customer");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<bool>("IsSupplier")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_supplier");

                    b.Property<string>("PartnerName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("partner_name");

                    b.Property<string>("Remark1")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_1");

                    b.Property<string>("Remark2")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_2");

                    b.Property<string>("Remark3")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_3");

                    b.Property<string>("Remark4")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_4");

                    b.Property<string>("Remark5")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_5");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("PartnerCode");

                    b.ToTable("partner", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.Masters.Reason", b =>
                {
                    b.Property<string>("ReasonCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("reason_code");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("ReasonCategoryValue")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("reason_category");

                    b.Property<string>("ReasonName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("reason_name");

                    b.Property<string>("Remark1")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_1");

                    b.Property<string>("Remark2")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_2");

                    b.Property<string>("Remark3")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_3");

                    b.Property<string>("Remark4")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_4");

                    b.Property<string>("Remark5")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_5");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("ReasonCode");

                    b.ToTable("reason", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.Masters.Zone", b =>
                {
                    b.Property<string>("ZoneCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("zone_code");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<string>("FactoryCode")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("factory_code");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("Remark1")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_1");

                    b.Property<string>("Remark2")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_2");

                    b.Property<string>("Remark3")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_3");

                    b.Property<string>("Remark4")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_4");

                    b.Property<string>("Remark5")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("remark_5");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.Property<string>("ZoneName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("zone_name");

                    b.HasKey("ZoneCode");

                    b.HasIndex("FactoryCode");

                    b.ToTable("zone", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.AppFunction", b =>
                {
                    b.Property<string>("FunctionCode")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("function_code");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<int?>("DisplayOrder")
                        .HasColumnType("int")
                        .HasColumnName("display_order");

                    b.Property<string>("FunctionName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("function_name");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("LargeCode")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("large_code");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("FunctionCode");

                    b.HasIndex("LargeCode");

                    b.ToTable("app_function", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.DefaultValue", b =>
                {
                    b.Property<string>("ValueType")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("value_type");

                    b.Property<string>("ValueCode")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("value_code");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<int>("DisplayOrder")
                        .HasColumnType("int")
                        .HasColumnName("display_order");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<string>("NameLang1")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("name_lang1");

                    b.Property<string>("NameLang2")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("name_lang2");

                    b.Property<string>("NameLang3")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("name_lang3");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("ValueType", "ValueCode");

                    b.ToTable("default_value", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.FunctionLarge", b =>
                {
                    b.Property<string>("LargeCode")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("large_code");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<int?>("DisplayOrder")
                        .HasColumnType("int")
                        .HasColumnName("display_order");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<string>("LargeName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("large_name");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.HasKey("LargeCode");

                    b.ToTable("function_large", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.FunctionVisibility", b =>
                {
                    b.Property<string>("TargetType")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("target_type");

                    b.Property<string>("TargetCode")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("target_code");

                    b.Property<bool>("IsVisible")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_visible");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("TargetType", "TargetCode");

                    b.ToTable("function_visibility", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.GeneralSetting", b =>
                {
                    b.Property<string>("ConfigCode")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("config_code");

                    b.Property<string>("Category")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("category");

                    b.Property<string>("ConfigValue")
                        .HasColumnType("nvarchar(MAX)")
                        .HasColumnName("config_value");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(200)")
                        .HasColumnName("description");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("ConfigCode");

                    b.ToTable("general", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.Language", b =>
                {
                    b.Property<string>("LanguageCode")
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("varchar(10)")
                        .HasColumnName("language_code");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("LanguageName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("language_name");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("LanguageCode");

                    b.ToTable("language", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.LanguageData", b =>
                {
                    b.Property<long>("No")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("no");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("No"));

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<string>("DeviceScope")
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("varchar(10)")
                        .HasColumnName("device_scope");

                    b.Property<string>("LabelKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("label_key");

                    b.Property<string>("LabelText")
                        .IsRequired()
                        .HasMaxLength(500)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(500)")
                        .HasColumnName("label_text");

                    b.Property<string>("LanguageCode")
                        .IsRequired()
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("varchar(10)")
                        .HasColumnName("language_code");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("No");

                    b.HasIndex("LabelKey", "LanguageCode")
                        .IsUnique();

                    b.ToTable("language_data", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.Permission", b =>
                {
                    b.Property<string>("PermissionCode")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("permission_code");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("action");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<string>("FunctionCode")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("function_code");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("PermissionCode");

                    b.HasIndex("FunctionCode");

                    b.ToTable("permission", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.RevokedToken", b =>
                {
                    b.Property<string>("Jti")
                        .HasColumnType("varchar(64)")
                        .HasColumnName("jti");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("expires_at");

                    b.Property<DateTime>("RevokedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("revoked_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("UserCode")
                        .IsRequired()
                        .HasColumnType("varchar(20)")
                        .HasColumnName("user_code");

                    b.HasKey("Jti");

                    b.HasIndex("ExpiresAt");

                    b.ToTable("revoked_token", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.Role", b =>
                {
                    b.Property<string>("RoleCode")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("role_code");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<string>("RoleName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("role_name");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("RoleCode");

                    b.ToTable("role", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.RolePermission", b =>
                {
                    b.Property<long>("No")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("no");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("No"));

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("created_by");

                    b.Property<bool>("IsAllowed")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true)
                        .HasColumnName("is_allowed");

                    b.Property<string>("PermissionCode")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("permission_code");

                    b.Property<string>("RoleCode")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("role_code");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("varchar(20)")
                        .HasColumnName("updated_by");

                    b.HasKey("No");

                    b.HasIndex("PermissionCode");

                    b.HasIndex("RoleCode", "PermissionCode")
                        .IsUnique();

                    b.ToTable("role_permission", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.System.StatusMaster", b =>
                {
                    b.Property<int>("StatusCode")
                        .HasColumnType("int")
                        .HasColumnName("status_code");

                    b.Property<string>("Category")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)")
                        .HasColumnName("category");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("StatusNameLang1")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(30)")
                        .HasColumnName("status_name_lang1");

                    b.Property<string>("StatusNameLang2")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(30)")
                        .HasColumnName("status_name_lang2");

                    b.Property<string>("StatusNameLang3")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(true)
                        .HasColumnType("nvarchar(30)")
                        .HasColumnName("status_name_lang3");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updated_at");

                    b.HasKey("StatusCode");

                    b.ToTable("status", (string)null);
                });

            modelBuilder.Entity("Ims.Domain.Masters.Factory", b =>
                {
                    b.HasOne("Ims.Domain.Masters.Company", "Company")
                        .WithMany("Factories")
                        .HasForeignKey("CompanyCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Company");
                });

            modelBuilder.Entity("Ims.Domain.Masters.Item", b =>
                {
                    b.HasOne("Ims.Domain.Masters.Location", "DefaultLocation")
                        .WithMany()
                        .HasForeignKey("DefaultLocationCode")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Ims.Domain.Masters.ItemGroup", "ItemGroup")
                        .WithMany("Items")
                        .HasForeignKey("ItemGroupCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("DefaultLocation");

                    b.Navigation("ItemGroup");
                });

            modelBuilder.Entity("Ims.Domain.Masters.Location", b =>
                {
                    b.HasOne("Ims.Domain.Masters.Location", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentLocationCode")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Ims.Domain.Masters.Zone", "Zone")
                        .WithMany("Locations")
                        .HasForeignKey("ZoneCode")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Parent");

                    b.Navigation("Zone");
                });

            modelBuilder.Entity("Ims.Domain.Masters.Zone", b =>
                {
                    b.HasOne("Ims.Domain.Masters.Factory", "Factory")
                        .WithMany("Zones")
                        .HasForeignKey("FactoryCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Factory");
                });

            modelBuilder.Entity("Ims.Domain.System.AppFunction", b =>
                {
                    b.HasOne("Ims.Domain.System.FunctionLarge", null)
                        .WithMany()
                        .HasForeignKey("LargeCode")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("Ims.Domain.System.Permission", b =>
                {
                    b.HasOne("Ims.Domain.System.AppFunction", null)
                        .WithMany()
                        .HasForeignKey("FunctionCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Ims.Domain.System.RolePermission", b =>
                {
                    b.HasOne("Ims.Domain.System.Permission", null)
                        .WithMany()
                        .HasForeignKey("PermissionCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Ims.Domain.System.Role", null)
                        .WithMany()
                        .HasForeignKey("RoleCode")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Ims.Domain.Masters.Company", b =>
                {
                    b.Navigation("Factories");
                });

            modelBuilder.Entity("Ims.Domain.Masters.Factory", b =>
                {
                    b.Navigation("Zones");
                });

            modelBuilder.Entity("Ims.Domain.Masters.ItemGroup", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("Ims.Domain.Masters.Location", b =>
                {
                    b.Navigation("Children");
                });

            modelBuilder.Entity("Ims.Domain.Masters.Zone", b =>
                {
                    b.Navigation("Locations");
                });
#pragma warning restore 612, 618
        }
    }
}
