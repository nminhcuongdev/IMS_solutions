using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddDeviceMaster : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "m_device",
                columns: table => new
                {
                    device_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    device_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    device_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    manufacturer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    spec = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    serial_no = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_m_device", x => x.device_code);
                    table.ForeignKey(
                        name: "FK_m_device_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_m_device_location_code",
                table: "m_device",
                column: "location_code");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "m_device");
        }
    }
}
