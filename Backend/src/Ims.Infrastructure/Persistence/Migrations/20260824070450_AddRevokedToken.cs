using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class AddRevokedToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "revoked_token",
                columns: table => new
                {
                    jti = table.Column<string>(type: "varchar(64)", nullable: false),
                    user_code = table.Column<string>(type: "varchar(20)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revoked_token", x => x.jti);
                });

            migrationBuilder.CreateIndex(
                name: "IX_revoked_token_expires_at",
                table: "revoked_token",
                column: "expires_at");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "revoked_token");
        }
    }
}
