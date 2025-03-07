// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ExtraInfoOnResources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ActualCost",
                table: "Resources",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ActualWorkHours",
                table: "Resources",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PlannedCost",
                table: "Resources",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PlannedWorkHours",
                table: "Resources",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualCost",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ActualWorkHours",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "PlannedCost",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "PlannedWorkHours",
                table: "Resources");
        }
    }
}
