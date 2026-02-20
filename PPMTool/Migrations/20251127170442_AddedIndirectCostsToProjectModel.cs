// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddedIndirectCostsToProjectModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ActualIndirects",
                table: "Projects",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "BudgetedIndirects",
                table: "Projects",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PlannedIndirects",
                table: "Projects",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualIndirects",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "BudgetedIndirects",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PlannedIndirects",
                table: "Projects");
        }
    }
}
