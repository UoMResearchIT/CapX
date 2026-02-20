// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class DropBackupTableForAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the backup table as it is no longer needed
            migrationBuilder.Sql("DROP TABLE IF EXISTS CompetencyAssessments_Backup;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create the backup table again in case we need it for rollback with the same column structure as expected in the previous migration
            migrationBuilder.Sql(@"
                -- Step 1: Create the backup table
                CREATE TABLE CompetencyAssessments_Backup (
                    AssessmentId INTEGER NOT NULL,
                    CompetencyId INTEGER
                );

                -- Step 2: Populate it from the existing CompetencyAssessments table
                INSERT INTO CompetencyAssessments_Backup (AssessmentId, CompetencyId)
                SELECT CompetencyAssessmentId, CompetencyId
                FROM CompetencyAssessments;
            ");
        }
    }
}
