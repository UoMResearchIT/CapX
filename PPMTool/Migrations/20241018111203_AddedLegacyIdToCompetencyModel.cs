using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedLegacyIdToCompetencyModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LegacyId",
                table: "Competency",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Competency",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegacyId",
                table: "Competency");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Competency");
        }
    }
}
