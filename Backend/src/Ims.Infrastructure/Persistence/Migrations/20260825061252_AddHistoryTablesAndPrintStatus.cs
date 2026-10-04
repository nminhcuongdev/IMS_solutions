using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddHistoryTablesAndPrintStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "print_history",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    category = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    doc_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    uom_code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    package_count = table.Column<int>(type: "int", nullable: false),
                    spec = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_history", x => x.no);
                });

            migrationBuilder.CreateTable(
                name: "rfid_gate_history",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    gate_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    direction_type = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfid_gate_history", x => x.no);
                });

            migrationBuilder.CreateIndex(
                name: "IX_print_history_created_at",
                table: "print_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_print_history_item_code",
                table: "print_history",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_rfid_gate_history_created_at",
                table: "rfid_gate_history",
                column: "created_at");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "print_history");

            migrationBuilder.DropTable(
                name: "rfid_gate_history");
        }
    }
}
