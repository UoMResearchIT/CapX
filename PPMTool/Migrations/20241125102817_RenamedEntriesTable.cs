// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RenamedEntriesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetActivities_InnateCodeTasks_InnateCodeTaskId",
                table: "TimesheetActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetActivities_Timesheets_TimesheetId",
                table: "TimesheetActivities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TimesheetActivities",
                table: "TimesheetActivities");

            migrationBuilder.RenameTable(
                name: "TimesheetActivities",
                newName: "TimesheetEntries");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetActivities_TimesheetId",
                table: "TimesheetEntries",
                newName: "IX_TimesheetEntries_TimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetActivities_InnateCodeTaskId",
                table: "TimesheetEntries",
                newName: "IX_TimesheetEntries_InnateCodeTaskId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TimesheetEntries",
                table: "TimesheetEntries",
                column: "TimesheetEntryId");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetEntries_InnateCodeTasks_InnateCodeTaskId",
                table: "TimesheetEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetEntries_Timesheets_TimesheetId",
                table: "TimesheetEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TimesheetEntries",
                table: "TimesheetEntries");

            migrationBuilder.RenameTable(
                name: "TimesheetEntries",
                newName: "TimesheetActivities");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetEntries_TimesheetId",
                table: "TimesheetActivities",
                newName: "IX_TimesheetActivities_TimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetEntries_InnateCodeTaskId",
                table: "TimesheetActivities",
                newName: "IX_TimesheetActivities_InnateCodeTaskId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TimesheetActivities",
                table: "TimesheetActivities",
                column: "TimesheetEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetActivities_InnateCodeTasks_InnateCodeTaskId",
                table: "TimesheetActivities",
                column: "InnateCodeTaskId",
                principalTable: "InnateCodeTasks",
                principalColumn: "InnateCodeTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetActivities_Timesheets_TimesheetId",
                table: "TimesheetActivities",
                column: "TimesheetId",
                principalTable: "Timesheets",
                principalColumn: "TimesheetId");
        }
    }
}
