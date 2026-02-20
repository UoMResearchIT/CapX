// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedAssessmentsToContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessment_Competencies_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessment");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessment_People_PersonId",
                table: "CompetencyAssessment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CompetencyAssessment",
                table: "CompetencyAssessment");

            migrationBuilder.RenameTable(
                name: "CompetencyAssessment",
                newName: "CompetencyAssessments");

            migrationBuilder.RenameIndex(
                name: "IX_CompetencyAssessment_PersonId",
                table: "CompetencyAssessments",
                newName: "IX_CompetencyAssessments_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_CompetencyAssessment_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessments",
                newName: "IX_CompetencyAssessments_AssociatedCompetencyCompetencyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CompetencyAssessments",
                table: "CompetencyAssessments",
                column: "CompetencyAssessmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessments_Competencies_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessments",
                column: "AssociatedCompetencyCompetencyId",
                principalTable: "Competencies",
                principalColumn: "CompetencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessments_People_PersonId",
                table: "CompetencyAssessments",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessments_Competencies_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessments_People_PersonId",
                table: "CompetencyAssessments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CompetencyAssessments",
                table: "CompetencyAssessments");

            migrationBuilder.RenameTable(
                name: "CompetencyAssessments",
                newName: "CompetencyAssessment");

            migrationBuilder.RenameIndex(
                name: "IX_CompetencyAssessments_PersonId",
                table: "CompetencyAssessment",
                newName: "IX_CompetencyAssessment_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_CompetencyAssessments_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessment",
                newName: "IX_CompetencyAssessment_AssociatedCompetencyCompetencyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CompetencyAssessment",
                table: "CompetencyAssessment",
                column: "CompetencyAssessmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessment_Competencies_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessment",
                column: "AssociatedCompetencyCompetencyId",
                principalTable: "Competencies",
                principalColumn: "CompetencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessment_People_PersonId",
                table: "CompetencyAssessment",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }
    }
}
