// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedStatusFlagsToBaseTask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BudgetStatus",
                table: "SubTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleStatus",
                table: "SubTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BudgetStatus",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleStatus",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetStatus",
                table: "SubTasks");

            migrationBuilder.DropColumn(
                name: "ScheduleStatus",
                table: "SubTasks");

            migrationBuilder.DropColumn(
                name: "BudgetStatus",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ScheduleStatus",
                table: "Projects");
        }
    }
}
