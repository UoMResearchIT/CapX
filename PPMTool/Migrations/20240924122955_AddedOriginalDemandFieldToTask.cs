// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedOriginalDemandFieldToTask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "OriginalDemand",
                table: "SubTasks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql(
                @"
                    UPDATE SubTasks SET OriginalDemand = Demand;
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalDemand",
                table: "SubTasks");
        }
    }
}
