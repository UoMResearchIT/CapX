// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class MigrateAllDBTimeFieldsToOneFTERelative : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    UPDATE AvailabilityChanges
                    SET AvailabilityFTE = AvailabilityFTE / 0.84;
                    UPDATE People
                    SET FTE = FTE / 0.84;
                    UPDATE Resources
                    SET Percentage = Percentage / 84;
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    UPDATE AvailabilityChanges
                    SET AvailabilityFTE = AvailabilityFTE * 0.84;
                    UPDATE People
                    SET FTE = FTE * 0.84;
                    UPDATE Resources
                    SET Percentage = Percentage * 84;
                "
            );
        }
    }
}
