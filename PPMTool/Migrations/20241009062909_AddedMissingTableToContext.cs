// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedMissingTableToContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InnateCodeTask_InnateCodes_InnateCodeId",
                table: "InnateCodeTask");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InnateCodeTask",
                table: "InnateCodeTask");

            migrationBuilder.RenameTable(
                name: "InnateCodeTask",
                newName: "InnateCodeTasks");

            migrationBuilder.RenameIndex(
                name: "IX_InnateCodeTask_InnateCodeId",
                table: "InnateCodeTasks",
                newName: "IX_InnateCodeTasks_InnateCodeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InnateCodeTasks",
                table: "InnateCodeTasks",
                column: "InnateCodeTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks",
                column: "InnateCodeId",
                principalTable: "InnateCodes",
                principalColumn: "InnateCodeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                table: "InnateCodeTasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InnateCodeTasks",
                table: "InnateCodeTasks");

            migrationBuilder.RenameTable(
                name: "InnateCodeTasks",
                newName: "InnateCodeTask");

            migrationBuilder.RenameIndex(
                name: "IX_InnateCodeTasks_InnateCodeId",
                table: "InnateCodeTask",
                newName: "IX_InnateCodeTask_InnateCodeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InnateCodeTask",
                table: "InnateCodeTask",
                column: "InnateCodeTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_InnateCodeTask_InnateCodes_InnateCodeId",
                table: "InnateCodeTask",
                column: "InnateCodeId",
                principalTable: "InnateCodes",
                principalColumn: "InnateCodeId");
        }
    }
}
