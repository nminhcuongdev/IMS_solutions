using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddReceivingPlanAndResult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "receiving_plan",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    po_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    po_branch_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    partner_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    actual_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    invoice_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receiving_plan", x => x.no);
                    table.ForeignKey(
                        name: "FK_receiving_plan_factory_factory_code",
                        column: x => x.factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receiving_plan_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receiving_plan_partner_partner_code",
                        column: x => x.partner_code,
                        principalTable: "partner",
                        principalColumn: "partner_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receiving_plan_reason_reason_code",
                        column: x => x.reason_code,
                        principalTable: "reason",
                        principalColumn: "reason_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receiving_result",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    po_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    po_branch_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    is_on_hold = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    reason_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    invoice_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receiving_result", x => x.no);
                    table.ForeignKey(
                        name: "FK_receiving_result_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receiving_result_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receiving_result_reason_reason_code",
                        column: x => x.reason_code,
                        principalTable: "reason",
                        principalColumn: "reason_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_receiving_plan_factory_code",
                table: "receiving_plan",
                column: "factory_code");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_plan_item_code",
                table: "receiving_plan",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_plan_partner_code",
                table: "receiving_plan",
                column: "partner_code");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_plan_po",
                table: "receiving_plan",
                columns: new[] { "po_no", "po_branch_no" });

            migrationBuilder.CreateIndex(
                name: "IX_receiving_plan_reason_code",
                table: "receiving_plan",
                column: "reason_code");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_plan_scheduled_date",
                table: "receiving_plan",
                column: "scheduled_date");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_result_created_at",
                table: "receiving_result",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_result_item_code",
                table: "receiving_result",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_result_location_code",
                table: "receiving_result",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_result_po_no",
                table: "receiving_result",
                column: "po_no");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_result_reason_code",
                table: "receiving_result",
                column: "reason_code");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receiving_plan");

            migrationBuilder.DropTable(
                name: "receiving_result");
        }
    }
}
