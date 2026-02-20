// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class HackedDataModelToFitViews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "TimesheetEntries");

            migrationBuilder.RenameColumn(
                name: "Hours",
                table: "TimesheetEntries",
                newName: "WednesdayHours");

            migrationBuilder.AddColumn<double>(
                name: "FridayHours",
                table: "TimesheetEntries",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MondayHours",
                table: "TimesheetEntries",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SaturdayHours",
                table: "TimesheetEntries",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SundayHours",
                table: "TimesheetEntries",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ThursdayHours",
                table: "TimesheetEntries",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TuesdayHours",
                table: "TimesheetEntries",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FridayHours",
                table: "TimesheetEntries");

            migrationBuilder.DropColumn(
                name: "MondayHours",
                table: "TimesheetEntries");

            migrationBuilder.DropColumn(
                name: "SaturdayHours",
                table: "TimesheetEntries");

            migrationBuilder.DropColumn(
                name: "SundayHours",
                table: "TimesheetEntries");

            migrationBuilder.DropColumn(
                name: "ThursdayHours",
                table: "TimesheetEntries");

            migrationBuilder.DropColumn(
                name: "TuesdayHours",
                table: "TimesheetEntries");

            migrationBuilder.RenameColumn(
                name: "WednesdayHours",
                table: "TimesheetEntries",
                newName: "Hours");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "TimesheetEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
