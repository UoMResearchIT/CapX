using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AdjustProjectLevelCostModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TODO: Map the existing enum values to the new values now the values have simplified.
            // TODO: Set appropriate rate for individuals based on the former project model and the grade of the person
            migrationBuilder.Sql(@"
                
            ");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not really a reversible migration due to data loss
        }
    }
}
