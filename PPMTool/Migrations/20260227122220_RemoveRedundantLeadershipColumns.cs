// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantLeadershipColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_FundingSources_FundingSourceId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_FundingSourceId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RequiresLeadership",
                table: "SubTasks");

            migrationBuilder.DropColumn(
                name: "FundingSourceId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LeadershipFTE",
                table: "Projects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresLeadership",
                table: "SubTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FundingSourceId",
                table: "Projects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "LeadershipFTE",
                table: "Projects",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_FundingSourceId",
                table: "Projects",
                column: "FundingSourceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_FundingSources_FundingSourceId",
                table: "Projects",
                column: "FundingSourceId",
                principalTable: "FundingSources",
                principalColumn: "FundingSourceId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
