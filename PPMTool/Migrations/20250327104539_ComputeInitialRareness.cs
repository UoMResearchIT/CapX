// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ComputeInitialRareness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Create a temporary table to store the counts
                CREATE TEMP TABLE SkillTagCounts AS
                SELECT SkillTagId, COUNT(*) AS Count
                FROM OwnedSkills
                GROUP BY SkillTagId;

                -- Get the total number of currently employed people
                WITH EmployedPeople AS (
                    SELECT COUNT(*) AS Total
                    FROM People
                    WHERE StartDate <= DATE('now') AND (EndDate IS NULL OR EndDate >= DATE('now'))
                )
                -- Update the SkillTags table with Rareness and RarenessCount
                UPDATE SkillTags
                SET RarenessCount = (
                    SELECT Count FROM SkillTagCounts WHERE SkillTagCounts.SkillTagId = SkillTags.SkillTagId
                ),
                Rareness = CASE
                    WHEN (SELECT Count FROM SkillTagCounts WHERE SkillTagCounts.SkillTagId = SkillTags.SkillTagId) * 1.0 / (SELECT Total FROM EmployedPeople) * 100 < 5 THEN 4  -- Legendary
                    WHEN (SELECT Count FROM SkillTagCounts WHERE SkillTagCounts.SkillTagId = SkillTags.SkillTagId) * 1.0 / (SELECT Total FROM EmployedPeople) * 100 < 10 THEN 3  -- Epic
                    WHEN (SELECT Count FROM SkillTagCounts WHERE SkillTagCounts.SkillTagId = SkillTags.SkillTagId) * 1.0 / (SELECT Total FROM EmployedPeople) * 100 < 18 THEN 2  -- Rare
                    WHEN (SELECT Count FROM SkillTagCounts WHERE SkillTagCounts.SkillTagId = SkillTags.SkillTagId) * 1.0 / (SELECT Total FROM EmployedPeople) * 100 < 30 THEN 1  -- Uncommon
                    ELSE 0  -- Common
                END;

                -- Drop the temporary table
                DROP TABLE SkillTagCounts;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE SkillTags
                SET RarenessCount = 0, Rareness = 0;
            ");
        }
    }
}
