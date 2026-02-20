// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RemovedFundsReceivedColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundsReceived",
                table: "Projects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "FundsReceived",
                table: "Projects",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql(@"
                UPDATE Projects
                SET FundsReceived = (
                    SELECT COALESCE(SUM(py.Value), 0)
                    FROM Payments py
                    WHERE py.ProjectId = Projects.ProjectId
                )
            ");
        }
    }
}
