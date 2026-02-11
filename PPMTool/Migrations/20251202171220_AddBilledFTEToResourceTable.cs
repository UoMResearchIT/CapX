// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddBilledFTEToResourceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BilledFTE",
                table: "Resources",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            // By default add the BilledFTE as equal to AssignmentFTE for existing records
            migrationBuilder.Sql(@"
                UPDATE Resources
                SET BilledFTE = AssignmentFTE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BilledFTE",
                table: "Resources");
        }
    }
}
