using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddPicking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "picking_plan",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    so_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    so_branch_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    picked_qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_picking_plan", x => x.no);
                    table.ForeignKey(
                        name: "FK_picking_plan_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_picking_plan_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "picking_result",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    so_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    so_branch_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_picking_result", x => x.no);
                    table.ForeignKey(
                        name: "FK_picking_result_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_picking_result_location_location_code",
                        column: x => x.location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_picking_plan_item_code",
                table: "picking_plan",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_picking_plan_location_code",
                table: "picking_plan",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_picking_plan_scheduled_date",
                table: "picking_plan",
                column: "scheduled_date");

            migrationBuilder.CreateIndex(
                name: "IX_picking_plan_so_no",
                table: "picking_plan",
                columns: new[] { "so_no", "so_branch_no" });

            migrationBuilder.CreateIndex(
                name: "IX_picking_result_created_at",
                table: "picking_result",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_picking_result_item_code",
                table: "picking_result",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_picking_result_location_code",
                table: "picking_result",
                column: "location_code");

            migrationBuilder.CreateIndex(
                name: "IX_picking_result_so_no",
                table: "picking_result",
                column: "so_no");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "picking_plan");

            migrationBuilder.DropTable(
                name: "picking_result");
        }
    }
}
