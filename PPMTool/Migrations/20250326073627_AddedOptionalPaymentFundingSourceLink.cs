// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedOptionalPaymentFundingSourceLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceFundingSourceId",
                table: "Payments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SourceFundingSourceId",
                table: "Payments",
                column: "SourceFundingSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_FundingSources_SourceFundingSourceId",
                table: "Payments",
                column: "SourceFundingSourceId",
                principalTable: "FundingSources",
                principalColumn: "FundingSourceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_FundingSources_SourceFundingSourceId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SourceFundingSourceId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SourceFundingSourceId",
                table: "Payments");
        }
    }
}
