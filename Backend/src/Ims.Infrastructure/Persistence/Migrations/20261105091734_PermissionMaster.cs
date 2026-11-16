using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PermissionMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "role_name",
                table: "role",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "is_builtin",
                table: "role",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_visible",
                table: "function_large",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_visible",
                table: "app_function",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(@"
UPDATE function_large SET is_visible = 0
WHERE large_code IN (SELECT target_code FROM function_visibility WHERE target_type = 'Group' AND is_visible = 0);
UPDATE app_function SET is_visible = 0
WHERE function_code IN (
    SELECT target_code FROM function_visibility
    WHERE is_visible = 0 AND (target_type IN ('PC', 'Function') OR (target_type = 'HT' AND target_code LIKE 'ht[_]%')));
");

            migrationBuilder.DropTable(
                name: "function_visibility");

            migrationBuilder.DropIndex(
                name: "IX_permission_function_code",
                table: "permission");

            migrationBuilder.RenameColumn(
                name: "action",
                table: "permission",
                newName: "action_type");

            migrationBuilder.Sql(@"
ALTER TABLE role_permission NOCHECK CONSTRAINT ALL;
UPDATE role SET role_code = UPPER(role_code) WHERE role_code IN ('Admin', 'General', 'Worker');
UPDATE role SET
    role_name = CASE role_code WHEN 'ADMIN' THEN 'Admin' WHEN 'GENERAL' THEN 'General' ELSE 'Worker' END,
    is_builtin = CASE WHEN role_code = 'ADMIN' THEN 1 ELSE 0 END
WHERE role_code IN ('ADMIN', 'GENERAL', 'WORKER');
UPDATE role_permission SET role_code = UPPER(role_code) WHERE role_code IN ('Admin', 'General', 'Worker');
UPDATE app_user SET role_code = UPPER(role_code) WHERE role_code IN ('Admin', 'General', 'Worker');
DELETE FROM role_permission WHERE role_code = 'ADMIN';

DECLARE @map TABLE (old_action varchar(30) COLLATE Latin1_General_BIN NOT NULL, new_action varchar(30) NOT NULL);
INSERT INTO @map VALUES
    ('view', 'Open'), ('new', 'New'), ('edit', 'Edit'), ('copy', 'Copy'), ('delete', 'Delete'),
    ('import', 'Import'), ('export', 'Export'), ('label', 'Label'), ('status', 'StatusChange'),
    ('status_change', 'StatusChange'), ('shortage', 'Shortage'), ('adjust', 'Adjust'), ('disposal', 'Disposal'),
    ('restore', 'RestoreDisposal'), ('hold', 'Hold'), ('release', 'Release'), ('start', 'Start'),
    ('complete', 'Complete'), ('finalize', 'Finalize'), ('reset', 'Reset'),
    ('material_picking', 'MaterialPicking'), ('reverse', 'Reverse');

DELETE rp FROM role_permission rp
JOIN permission p ON p.permission_code = rp.permission_code
WHERE p.function_code IN ('dashboard', 'super_admin')
   OR NOT EXISTS (SELECT 1 FROM @map m WHERE m.old_action = p.action_type COLLATE Latin1_General_BIN
                                         OR m.new_action = p.action_type COLLATE Latin1_General_BIN);
DELETE p FROM permission p
WHERE p.function_code IN ('dashboard', 'super_admin')
   OR NOT EXISTS (SELECT 1 FROM @map m WHERE m.old_action = p.action_type COLLATE Latin1_General_BIN
                                         OR m.new_action = p.action_type COLLATE Latin1_General_BIN);

UPDATE rp SET permission_code = p.function_code + '.' + m.new_action
FROM role_permission rp
JOIN permission p ON p.permission_code = rp.permission_code
JOIN @map m ON m.old_action = p.action_type COLLATE Latin1_General_BIN;
UPDATE p SET action_type = m.new_action, permission_code = p.function_code + '.' + m.new_action
FROM permission p
JOIN @map m ON m.old_action = p.action_type COLLATE Latin1_General_BIN;
ALTER TABLE role_permission WITH CHECK CHECK CONSTRAINT ALL;
");

            migrationBuilder.CreateIndex(
                name: "IX_permission_function_code_action_type",
                table: "permission",
                columns: new[] { "function_code", "action_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_user_role_code",
                table: "app_user",
                column: "role_code");

            migrationBuilder.AddForeignKey(
                name: "FK_app_user_role_role_code",
                table: "app_user",
                column: "role_code",
                principalTable: "role",
                principalColumn: "role_code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_app_user_role_role_code",
                table: "app_user");

            migrationBuilder.DropIndex(
                name: "IX_permission_function_code_action_type",
                table: "permission");

            migrationBuilder.DropIndex(
                name: "IX_app_user_role_code",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "is_builtin",
                table: "role");

            migrationBuilder.DropColumn(
                name: "is_visible",
                table: "function_large");

            migrationBuilder.DropColumn(
                name: "is_visible",
                table: "app_function");

            migrationBuilder.RenameColumn(
                name: "action_type",
                table: "permission",
                newName: "action");

            migrationBuilder.AlterColumn<string>(
                name: "role_name",
                table: "role",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "function_visibility",
                columns: table => new
                {
                    target_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    target_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    is_visible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_function_visibility", x => new { x.target_type, x.target_code });
                });

            migrationBuilder.CreateIndex(
                name: "IX_permission_function_code",
                table: "permission",
                column: "function_code");
        }
    }
}
