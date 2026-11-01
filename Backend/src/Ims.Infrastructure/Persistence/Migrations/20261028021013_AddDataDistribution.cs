using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataDistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dist_audit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    who = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    action = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    target = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dist_audit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dist_check_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    device_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    received_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    app_version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dist_check_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dist_device_state",
                columns: table => new
                {
                    device_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    version_code = table.Column<int>(type: "int", nullable: true),
                    last_check_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_update_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dist_device_state", x => x.device_code);
                });

            migrationBuilder.CreateTable(
                name: "dist_download",
                columns: table => new
                {
                    device_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    rule_id = table.Column<int>(type: "int", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dist_download", x => new { x.device_code, x.rule_id });
                });

            migrationBuilder.CreateTable(
                name: "dist_package",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    package_name = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    version_code = table.Column<int>(type: "int", nullable: false),
                    version_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    signer_fingerprint = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dist_package", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dist_unknown_device",
                columns: table => new
                {
                    device_code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    first_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    request_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dist_unknown_device", x => x.device_code);
                });

            migrationBuilder.CreateTable(
                name: "dist_rule",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    package_id = table.Column<int>(type: "int", nullable: false),
                    models = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    locations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    include_devices = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    exclude_devices = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    static_targets = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    auto_add = table.Column<bool>(type: "bit", nullable: false),
                    deliver_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    state = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    force_update = table.Column<bool>(type: "bit", nullable: false),
                    min_version_code = table.Column<int>(type: "int", nullable: true),
                    note_ja = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    note_en = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    note_vi = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    published_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    activated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    error = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    stop_reason = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    deploy_steps = table.Column<string>(type: "varchar(400)", unicode: false, maxLength: 400, nullable: true),
                    created_by = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dist_rule", x => x.id);
                    table.ForeignKey(
                        name: "FK_dist_rule_dist_package_package_id",
                        column: x => x.package_id,
                        principalTable: "dist_package",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dist_audit_at",
                table: "dist_audit",
                column: "at");

            migrationBuilder.CreateIndex(
                name: "IX_dist_check_history_device_code_received_at",
                table: "dist_check_history",
                columns: new[] { "device_code", "received_at" });

            migrationBuilder.CreateIndex(
                name: "IX_dist_package_file_name",
                table: "dist_package",
                column: "file_name");

            migrationBuilder.CreateIndex(
                name: "IX_dist_rule_package_id",
                table: "dist_rule",
                column: "package_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dist_audit");

            migrationBuilder.DropTable(
                name: "dist_check_history");

            migrationBuilder.DropTable(
                name: "dist_device_state");

            migrationBuilder.DropTable(
                name: "dist_download");

            migrationBuilder.DropTable(
                name: "dist_rule");

            migrationBuilder.DropTable(
                name: "dist_unknown_device");

            migrationBuilder.DropTable(
                name: "dist_package");
        }
    }
}
