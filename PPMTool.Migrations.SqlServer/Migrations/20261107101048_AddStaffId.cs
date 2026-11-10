// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StaffId",
                table: "People",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "People");
        }
    }
}
