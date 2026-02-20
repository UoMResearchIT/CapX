// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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
            // Set appropriate rate for individuals based on the former project model and the grade of the person
            migrationBuilder.Sql(@"
                -- Step 1: Create a temporary table to store intermediate values
                CREATE TEMP TABLE TempResourceUpdates (
                    ResourceId INTEGER PRIMARY KEY,
                    SubTaskId INTEGER,
                    CostModel INTEGER,
                    Grade INTEGER,
                    Rate INTEGER
                );

                -- Step 2: Insert intermediate data into the temporary table
                INSERT INTO TempResourceUpdates (ResourceId, SubTaskId, CostModel, Grade)
                SELECT
                    r.ResourceId,
                    r.SubTaskId,
                    p.CostModel,
                    COALESCE(
                        (SELECT wm.Grade
                         FROM WorkloadModelChanges wm
                         WHERE wm.PersonId = r.PersonId
                           AND wm.ChangeDate <= st.StartDate
                         ORDER BY wm.ChangeDate DESC
                         LIMIT 1),
                        6 -- Default Grade value
                    ) AS Grade
                FROM Resources r
                JOIN SubTasks st ON r.SubTaskId = st.SubTaskId
                JOIN Projects p ON st.OwningProjectProjectId = p.ProjectId;

                -- Step 3: Update the Rate column in the Resources table based on conditions
                UPDATE Resources
                SET Rate = CASE
                    WHEN t.CostModel <> 0 AND t.Grade < 6 THEN 1
                    WHEN t.CostModel <> 0 AND t.Grade > 7 THEN 2
                    ELSE 0
                END
                FROM TempResourceUpdates t
                WHERE Resources.ResourceId = t.ResourceId;

                -- Step 4: Drop the temporary table to clean up
                DROP TABLE TempResourceUpdates;
            ");

            // Update mapping now enums have changed: 1 or 2 => 1; 3 or 4 => 2
            migrationBuilder.Sql(@"
                UPDATE Projects
                SET CostModel = 1 WHERE CostModel = 2;
                
                UPDATE Projects
                SET CostModel = 2 WHERE CostModel = 3 OR CostModel = 4;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not really a reversible migration due to data loss
        }
    }
}
