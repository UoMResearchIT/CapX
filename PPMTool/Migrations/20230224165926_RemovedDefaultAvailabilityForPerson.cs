// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RemovedDefaultAvailabilityForPerson : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultAvailabilityFTE",
                table: "People");

            migrationBuilder.DropColumn(
                name: "BaselineActivities",
                table: "People");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "People",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaselineActivities",
                table: "People");

            migrationBuilder.AddColumn<double>(
                name: "DefaultAvailabilityFTE",
                table: "People",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "People");
        }
    }
}
