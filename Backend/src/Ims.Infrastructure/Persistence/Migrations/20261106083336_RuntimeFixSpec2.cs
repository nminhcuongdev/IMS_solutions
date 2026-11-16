using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RuntimeFixSpec2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_stocktaking_label",
                table: "stocktaking_label");

            migrationBuilder.DropPrimaryKey(
                name: "PK_stocktaking_history",
                table: "stocktaking_history");

            migrationBuilder.AddColumn<bool>(
                name: "moved_after_start",
                table: "stocktaking_label",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "planned_qty",
                table: "stocktaking_label",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "stocktaking_session_no",
                table: "stocktaking_history",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "reference_no",
                table: "inventory_history",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE l SET planned_qty = ISNULL(s.stock_qty, 0)
                FROM stocktaking_label l
                LEFT JOIN stock s ON s.item_code = l.item_code AND s.serial_no = l.serial_no;

                UPDATE h SET stocktaking_session_no = ISNULL((
                    SELECT TOP 1 s.no FROM stocktaking_session s
                    WHERE s.session_name = h.stocktaking_name AND s.status = 21
                    ORDER BY s.no DESC), 0)
                FROM stocktaking_history h;

                UPDATE a SET action_type = 'RestoreDisposal', reference_no = CAST(d.no AS varchar(30))
                FROM inventory_history a
                CROSS APPLY (
                    SELECT TOP 1 x.no FROM inventory_history x
                    WHERE x.action_type = 'Disposal' AND x.no < a.no AND x.item_code = a.item_code
                      AND ISNULL(x.serial_no, '') = ISNULL(a.serial_no, '')
                      AND ISNULL(x.lot_no, '') = ISNULL(a.lot_no, '')
                      AND x.qty = -a.qty
                      AND ISNULL(x.reason_code, '') = ISNULL(a.reason_code, '')
                    ORDER BY x.no DESC) d
                WHERE a.action_type = 'Adjust' AND a.qty > 0 AND a.reference_no IS NULL;

                UPDATE a SET action_type = 'ProdReverse', reference_no = CAST(r.no AS varchar(30))
                FROM inventory_history a
                CROSS APPLY (
                    SELECT TOP 1 p.no FROM production_result p
                    WHERE p.reverses_no IS NOT NULL AND p.item_code = a.item_code
                      AND ISNULL(p.serial_no, '') = ISNULL(a.serial_no, '')
                      AND p.qty = ABS(a.qty)
                      AND ABS(DATEDIFF(second, p.created_at, a.created_at)) <= 5
                    ORDER BY p.no) r
                WHERE a.action_type = 'Adjust' AND a.reference_no IS NULL;

                UPDATE a SET action_type = 'Receiving'
                FROM inventory_history a
                WHERE a.action_type = 'Adjust' AND a.qty < 0 AND a.reason_code IS NULL
                  AND a.serial_no LIKE 'RCV-%' AND a.reference_no IS NULL
                  AND NOT EXISTS (SELECT 1 FROM receiving_result rr
                                  WHERE 'RCV-' + RIGHT('0000000000' + CAST(rr.no AS varchar(10)), 10) = a.serial_no);

                UPDATE t SET lot_no = NULL FROM stock t JOIN item i ON i.item_code = t.item_code
                WHERE i.is_lot_managed = 0 AND t.lot_no IN ('-', 'NA', 'N/A');
                UPDATE t SET lot_no = NULL FROM receiving_result t JOIN item i ON i.item_code = t.item_code
                WHERE i.is_lot_managed = 0 AND t.lot_no IN ('-', 'NA', 'N/A');
                UPDATE t SET lot_no = NULL FROM shipping_result t JOIN item i ON i.item_code = t.item_code
                WHERE i.is_lot_managed = 0 AND t.lot_no IN ('-', 'NA', 'N/A');
                UPDATE t SET lot_no = NULL FROM picking_result t JOIN item i ON i.item_code = t.item_code
                WHERE i.is_lot_managed = 0 AND t.lot_no IN ('-', 'NA', 'N/A');
                UPDATE t SET lot_no = NULL FROM return_result t JOIN item i ON i.item_code = t.item_code
                WHERE i.is_lot_managed = 0 AND t.lot_no IN ('-', 'NA', 'N/A');
                UPDATE t SET lot_no = NULL FROM inventory_history t JOIN item i ON i.item_code = t.item_code
                WHERE i.is_lot_managed = 0 AND t.lot_no IN ('-', 'NA', 'N/A');

                IF NOT EXISTS (SELECT 1 FROM reason WHERE reason_code = 'STK')
                    INSERT INTO reason (reason_code, reason_name, reason_category, is_active, is_deleted, created_at, created_by)
                    VALUES ('STK', N'Stocktaking difference', 'Adjustment', 1, 0, GETDATE(), 'system');
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_stocktaking_label",
                table: "stocktaking_label",
                columns: new[] { "stocktaking_session_no", "serial_no" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_stocktaking_history",
                table: "stocktaking_history",
                columns: new[] { "stocktaking_session_no", "item_code", "lot_no" });

            migrationBuilder.CreateTable(
                name: "request_replay",
                columns: table => new
                {
                    request_id = table.Column<string>(type: "varchar(50)", nullable: false),
                    kind = table.Column<string>(type: "varchar(30)", nullable: false),
                    status_code = table.Column<int>(type: "int", nullable: false),
                    location = table.Column<string>(type: "nvarchar(400)", nullable: true),
                    response_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_replay", x => x.request_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stocktaking_history_item_code",
                table: "stocktaking_history",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_request_replay_created_at",
                table: "request_replay",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_replay");

            migrationBuilder.DropPrimaryKey(
                name: "PK_stocktaking_label",
                table: "stocktaking_label");

            migrationBuilder.DropPrimaryKey(
                name: "PK_stocktaking_history",
                table: "stocktaking_history");

            migrationBuilder.DropIndex(
                name: "IX_stocktaking_history_item_code",
                table: "stocktaking_history");

            migrationBuilder.DropColumn(
                name: "moved_after_start",
                table: "stocktaking_label");

            migrationBuilder.DropColumn(
                name: "planned_qty",
                table: "stocktaking_label");

            migrationBuilder.DropColumn(
                name: "stocktaking_session_no",
                table: "stocktaking_history");

            migrationBuilder.DropColumn(
                name: "reference_no",
                table: "inventory_history");

            migrationBuilder.AddPrimaryKey(
                name: "PK_stocktaking_label",
                table: "stocktaking_label",
                column: "serial_no");

            migrationBuilder.AddPrimaryKey(
                name: "PK_stocktaking_history",
                table: "stocktaking_history",
                columns: new[] { "item_code", "lot_no" });
        }
    }
}
