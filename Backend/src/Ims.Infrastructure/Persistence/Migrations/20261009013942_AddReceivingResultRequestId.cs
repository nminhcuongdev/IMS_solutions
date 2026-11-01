using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddReceivingResultRequestId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "request_id",
                table: "receiving_result",
                type: "varchar(40)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_receiving_result_request_id",
                table: "receiving_result",
                column: "request_id",
                unique: true,
                filter: "[request_id] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_receiving_result_request_id",
                table: "receiving_result");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "receiving_result");
        }
    }
}
