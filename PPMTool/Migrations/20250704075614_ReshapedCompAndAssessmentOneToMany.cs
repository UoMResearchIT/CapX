// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class ReshapedCompAndAssessmentOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessments_Competencies_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessments");

            migrationBuilder.DropIndex(
                name: "IX_CompetencyAssessments_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessments");

            migrationBuilder.DropColumn(
                name: "AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessments");

            migrationBuilder.AddColumn<int>(
                name: "CompetencyId",
                table: "CompetencyAssessments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Copy values in from the backup table to the new column
            migrationBuilder.Sql(@"
                UPDATE CompetencyAssessments
                SET CompetencyId = (
                    SELECT AssociatedCompetencyCompetencyId
                    FROM CompetencyAssessments_Backup
                    WHERE CompetencyAssessments.CompetencyAssessmentId = CompetencyAssessments_Backup.AssessmentId
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM CompetencyAssessments_Backup
                    WHERE CompetencyAssessments.CompetencyAssessmentId = CompetencyAssessments_Backup.AssessmentId
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyAssessments_CompetencyId",
                table: "CompetencyAssessments",
                column: "CompetencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessments_Competencies_CompetencyId",
                table: "CompetencyAssessments",
                column: "CompetencyId",
                principalTable: "Competencies",
                principalColumn: "CompetencyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessments_Competencies_CompetencyId",
                table: "CompetencyAssessments");

            migrationBuilder.DropIndex(
                name: "IX_CompetencyAssessments_CompetencyId",
                table: "CompetencyAssessments");

            migrationBuilder.DropColumn(
                name: "CompetencyId",
                table: "CompetencyAssessments");

            migrationBuilder.AddColumn<int>(
                name: "AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessments",
                type: "INTEGER",
                nullable: true);

            // Copy values in from the backup table to the new column
            migrationBuilder.Sql(@"
                UPDATE CompetencyAssessments
                SET AssociatedCompetencyCompetencyId = (
                    SELECT AssociatedCompetencyCompetencyId
                    FROM CompetencyAssessments_Backup
                    WHERE CompetencyAssessments.CompetencyAssessmentId = CompetencyAssessments_Backup.AssessmentId
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM CompetencyAssessments_Backup
                    WHERE CompetencyAssessments.CompetencyAssessmentId = CompetencyAssessments_Backup.AssessmentId
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyAssessments_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessments",
                column: "AssociatedCompetencyCompetencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessments_Competencies_AssociatedCompetencyCompetencyId",
                table: "CompetencyAssessments",
                column: "AssociatedCompetencyCompetencyId",
                principalTable: "Competencies",
                principalColumn: "CompetencyId");
        }
    }
}
