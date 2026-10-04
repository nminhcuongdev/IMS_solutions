using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddStocktaking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stocktaking_history",
                columns: table => new
                {
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    stocktaking_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    stocktaking_date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    planned_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    result_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    missing_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocktaking_history", x => new { x.item_code, x.lot_no });
                    table.ForeignKey(
                        name: "FK_stocktaking_history_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stocktaking_session",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    session_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocktaking_session", x => x.no);
                });

            migrationBuilder.CreateTable(
                name: "stocktaking_label",
                columns: table => new
                {
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    stocktaking_session_no = table.Column<long>(type: "bigint", nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_scanned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocktaking_label", x => x.serial_no);
                    table.ForeignKey(
                        name: "FK_stocktaking_label_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stocktaking_label_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stocktaking_label_stocktaking_session_stocktaking_session_no",
                        column: x => x.stocktaking_session_no,
                        principalTable: "stocktaking_session",
                        principalColumn: "no",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stocktaking_history_date",
                table: "stocktaking_history",
                column: "stocktaking_date");

            migrationBuilder.CreateIndex(
                name: "IX_stocktaking_label_item_code",
                table: "stocktaking_label",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_stocktaking_label_location_code",
                table: "stocktaking_label",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_stocktaking_label_session_item",
                table: "stocktaking_label",
                columns: new[] { "stocktaking_session_no", "item_code" });

            migrationBuilder.CreateIndex(
                name: "IX_stocktaking_label_session_location",
                table: "stocktaking_label",
                columns: new[] { "stocktaking_session_no", "location_code" });

            migrationBuilder.CreateIndex(
                name: "IX_stocktaking_session_status",
                table: "stocktaking_session",
                column: "status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stocktaking_history");

            migrationBuilder.DropTable(
                name: "stocktaking_label");

            migrationBuilder.DropTable(
                name: "stocktaking_session");
        }
    }
}
