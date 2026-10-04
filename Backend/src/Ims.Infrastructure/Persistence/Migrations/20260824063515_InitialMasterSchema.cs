using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class InitialMasterSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    user_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    user_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "varchar(256)", nullable: false),
                    role_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    default_language = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user", x => x.user_code);
                });

            migrationBuilder.CreateTable(
                name: "company",
                columns: table => new
                {
                    company_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    company_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company", x => x.company_code);
                });

            migrationBuilder.CreateTable(
                name: "default_value",
                columns: table => new
                {
                    value_type = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    value_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    name_lang1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name_lang2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name_lang3 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_default_value", x => new { x.value_type, x.value_code });
                });

            migrationBuilder.CreateTable(
                name: "function_large",
                columns: table => new
                {
                    large_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    large_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_function_large", x => x.large_code);
                });

            migrationBuilder.CreateTable(
                name: "function_visibility",
                columns: table => new
                {
                    target_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    target_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    is_visible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_function_visibility", x => new { x.target_type, x.target_code });
                });

            migrationBuilder.CreateTable(
                name: "general",
                columns: table => new
                {
                    config_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    config_value = table.Column<string>(type: "nvarchar(MAX)", nullable: true),
                    category = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    description = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_general", x => x.config_code);
                });

            migrationBuilder.CreateTable(
                name: "item_group",
                columns: table => new
                {
                    item_group_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    item_group_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    category = table.Column<byte>(type: "tinyint", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_group", x => x.item_group_code);
                });

            migrationBuilder.CreateTable(
                name: "language",
                columns: table => new
                {
                    language_code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    language_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_language", x => x.language_code);
                });

            migrationBuilder.CreateTable(
                name: "language_data",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    label_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    language_code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    label_text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    device_scope = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_language_data", x => x.no);
                });

            migrationBuilder.CreateTable(
                name: "partner",
                columns: table => new
                {
                    partner_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    partner_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_supplier = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_customer = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner", x => x.partner_code);
                });

            migrationBuilder.CreateTable(
                name: "reason",
                columns: table => new
                {
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    reason_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    reason_category = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reason", x => x.reason_code);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    role_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    role_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.role_code);
                });

            migrationBuilder.CreateTable(
                name: "status",
                columns: table => new
                {
                    status_code = table.Column<int>(type: "int", nullable: false),
                    status_name_lang1 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    status_name_lang2 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    status_name_lang3 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    category = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status", x => x.status_code);
                });

            migrationBuilder.CreateTable(
                name: "factory",
                columns: table => new
                {
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    factory_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    company_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory", x => x.factory_code);
                    table.ForeignKey(
                        name: "FK_factory_company_company_code",
                        column: x => x.company_code,
                        principalTable: "company",
                        principalColumn: "company_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "app_function",
                columns: table => new
                {
                    function_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    function_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    large_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_function", x => x.function_code);
                    table.ForeignKey(
                        name: "FK_app_function_function_large_large_code",
                        column: x => x.large_code,
                        principalTable: "function_large",
                        principalColumn: "large_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "zone",
                columns: table => new
                {
                    zone_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    zone_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zone", x => x.zone_code);
                    table.ForeignKey(
                        name: "FK_zone_factory_factory_code",
                        column: x => x.factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    permission_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    function_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    action = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission", x => x.permission_code);
                    table.ForeignKey(
                        name: "FK_permission_app_function_function_code",
                        column: x => x.function_code,
                        principalTable: "app_function",
                        principalColumn: "function_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "location",
                columns: table => new
                {
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    location_type = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    large_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    middle_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    small_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    location_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    zone_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    parent_location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_location", x => x.location_code);
                    table.ForeignKey(
                        name: "FK_location_location_parent_location_code",
                        column: x => x.parent_location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_location_zone_zone_code",
                        column: x => x.zone_code,
                        principalTable: "zone",
                        principalColumn: "zone_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    permission_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    is_allowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permission", x => x.no);
                    table.ForeignKey(
                        name: "FK_role_permission_permission_permission_code",
                        column: x => x.permission_code,
                        principalTable: "permission",
                        principalColumn: "permission_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_permission_role_role_code",
                        column: x => x.role_code,
                        principalTable: "role",
                        principalColumn: "role_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item",
                columns: table => new
                {
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    item_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    item_short_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    item_group_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    uom_code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    packing_type_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    package_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 1m),
                    is_lot_managed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_serial_managed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    barcode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    expiration_period = table.Column<int>(type: "int", nullable: true),
                    surplus_point = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    reorder_level_1 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    reorder_level_2 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    default_location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item", x => x.item_code);
                    table.ForeignKey(
                        name: "FK_item_item_group_item_group_code",
                        column: x => x.item_group_code,
                        principalTable: "item_group",
                        principalColumn: "item_group_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_location_default_location_code",
                        column: x => x.default_location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_function_large_code",
                table: "app_function",
                column: "large_code");

            migrationBuilder.CreateIndex(
                name: "IX_factory_company_code",
                table: "factory",
                column: "company_code");

            migrationBuilder.CreateIndex(
                name: "IX_item_default_location_code",
                table: "item",
                column: "default_location_code");

            migrationBuilder.CreateIndex(
                name: "IX_item_item_group_code",
                table: "item",
                column: "item_group_code");

            migrationBuilder.CreateIndex(
                name: "IX_language_data_label_key_language_code",
                table: "language_data",
                columns: new[] { "label_key", "language_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_location_parent_location_code",
                table: "location",
                column: "parent_location_code");

            migrationBuilder.CreateIndex(
                name: "IX_location_zone_code",
                table: "location",
                column: "zone_code");

            migrationBuilder.CreateIndex(
                name: "IX_permission_function_code",
                table: "permission",
                column: "function_code");

            migrationBuilder.CreateIndex(
                name: "IX_role_permission_permission_code",
                table: "role_permission",
                column: "permission_code");

            migrationBuilder.CreateIndex(
                name: "IX_role_permission_role_code_permission_code",
                table: "role_permission",
                columns: new[] { "role_code", "permission_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_zone_factory_code",
                table: "zone",
                column: "factory_code");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_user");

            migrationBuilder.DropTable(
                name: "default_value");

            migrationBuilder.DropTable(
                name: "function_visibility");

            migrationBuilder.DropTable(
                name: "general");

            migrationBuilder.DropTable(
                name: "item");

            migrationBuilder.DropTable(
                name: "language");

            migrationBuilder.DropTable(
                name: "language_data");

            migrationBuilder.DropTable(
                name: "partner");

            migrationBuilder.DropTable(
                name: "reason");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "status");

            migrationBuilder.DropTable(
                name: "item_group");

            migrationBuilder.DropTable(
                name: "location");

            migrationBuilder.DropTable(
                name: "permission");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "zone");

            migrationBuilder.DropTable(
                name: "app_function");

            migrationBuilder.DropTable(
                name: "factory");

            migrationBuilder.DropTable(
                name: "function_large");

            migrationBuilder.DropTable(
                name: "company");
        }
    }
}
