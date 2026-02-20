// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RemovedIsWorkDrivenField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWorkDriven",
                table: "SubTasks");

            migrationBuilder.Sql(
                @"
                    UPDATE SubTasks
                    SET TaskType = 0
                    WHERE TaskType = 2
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWorkDriven",
                table: "SubTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
