using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class RemoveCompanyMaster : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_factory_company_company_code",
                table: "factory");

            migrationBuilder.DropTable(
                name: "company");

            migrationBuilder.DropIndex(
                name: "IX_factory_company_code",
                table: "factory");

            migrationBuilder.DropColumn(
                name: "company_code",
                table: "factory");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company_code",
                table: "factory",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "company",
                columns: table => new
                {
                    company_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    company_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    created_by = table.Column<string>(type: "varchar(20)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    remark_1 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_2 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_3 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_4 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    remark_5 = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company", x => x.company_code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_factory_company_code",
                table: "factory",
                column: "company_code");

            migrationBuilder.AddForeignKey(
                name: "FK_factory_company_company_code",
                table: "factory",
                column: "company_code",
                principalTable: "company",
                principalColumn: "company_code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
