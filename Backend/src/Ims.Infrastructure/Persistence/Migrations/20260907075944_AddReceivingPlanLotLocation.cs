using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddReceivingPlanLotLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "location_code",
                table: "receiving_plan",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lot_no",
                table: "receiving_plan",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_receiving_plan_location_code",
                table: "receiving_plan",
                column: "location_code");

            migrationBuilder.AddForeignKey(
                name: "FK_receiving_plan_location_location_code",
                table: "receiving_plan",
                column: "location_code",
                principalTable: "location",
                principalColumn: "location_code",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_receiving_plan_location_location_code",
                table: "receiving_plan");

            migrationBuilder.DropIndex(
                name: "IX_receiving_plan_location_code",
                table: "receiving_plan");

            migrationBuilder.DropColumn(
                name: "location_code",
                table: "receiving_plan");

            migrationBuilder.DropColumn(
                name: "lot_no",
                table: "receiving_plan");
        }
    }
}
