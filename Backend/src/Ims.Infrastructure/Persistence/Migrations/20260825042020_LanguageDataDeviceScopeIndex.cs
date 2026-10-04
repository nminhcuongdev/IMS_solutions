using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    public partial class LanguageDataDeviceScopeIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_language_data_label_key_language_code",
                table: "language_data");

            migrationBuilder.CreateIndex(
                name: "IX_language_data_label_key_language_code_device_scope",
                table: "language_data",
                columns: new[] { "label_key", "language_code", "device_scope" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_language_data_label_key_language_code_device_scope",
                table: "language_data");

            migrationBuilder.CreateIndex(
                name: "IX_language_data_label_key_language_code",
                table: "language_data",
                columns: new[] { "label_key", "language_code" },
                unique: true);
        }
    }
}
