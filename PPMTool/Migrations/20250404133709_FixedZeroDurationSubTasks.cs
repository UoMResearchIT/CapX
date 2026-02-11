// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class FixedZeroDurationSubTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM SubTasks WHERE SubTaskId = 143;
            ");

            migrationBuilder.Sql(@"
                UPDATE SubTasks
                SET OriginalDemand = 0.4,
                Demand = 0.4,
                EndDate = datetime(StartDate, '+112 days'),
                DurationDays = 112
                WHERE SubTaskId = 318;
            ");

            migrationBuilder.Sql(@"
                UPDATE SubTasks
                SET OriginalDemand = 0.4,
                Demand = 0.4,
                EndDate = datetime(StartDate, '+10 days'),
                DurationDays = 10
                WHERE SubTaskId = 449;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot restore the deleted task

            migrationBuilder.Sql(@"
                UPDATE SubTasks
                SET OriginalDemand = 0.001,
                Demand = 0.001,
                EndDate = datetime(StartDate, '-1 days'),
                DurationDays = 0
                WHERE SubTaskId = 318;
            ");

            migrationBuilder.Sql(@"
                UPDATE SubTasks
                SET OriginalDemand = 0.001,
                Demand = 0.001,
                EndDate = datetime(StartDate, '-1 days'),
                DurationDays = 0
                WHERE SubTaskId = 449;
            ");
        }
    }
}
