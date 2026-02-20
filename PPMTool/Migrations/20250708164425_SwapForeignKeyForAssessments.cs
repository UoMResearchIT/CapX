// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class SwapForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessments_People_PersonId",
                table: "CompetencyAssessments");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "CompetencyAssessments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessments_People_PersonId",
                table: "CompetencyAssessments",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyAssessments_People_PersonId",
                table: "CompetencyAssessments");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "CompetencyAssessments",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyAssessments_People_PersonId",
                table: "CompetencyAssessments",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }
    }
}
