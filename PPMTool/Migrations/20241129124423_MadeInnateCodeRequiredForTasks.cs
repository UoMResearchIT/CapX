// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class MadeInnateCodeRequiredForTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks");

            migrationBuilder.AlterColumn<int>(
                name: "InnateCodeId",
                table: "InnateCodeTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks",
                column: "InnateCodeId",
                principalTable: "InnateCodes",
                principalColumn: "InnateCodeId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks");

            migrationBuilder.AlterColumn<int>(
                name: "InnateCodeId",
                table: "InnateCodeTasks",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks",
                column: "InnateCodeId",
                principalTable: "InnateCodes",
                principalColumn: "InnateCodeId");
        }
    }
}
