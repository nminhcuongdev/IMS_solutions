using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddStockScan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_scan",
                columns: table => new
                {
                    epc = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    item_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    item_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    item_category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_scanned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    worker_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    worker_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_scan", x => x.epc);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_scan_item_code",
                table: "stock_scan",
                column: "item_code");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_scan");
        }
    }
}
