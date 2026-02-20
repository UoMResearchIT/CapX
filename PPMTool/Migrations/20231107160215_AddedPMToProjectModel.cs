// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedPMToProjectModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectManagerPersonId",
                table: "Projects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectManagerPersonId",
                table: "Projects",
                column: "ProjectManagerPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_People_ProjectManagerPersonId",
                table: "Projects",
                column: "ProjectManagerPersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_People_ProjectManagerPersonId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectManagerPersonId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectManagerPersonId",
                table: "Projects");
        }
    }
}
