// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RenameTableAvailabilityChangesToWorkloadModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilityChanges_People_PersonId",
                table: "AvailabilityChanges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvailabilityChanges",
                table: "AvailabilityChanges");

            migrationBuilder.RenameTable(
                name: "AvailabilityChanges",
                newName: "WorkloadModelChanges");

            migrationBuilder.RenameIndex(
                name: "IX_AvailabilityChanges_PersonId",
                table: "WorkloadModelChanges",
                newName: "IX_WorkloadModelChanges_PersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkloadModelChanges",
                table: "WorkloadModelChanges",
                column: "WorkloadModelChangeId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkloadModelChanges_People_PersonId",
                table: "WorkloadModelChanges",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkloadModelChanges_People_PersonId",
                table: "WorkloadModelChanges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkloadModelChanges",
                table: "WorkloadModelChanges");

            migrationBuilder.RenameTable(
                name: "WorkloadModelChanges",
                newName: "AvailabilityChanges");

            migrationBuilder.RenameIndex(
                name: "IX_WorkloadModelChanges_PersonId",
                table: "AvailabilityChanges",
                newName: "IX_AvailabilityChanges_PersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvailabilityChanges",
                table: "AvailabilityChanges",
                column: "WorkloadModelChangeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilityChanges_People_PersonId",
                table: "AvailabilityChanges",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }
    }
}
