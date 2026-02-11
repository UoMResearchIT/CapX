// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class SwitchedIndirectsToBaseClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlannedIndirects",
                table: "Projects",
                newName: "PlannedIndirectCost");

            migrationBuilder.RenameColumn(
                name: "ActualIndirects",
                table: "Projects",
                newName: "ActualIndirectCost");

            migrationBuilder.AddColumn<double>(
                name: "ActualIndirectCost",
                table: "SubTasks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PlannedIndirectCost",
                table: "SubTasks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ActualIndirectCost",
                table: "Resources",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PlannedIndirectCost",
                table: "Resources",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualIndirectCost",
                table: "SubTasks");

            migrationBuilder.DropColumn(
                name: "PlannedIndirectCost",
                table: "SubTasks");

            migrationBuilder.DropColumn(
                name: "ActualIndirectCost",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "PlannedIndirectCost",
                table: "Resources");

            migrationBuilder.RenameColumn(
                name: "PlannedIndirectCost",
                table: "Projects",
                newName: "PlannedIndirects");

            migrationBuilder.RenameColumn(
                name: "ActualIndirectCost",
                table: "Projects",
                newName: "ActualIndirects");
        }
    }
}
