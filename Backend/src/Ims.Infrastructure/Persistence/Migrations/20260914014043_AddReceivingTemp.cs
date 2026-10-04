using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddReceivingTemp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "receiving_temp",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    po_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    po_branch_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    partner_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    msg = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receiving_temp", x => x.no);
                });

            migrationBuilder.CreateIndex(
                name: "IX_receiving_temp_created_by",
                table: "receiving_temp",
                column: "created_by");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receiving_temp");
        }
    }
}
