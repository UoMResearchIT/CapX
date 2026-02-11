// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedCorrectInversePropertyWithRequiredForResourceSubTask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_SubTasks_SubTaskId",
                table: "Resources");

            migrationBuilder.AlterColumn<int>(
                name: "SubTaskId",
                table: "Resources",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_SubTasks_SubTaskId",
                table: "Resources",
                column: "SubTaskId",
                principalTable: "SubTasks",
                principalColumn: "SubTaskId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_SubTasks_SubTaskId",
                table: "Resources");

            migrationBuilder.AlterColumn<int>(
                name: "SubTaskId",
                table: "Resources",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_SubTasks_SubTaskId",
                table: "Resources",
                column: "SubTaskId",
                principalTable: "SubTasks",
                principalColumn: "SubTaskId");
        }
    }
}
