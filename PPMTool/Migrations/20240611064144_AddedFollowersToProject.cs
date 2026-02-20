// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedFollowersToProject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "People",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_ProjectId",
                table: "People",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_People_Projects_ProjectId",
                table: "People",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_Projects_ProjectId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_ProjectId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "People");
        }
    }
}
