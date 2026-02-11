// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class BackUpCompetencyAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create a backup of the CompetencyAssessments table data as we are about to change the column in the next migration.
            migrationBuilder.Sql(@"
                -- Step 1: Create the backup table
                CREATE TABLE CompetencyAssessments_Backup (
                    AssessmentId INTEGER NOT NULL,
                    CompetencyId INTEGER
                );

                -- Step 2: Populate it from the existing CompetencyAssessments table
                INSERT INTO CompetencyAssessments_Backup (AssessmentId, CompetencyId)
                SELECT CompetencyAssessmentId, AssociatedCompetencyCompetencyId
                FROM CompetencyAssessments;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS CompetencyAssessments_Backup;");
        }
    }
}
