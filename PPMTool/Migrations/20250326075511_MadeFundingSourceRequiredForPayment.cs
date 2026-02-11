// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class MadeFundingSourceRequiredForPayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_FundingSources_SourceFundingSourceId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "SourceFundingSourceId",
                table: "Payments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_FundingSources_SourceFundingSourceId",
                table: "Payments",
                column: "SourceFundingSourceId",
                principalTable: "FundingSources",
                principalColumn: "FundingSourceId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_FundingSources_SourceFundingSourceId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "SourceFundingSourceId",
                table: "Payments",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_FundingSources_SourceFundingSourceId",
                table: "Payments",
                column: "SourceFundingSourceId",
                principalTable: "FundingSources",
                principalColumn: "FundingSourceId");
        }
    }
}
