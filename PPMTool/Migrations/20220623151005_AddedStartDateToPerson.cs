// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PPMTool.Migrations
{
    public partial class AddedStartDateToPerson : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "People",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "People");
        }
    }
}
