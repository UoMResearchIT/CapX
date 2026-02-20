// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RoundAssignmentsAndDemand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Resources
                SET AssignmentFTE = ROUND(AssignmentFTE, 3)
                WHERE CAST(AssignmentFTE AS REAL) != ROUND(AssignmentFTE, 3);

                UPDATE SubTasks
                SET Demand = ROUND(Demand, 3)
                WHERE CAST(Demand AS REAL) != ROUND(Demand, 3);

                UPDATE SubTasks
                SET OriginalDemand = ROUND(OriginalDemand, 3)
                WHERE CAST(OriginalDemand AS REAL) != ROUND(OriginalDemand, 3);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not possible to reverse this migration
        }
    }
}
