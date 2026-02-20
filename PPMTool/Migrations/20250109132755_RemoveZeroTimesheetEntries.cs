// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RemoveZeroTimesheetEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    DELETE FROM TimesheetEntries
                    WHERE MondayHours = 0
                      AND TuesdayHours = 0
                      AND WednesdayHours = 0
                      AND ThursdayHours = 0
                      AND FridayHours = 0
                      AND SaturdayHours = 0
                      AND SundayHours = 0
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
