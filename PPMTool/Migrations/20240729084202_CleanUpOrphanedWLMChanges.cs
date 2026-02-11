// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class CleanUpOrphanedWLMChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BaselineActivities",
                table: "WorkloadModelChanges",
                newName: "Notes");

            migrationBuilder.Sql(
                @"
                    DELETE FROM WorkloadModelChanges
                    WHERE PersonId IS NULL;
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "WorkloadModelChanges",
                newName: "BaselineActivities");
        }
    }
}
