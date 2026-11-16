using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductionResultMerge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "production_order",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "received_by",
                table: "production_order",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "production_result",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    result_type = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    line_no = table.Column<int>(type: "int", nullable: true),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    reverses_no = table.Column<long>(type: "bigint", nullable: true),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    label_issued_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    label_issued_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_result", x => x.no);
                    table.CheckConstraint("CK_production_result_line", "([result_type] = 'Output' AND [line_no] IS NULL) OR ([result_type] <> 'Output' AND [line_no] IS NOT NULL)");
                    table.CheckConstraint("CK_production_result_qty", "[qty] > 0");
                    table.CheckConstraint("CK_production_result_type", "[result_type] IN ('Consume', 'Return', 'Output')");
                    table.ForeignKey(
                        name: "FK_production_result_factory_factory_code",
                        column: x => x.factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_result_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_result_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_result_production_order_line_production_order_no_line_no",
                        columns: x => new { x.production_order_no, x.line_no },
                        principalTable: "production_order_line",
                        principalColumns: new[] { "production_order_no", "line_no" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_result_production_order_production_order_no",
                        column: x => x.production_order_no,
                        principalTable: "production_order",
                        principalColumn: "production_order_no",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_result_production_result_reverses_no",
                        column: x => x.reverses_no,
                        principalTable: "production_result",
                        principalColumn: "no",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_result_reason_reason_code",
                        column: x => x.reason_code,
                        principalTable: "reason",
                        principalColumn: "reason_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_production_result_factory_code",
                table: "production_result",
                column: "factory_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_result_item_code_lot_no",
                table: "production_result",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_production_result_location_code",
                table: "production_result",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_result_production_order_no_line_no",
                table: "production_result",
                columns: new[] { "production_order_no", "line_no" });

            migrationBuilder.CreateIndex(
                name: "IX_production_result_production_order_no_result_type_line_no",
                table: "production_result",
                columns: new[] { "production_order_no", "result_type", "line_no" });

            migrationBuilder.CreateIndex(
                name: "IX_production_result_reason_code",
                table: "production_result",
                column: "reason_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_result_request_id_serial_no",
                table: "production_result",
                columns: new[] { "request_id", "serial_no" },
                unique: true,
                filter: "[reverses_no] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_production_result_reverses_no",
                table: "production_result",
                column: "reverses_no",
                unique: true,
                filter: "[reverses_no] IS NOT NULL");

            migrationBuilder.Sql(@"
DECLARE @cons int = (SELECT COUNT(*) FROM production_consumption);
DECLARE @ret int = (SELECT COUNT(*) FROM production_return);
DECLARE @out int = (SELECT COUNT(*) FROM production_output);
CREATE TABLE #prd_map (old_table varchar(10) NOT NULL, old_no bigint NOT NULL, old_reverses bigint NULL, new_no bigint NOT NULL);

MERGE INTO production_result AS t
USING (SELECT * FROM production_consumption) AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (created_at, request_id, production_order_no, result_type, line_no, item_code, lot_no,
    serial_no, qty, location_code, factory_code, expiry_date, reverses_no, reason_code, created_by)
VALUES (s.created_at, s.request_id, s.production_order_no, 'Consume', s.line_no, s.item_code, s.lot_no,
    s.serial_no, s.qty, s.location_code, s.factory_code, s.expiry_date, NULL, s.reason_code, s.created_by)
OUTPUT 'Consume', s.no, s.reverses_no, inserted.no INTO #prd_map;

MERGE INTO production_result AS t
USING (SELECT * FROM production_return) AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (created_at, request_id, production_order_no, result_type, line_no, item_code, lot_no,
    serial_no, qty, location_code, factory_code, expiry_date, reverses_no, reason_code, created_by)
VALUES (s.created_at, s.request_id, s.production_order_no, 'Return', s.line_no, s.item_code, s.lot_no,
    s.serial_no, s.qty, s.location_code, s.factory_code, s.expiry_date, NULL, s.reason_code, s.created_by)
OUTPUT 'Return', s.no, s.reverses_no, inserted.no INTO #prd_map;

MERGE INTO production_result AS t
USING (
    SELECT o.*, po.factory_code AS order_factory,
           (SELECT MIN(l.created_at) FROM production_label l
            WHERE l.output_request_id = o.request_id AND l.production_order_no = o.production_order_no) AS label_at,
           (SELECT TOP 1 l.created_by FROM production_label l
            WHERE l.output_request_id = o.request_id AND l.production_order_no = o.production_order_no
            ORDER BY l.created_at) AS label_by
    FROM production_output o
    JOIN production_order po ON po.production_order_no = o.production_order_no
) AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (created_at, request_id, production_order_no, result_type, line_no, item_code, lot_no,
    serial_no, qty, location_code, factory_code, expiry_date, reverses_no, reason_code, label_issued_at, label_issued_by, created_by)
VALUES (s.created_at, s.request_id, s.production_order_no, 'Output', NULL, s.item_code, s.lot_no,
    s.serial_no, s.qty, s.location_code, s.order_factory, s.expiry_date, NULL, s.reason_code,
    CASE WHEN s.reverses_no IS NULL THEN s.label_at END, CASE WHEN s.reverses_no IS NULL THEN s.label_by END, s.created_by)
OUTPUT 'Output', s.no, s.reverses_no, inserted.no INTO #prd_map;

UPDATE r SET reverses_no = target.new_no
FROM production_result r
JOIN #prd_map m ON m.new_no = r.no
JOIN #prd_map target ON target.old_table = m.old_table AND target.old_no = m.old_reverses
WHERE m.old_reverses IS NOT NULL;

UPDATE o SET received_at = x.created_at, received_by = x.created_by
FROM production_order o
CROSS APPLY (SELECT TOP 1 r.created_at, r.created_by FROM production_receipt r
             WHERE r.production_order_no = o.production_order_no ORDER BY r.created_at DESC) x;

IF (SELECT COUNT(*) FROM production_result WHERE result_type = 'Consume') <> @cons
   OR (SELECT COUNT(*) FROM production_result WHERE result_type = 'Return') <> @ret
   OR (SELECT COUNT(*) FROM production_result WHERE result_type = 'Output') <> @out
   OR (SELECT COUNT(*) FROM #prd_map WHERE old_reverses IS NOT NULL)
      <> (SELECT COUNT(*) FROM production_result WHERE reverses_no IS NOT NULL)
    THROW 50001, 'production_result migration: row counts or reversal links do not match.', 1;

DROP TABLE #prd_map;
");

            migrationBuilder.DropTable(
                name: "production_label");

            migrationBuilder.DropTable(
                name: "production_output");

            migrationBuilder.DropTable(
                name: "production_receipt");

            migrationBuilder.DropTable(
                name: "production_return");

            migrationBuilder.DropTable(
                name: "production_consumption");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_result");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "production_order");

            migrationBuilder.DropColumn(
                name: "received_by",
                table: "production_order");

            migrationBuilder.CreateTable(
                name: "production_consumption",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    line_no = table.Column<int>(type: "int", nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    reverses_no = table.Column<long>(type: "bigint", nullable: true),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_consumption", x => x.no);
                    table.ForeignKey(
                        name: "FK_production_consumption_factory_factory_code",
                        column: x => x.factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_consumption_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_consumption_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_consumption_production_consumption_reverses_no",
                        column: x => x.reverses_no,
                        principalTable: "production_consumption",
                        principalColumn: "no",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_consumption_production_order_line_production_order_no_line_no",
                        columns: x => new { x.production_order_no, x.line_no },
                        principalTable: "production_order_line",
                        principalColumns: new[] { "production_order_no", "line_no" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_consumption_reason_reason_code",
                        column: x => x.reason_code,
                        principalTable: "reason",
                        principalColumn: "reason_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_label",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    label_count = table.Column<int>(type: "int", nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    output_request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    remainder_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    std_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    total_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_label", x => x.no);
                    table.ForeignKey(
                        name: "FK_production_label_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_label_production_order_production_order_no",
                        column: x => x.production_order_no,
                        principalTable: "production_order",
                        principalColumn: "production_order_no",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_output",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    reverses_no = table.Column<long>(type: "bigint", nullable: true),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_output", x => x.no);
                    table.ForeignKey(
                        name: "FK_production_output_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_output_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_output_production_order_production_order_no",
                        column: x => x.production_order_no,
                        principalTable: "production_order",
                        principalColumn: "production_order_no",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_output_production_output_reverses_no",
                        column: x => x.reverses_no,
                        principalTable: "production_output",
                        principalColumn: "no",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_output_reason_reason_code",
                        column: x => x.reason_code,
                        principalTable: "reason",
                        principalColumn: "reason_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_receipt",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_receipt", x => x.no);
                    table.ForeignKey(
                        name: "FK_production_receipt_production_order_production_order_no",
                        column: x => x.production_order_no,
                        principalTable: "production_order",
                        principalColumn: "production_order_no",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_return",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    consumption_no = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    line_no = table.Column<int>(type: "int", nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    reverses_no = table.Column<long>(type: "bigint", nullable: true),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_return", x => x.no);
                    table.ForeignKey(
                        name: "FK_production_return_factory_factory_code",
                        column: x => x.factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_return_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_return_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_return_production_consumption_consumption_no",
                        column: x => x.consumption_no,
                        principalTable: "production_consumption",
                        principalColumn: "no",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_return_production_order_line_production_order_no_line_no",
                        columns: x => new { x.production_order_no, x.line_no },
                        principalTable: "production_order_line",
                        principalColumns: new[] { "production_order_no", "line_no" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_return_production_return_reverses_no",
                        column: x => x.reverses_no,
                        principalTable: "production_return",
                        principalColumn: "no",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_return_reason_reason_code",
                        column: x => x.reason_code,
                        principalTable: "reason",
                        principalColumn: "reason_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_production_consumption_factory_code",
                table: "production_consumption",
                column: "factory_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_consumption_item_code_lot_no",
                table: "production_consumption",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_production_consumption_location_code",
                table: "production_consumption",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_consumption_production_order_no_line_no",
                table: "production_consumption",
                columns: new[] { "production_order_no", "line_no" });

            migrationBuilder.CreateIndex(
                name: "IX_production_consumption_reason_code",
                table: "production_consumption",
                column: "reason_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_consumption_request_id_serial_no",
                table: "production_consumption",
                columns: new[] { "request_id", "serial_no" },
                unique: true,
                filter: "[reverses_no] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_production_consumption_reverses_no",
                table: "production_consumption",
                column: "reverses_no",
                unique: true,
                filter: "[reverses_no] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_production_label_item_code",
                table: "production_label",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_label_output_request_id",
                table: "production_label",
                column: "output_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_label_production_order_no",
                table: "production_label",
                column: "production_order_no");

            migrationBuilder.CreateIndex(
                name: "IX_production_label_request_id",
                table: "production_label",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_output_item_code_lot_no",
                table: "production_output",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_production_output_location_code",
                table: "production_output",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_output_production_order_no",
                table: "production_output",
                column: "production_order_no");

            migrationBuilder.CreateIndex(
                name: "IX_production_output_reason_code",
                table: "production_output",
                column: "reason_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_output_request_id_serial_no",
                table: "production_output",
                columns: new[] { "request_id", "serial_no" },
                unique: true,
                filter: "[reverses_no] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_production_output_reverses_no",
                table: "production_output",
                column: "reverses_no",
                unique: true,
                filter: "[reverses_no] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_production_receipt_production_order_no",
                table: "production_receipt",
                column: "production_order_no");

            migrationBuilder.CreateIndex(
                name: "IX_production_receipt_request_id",
                table: "production_receipt",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_return_consumption_no",
                table: "production_return",
                column: "consumption_no");

            migrationBuilder.CreateIndex(
                name: "IX_production_return_factory_code",
                table: "production_return",
                column: "factory_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_return_item_code_lot_no",
                table: "production_return",
                columns: new[] { "item_code", "lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_production_return_location_code",
                table: "production_return",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_return_production_order_no_line_no",
                table: "production_return",
                columns: new[] { "production_order_no", "line_no" });

            migrationBuilder.CreateIndex(
                name: "IX_production_return_reason_code",
                table: "production_return",
                column: "reason_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_return_request_id_serial_no",
                table: "production_return",
                columns: new[] { "request_id", "serial_no" },
                unique: true,
                filter: "[reverses_no] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_production_return_reverses_no",
                table: "production_return",
                column: "reverses_no",
                unique: true,
                filter: "[reverses_no] IS NOT NULL");
        }
    }
}
