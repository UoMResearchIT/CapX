// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ChangeAvailabilityToWorkloadModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvailabilityFTE",
                table: "AvailabilityChanges",
                newName: "ProjectWorkFTE");

            migrationBuilder.RenameColumn(
                name: "AvailabilityChangeId",
                table: "AvailabilityChanges",
                newName: "WorkloadModelChangeId");

            migrationBuilder.AddColumn<double>(
                name: "ArchitectureFTE",
                table: "AvailabilityChanges",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "BusinessAsUsualFTE",
                table: "AvailabilityChanges",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PersonalDevelopmentFTE",
                table: "AvailabilityChanges",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ProjectAndServiceManagementFTE",
                table: "AvailabilityChanges",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "StaffManagementFTE",
                table: "AvailabilityChanges",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchitectureFTE",
                table: "AvailabilityChanges");

            migrationBuilder.DropColumn(
                name: "BusinessAsUsualFTE",
                table: "AvailabilityChanges");

            migrationBuilder.DropColumn(
                name: "PersonalDevelopmentFTE",
                table: "AvailabilityChanges");

            migrationBuilder.DropColumn(
                name: "ProjectAndServiceManagementFTE",
                table: "AvailabilityChanges");

            migrationBuilder.DropColumn(
                name: "StaffManagementFTE",
                table: "AvailabilityChanges");

            migrationBuilder.RenameColumn(
                name: "ProjectWorkFTE",
                table: "AvailabilityChanges",
                newName: "AvailabilityFTE");

            migrationBuilder.RenameColumn(
                name: "WorkloadModelChangeId",
                table: "AvailabilityChanges",
                newName: "AvailabilityChangeId");
        }
    }
}
