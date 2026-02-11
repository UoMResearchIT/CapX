// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class SetProjectDayRatesBasedOnExistingResources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set project day rate based on lowest rate used by resources on subtasks (or £250 if not)
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET DayRate = (
                        SELECT COALESCE(MIN(DayRate), 250)
                        FROM (
                            SELECT Resources.DayRate
                            FROM SubTasks
                            JOIN Resources ON SubTasks.SubTaskId = Resources.SubTaskId
                            WHERE SubTasks.ProjectId = Projects.ProjectId AND Resources.DayRate > 0
                        )
                    );
                "
            );

            // Set resources to use the project day rate if they have no day rate or it matches the project day rate
            migrationBuilder.Sql(
                @"
                    UPDATE Resources
                    SET UseProjectDayRate = 1
                    WHERE SubTaskId IN (
                        SELECT SubTasks.SubTaskId
                        FROM SubTasks
                        JOIN Projects ON SubTasks.ProjectId = Projects.ProjectId
                        WHERE Resources.DayRate IS NULL OR Resources.DayRate = Projects.DayRate
                    );
                "
            );

            // Clean up resources that are not assigned to subtasks -- needs a better fix in the code as this shouldn't be happening
            // Possibly when resources are removed once a project is cancelled.
            migrationBuilder.Sql(
                @"
                    DELETE FROM Resources
                    WHERE SubTaskId IS NULL;
                "
            );

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
