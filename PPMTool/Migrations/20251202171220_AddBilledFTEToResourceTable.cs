using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddBilledFTEToResourceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BilledFTE",
                table: "Resources",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            // Technically should give it a value based on AssignmentFTE and the project cost model but should be added at the same time as the cost model so 0 is fine for now.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BilledFTE",
                table: "Resources");
        }
    }
}
