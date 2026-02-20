// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class MakeLineManagerNotRequiredToAllowSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_People_LineManagerPersonId",
                table: "People");

            migrationBuilder.AlterColumn<int>(
                name: "LineManagerPersonId",
                table: "People",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_People_People_LineManagerPersonId",
                table: "People",
                column: "LineManagerPersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_People_LineManagerPersonId",
                table: "People");

            migrationBuilder.AlterColumn<int>(
                name: "LineManagerPersonId",
                table: "People",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_People_People_LineManagerPersonId",
                table: "People",
                column: "LineManagerPersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
