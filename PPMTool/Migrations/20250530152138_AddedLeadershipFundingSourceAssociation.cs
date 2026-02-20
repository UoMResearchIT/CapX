// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddedLeadershipFundingSourceAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FundingSourceId",
                table: "Projects",
                type: "INTEGER",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_FundingSources_FundingSourceId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_FundingSourceId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FundingSourceId",
                table: "Projects");
        }
    }
}
