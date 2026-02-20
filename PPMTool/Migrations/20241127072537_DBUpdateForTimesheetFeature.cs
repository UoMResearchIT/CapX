// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class DBUpdateForTimesheetFeature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetEntries_InnateCodeTasks_InnateCodeTaskId",
                table: "TimesheetEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetEntries_Timesheets_TimesheetId",
                table: "TimesheetEntries");

            migrationBuilder.AlterColumn<int>(
                name: "TimesheetId",
                table: "TimesheetEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InnateCodeTaskId",
                table: "TimesheetEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetEntries_InnateCodeTasks_InnateCodeTaskId",
                table: "TimesheetEntries",
                column: "InnateCodeTaskId",
                principalTable: "InnateCodeTasks",
                principalColumn: "InnateCodeTaskId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetEntries_Timesheets_TimesheetId",
                table: "TimesheetEntries",
                column: "TimesheetId",
                principalTable: "Timesheets",
                principalColumn: "TimesheetId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetEntries_InnateCodeTasks_InnateCodeTaskId",
                table: "TimesheetEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetEntries_Timesheets_TimesheetId",
                table: "TimesheetEntries");

            migrationBuilder.AlterColumn<int>(
                name: "TimesheetId",
                table: "TimesheetEntries",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "InnateCodeTaskId",
                table: "TimesheetEntries",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetEntries_InnateCodeTasks_InnateCodeTaskId",
                table: "TimesheetEntries",
                column: "InnateCodeTaskId",
                principalTable: "InnateCodeTasks",
                principalColumn: "InnateCodeTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetEntries_Timesheets_TimesheetId",
                table: "TimesheetEntries",
                column: "TimesheetId",
                principalTable: "Timesheets",
                principalColumn: "TimesheetId");
        }
    }
}
