using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddLocationTransfer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "location_transfer",
                columns: table => new
                {
                    no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    item_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    lot_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    from_factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    from_location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    to_factory_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    to_location_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_location_transfer", x => x.no);
                    table.ForeignKey(
                        name: "FK_location_transfer_factory_from_factory_code",
                        column: x => x.from_factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_location_transfer_factory_to_factory_code",
                        column: x => x.to_factory_code,
                        principalTable: "factory",
                        principalColumn: "factory_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_location_transfer_item_item_code",
                        column: x => x.item_code,
                        principalTable: "item",
                        principalColumn: "item_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_location_transfer_location_from_location_code",
                        column: x => x.from_location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_location_transfer_location_to_location_code",
                        column: x => x.to_location_code,
                        principalTable: "location",
                        principalColumn: "location_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_location_transfer_created_at",
                table: "location_transfer",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_location_transfer_from_factory_code",
                table: "location_transfer",
                column: "from_factory_code");

            migrationBuilder.CreateIndex(
                name: "IX_location_transfer_from_location_code",
                table: "location_transfer",
                column: "from_location_code");

            migrationBuilder.CreateIndex(
                name: "IX_location_transfer_item_code",
                table: "location_transfer",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "IX_location_transfer_to_factory_code",
                table: "location_transfer",
                column: "to_factory_code");

            migrationBuilder.CreateIndex(
                name: "IX_location_transfer_to_location_code",
                table: "location_transfer",
                column: "to_location_code");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "location_transfer");
        }
    }
}
