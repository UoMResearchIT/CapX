using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ChangeTableNameForCompetencies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessment_Competency_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Competency",
                table: "Competency");

            migrationBuilder.RenameTable(
                name: "Competency",
                newName: "Competencies");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Competencies",
                table: "Competencies",
                column: "CompetencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessment_Competencies_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessment",
                column: "AssociatedCompetencyCompetencyId",
                principalTable: "Competencies",
                principalColumn: "CompetencyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessment_Competencies_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Competencies",
                table: "Competencies");

            migrationBuilder.RenameTable(
                name: "Competencies",
                newName: "Competency");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Competency",
                table: "Competency",
                column: "CompetencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessment_Competency_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessment",
                column: "AssociatedCompetencyCompetencyId",
                principalTable: "Competency",
                principalColumn: "CompetencyId");
        }
    }
}
