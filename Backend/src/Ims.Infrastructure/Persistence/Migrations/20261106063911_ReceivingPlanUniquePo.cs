using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ims.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReceivingPlanUniquePo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM receiving_plan GROUP BY po_no, po_branch_no HAVING COUNT(*) > 1)
BEGIN
    DROP INDEX IX_receiving_plan_po ON receiving_plan;
    CREATE UNIQUE INDEX IX_receiving_plan_po ON receiving_plan (po_no, po_branch_no);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_receiving_plan_po",
                table: "receiving_plan");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_plan_po",
                table: "receiving_plan",
                columns: new[] { "po_no", "po_branch_no" });
        }
    }
}
