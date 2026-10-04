using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddInventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_history",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    action_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_history", x => x.no);
                    table.ForeignKey(
                        name: "FK_inventory_history_factory_factory_code",
                        column: x => x.factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_history_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_history_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_history_reason_reason_code",
                        column: x => x.reason_code,
                        principalTable: "reason",
                        principalColumn: "reason_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock",
                columns: table => new
                {
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    stock_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    reserved_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    available_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    is_on_hold = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock", x => new { x.item_code, x.serial_no });
                    table.ForeignKey(
                        name: "FK_stock_factory_factory_code",
                        column: x => x.factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reason_reason_code",
                        column: x => x.reason_code,
                        principalTable: "reason",
                        principalColumn: "reason_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_history_action_type",
                table: "inventory_history",
                column: "action_type");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_history_created_at",
                table: "inventory_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_history_factory_code",
                table: "inventory_history",
                column: "factory_code");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_history_item_code",
                table: "inventory_history",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_history_location_code",
                table: "inventory_history",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_history_reason_code",
                table: "inventory_history",
                column: "reason_code");

            migrationBuilder.CreateIndex(
                name: "IX_stock_factory_code",
                table: "stock",
                column: "factory_code");

            migrationBuilder.CreateIndex(
                name: "IX_stock_is_on_hold",
                table: "stock",
                column: "is_on_hold");

            migrationBuilder.CreateIndex(
                name: "IX_stock_location_code",
                table: "stock",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_stock_lot_no",
                table: "stock",
                column: "lot_no");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reason_code",
                table: "stock",
                column: "reason_code");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_history");

            migrationBuilder.DropTable(
                name: "stock");
        }
    }
}
