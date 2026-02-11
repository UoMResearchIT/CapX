// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ChangeEndDateDrivenToFixedEndDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsEndDateDriven",
                table: "SubTasks",
                newName: "HasFixedEndDate");

            migrationBuilder.Sql(
                @"
                    UPDATE SubTasks
                    SET HasFixedEndDate = NOT HasFixedEndDate;
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HasFixedEndDate",
                table: "SubTasks",
                newName: "IsEndDateDriven");

            migrationBuilder.Sql(
                @"
                    UPDATE SubTasks
                    SET IsEndDateDriven = NOT IsEndDateDriven;
                "
            );
        }
    }
}
