using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddTraceabilityPhase1a : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shipping_result_item_code",
                table: "shipping_result");

            migrationBuilder.DropIndex(
                name: "IX_receiving_result_item_code",
                table: "receiving_result");

            migrationBuilder.DropIndex(
                name: "IX_picking_result_item_code",
                table: "picking_result");

            migrationBuilder.DropIndex(
                name: "IX_inventory_history_item_code",
                table: "inventory_history");

            migrationBuilder.AddColumn<bool>(
                name: "ht_scan_required",
                table: "item",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "picking_result_lot",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    picking_result_no = table.Column<long>(type: "bigint", nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_picking_result_lot", x => x.no);
                    table.ForeignKey(
                        name: "FK_picking_result_lot_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_picking_result_lot_picking_result_picking_result_no",
                        column: x => x.picking_result_no,
                        principalTable: "picking_result",
                        principalColumn: "no",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shipping_result_lot",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    shipping_result_no = table.Column<long>(type: "bigint", nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_result_lot", x => x.no);
                    table.ForeignKey(
                        name: "FK_shipping_result_lot_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipping_result_lot_shipping_result_shipping_result_no",
                        column: x => x.shipping_result_no,
                        principalTable: "shipping_result",
                        principalColumn: "no",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_item_code_lot_no",
                table: "stock",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_shipping_result_item_code_lot_no",
                table: "shipping_result",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_receiving_result_item_code_lot_no",
                table: "receiving_result",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_picking_result_item_code_lot_no",
                table: "picking_result",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_history_item_code_lot_no",
                table: "inventory_history",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_picking_result_lot_item_code_lot_no",
                table: "picking_result_lot",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_picking_result_lot_picking_result_no",
                table: "picking_result_lot",
                column: "picking_result_no");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_result_lot_item_code_lot_no",
                table: "shipping_result_lot",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_shipping_result_lot_shipping_result_no",
                table: "shipping_result_lot",
                column: "shipping_result_no");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "picking_result_lot");

            migrationBuilder.DropTable(
                name: "shipping_result_lot");

            migrationBuilder.DropIndex(
                name: "IX_stock_item_code_lot_no",
                table: "stock");

            migrationBuilder.DropIndex(
                name: "IX_shipping_result_item_code_lot_no",
                table: "shipping_result");

            migrationBuilder.DropIndex(
                name: "IX_receiving_result_item_code_lot_no",
                table: "receiving_result");

            migrationBuilder.DropIndex(
                name: "IX_picking_result_item_code_lot_no",
                table: "picking_result");

            migrationBuilder.DropIndex(
                name: "IX_inventory_history_item_code_lot_no",
                table: "inventory_history");

            migrationBuilder.DropColumn(
                name: "ht_scan_required",
                table: "item");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_result_item_code",
                table: "shipping_result",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_result_item_code",
                table: "receiving_result",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_picking_result_item_code",
                table: "picking_result",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_history_item_code",
                table: "inventory_history",
                column: "item_code");
        }
    }
}
