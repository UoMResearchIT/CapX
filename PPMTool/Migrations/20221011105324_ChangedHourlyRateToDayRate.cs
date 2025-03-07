// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ChangedHourlyRateToDayRate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HourlyRate",
                table: "People",
                newName: "DayRate");

            // Set new day rate
            migrationBuilder.Sql("UPDATE People SET DayRate = 312");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DayRate",
                table: "People",
                newName: "HourlyRate");

            // Revert to old hourly rate based on £250 day rate
            migrationBuilder.Sql("UPDATE People SET HourlyRate = 35.71");
        }
    }
}
