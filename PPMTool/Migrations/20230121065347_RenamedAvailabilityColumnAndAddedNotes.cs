// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RenamedAvailabilityColumnAndAddedNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvailabilityFTE",
                table: "People",
                newName: "DefaultAvailabilityFTE");

            migrationBuilder.AddColumn<string>(
                name: "BaselineActivities",
                table: "People",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaselineActivities",
                table: "AvailabilityChanges",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaselineActivities",
                table: "People");

            migrationBuilder.DropColumn(
                name: "BaselineActivities",
                table: "AvailabilityChanges");

            migrationBuilder.RenameColumn(
                name: "DefaultAvailabilityFTE",
                table: "People",
                newName: "AvailabilityFTE");
        }
    }
}
