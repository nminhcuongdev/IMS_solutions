using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddProductionPhase1b : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "process",
                columns: table => new
                {
                    process_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    process_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_process", x => x.process_code);
                });

            migrationBuilder.CreateTable(
                name: "production_order",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    output_item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    planned_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    uom_code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    output_lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    production_lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    storage_location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    planned_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status_code = table.Column<int>(type: "int", nullable: false),
                    cancel_reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_order", x => x.no);
                    table.UniqueConstraint("AK_production_order_production_order_no", x => x.production_order_no);
                    table.ForeignKey(
                        name: "FK_production_order_factory_factory_code",
                        column: x => x.factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_order_item_output_item_code",
                        column: x => x.output_item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_order_location_storage_location_code",
                        column: x => x.storage_location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_order_reason_cancel_reason_code",
                        column: x => x.cancel_reason_code,
                        principalTable: "reason",
                        principalColumn: "reason_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_order_status_status_code",
                        column: x => x.status_code,
                        principalTable: "status",
                        principalColumn: "status_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_label",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    output_request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    total_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    std_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    label_count = table.Column<int>(type: "int", nullable: false),
                    remainder_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
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
                name: "production_order_line",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    line_no = table.Column<int>(type: "int", nullable: false),
                    process_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    planned_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    uom_code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                    picked_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    picked_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_order_line", x => x.no);
                    table.UniqueConstraint("AK_production_order_line_production_order_no_line_no", x => new { x.production_order_no, x.line_no });
                    table.ForeignKey(
                        name: "FK_production_order_line_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_order_line_process_process_code",
                        column: x => x.process_code,
                        principalTable: "process",
                        principalColumn: "process_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_order_line_production_order_production_order_no",
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
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    reverses_no = table.Column<long>(type: "bigint", nullable: true),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
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
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
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
                name: "production_consumption",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    line_no = table.Column<int>(type: "int", nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    reverses_no = table.Column<long>(type: "bigint", nullable: true),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
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
                name: "production_return",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    request_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    production_order_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    line_no = table.Column<int>(type: "int", nullable: false),
                    consumption_no = table.Column<long>(type: "bigint", nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    serial_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    reverses_no = table.Column<long>(type: "bigint", nullable: true),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
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
                name: "IX_production_order_cancel_reason_code",
                table: "production_order",
                column: "cancel_reason_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_order_factory_code",
                table: "production_order",
                column: "factory_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_order_output_item_code_output_lot_no",
                table: "production_order",
                columns: new[] { "output_item_code", "output_lot_no" });

            migrationBuilder.CreateIndex(
                name: "IX_production_order_production_order_no",
                table: "production_order",
                column: "production_order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_order_status_code_planned_start_date",
                table: "production_order",
                columns: new[] { "status_code", "planned_start_date" });

            migrationBuilder.CreateIndex(
                name: "IX_production_order_storage_location_code",
                table: "production_order",
                column: "storage_location_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_order_line_item_code",
                table: "production_order_line",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_order_line_process_code",
                table: "production_order_line",
                column: "process_code");

            migrationBuilder.CreateIndex(
                name: "IX_production_order_line_production_order_no_line_no",
                table: "production_order_line",
                columns: new[] { "production_order_no", "line_no" },
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropTable(
                name: "production_order_line");

            migrationBuilder.DropTable(
                name: "process");

            migrationBuilder.DropTable(
                name: "production_order");
        }
    }
}
