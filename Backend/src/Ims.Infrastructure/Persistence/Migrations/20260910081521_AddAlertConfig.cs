using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddAlertConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_config",
                columns: table => new
                {
                    alert_type_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    alert_type_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    max_threshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    min_threshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_config", x => x.alert_type_code);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_config");
        }
    }
}
