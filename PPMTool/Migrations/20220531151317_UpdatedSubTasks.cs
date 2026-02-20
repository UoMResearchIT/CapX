// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

namespace PPMTool.Migrations
{
    public partial class UpdatedSubTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubTask_SubTask_SubTaskId1",
                table: "SubTask");

            migrationBuilder.RenameColumn(
                name: "SubTaskId1",
                table: "SubTask",
                newName: "PredecessorSubTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_SubTask_SubTaskId1",
                table: "SubTask",
                newName: "IX_SubTask_PredecessorSubTaskId");

            migrationBuilder.AddColumn<double>(
                name: "DurationHours",
                table: "SubTask",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsWorkDriven",
                table: "SubTask",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_SubTask_SubTask_PredecessorSubTaskId",
                table: "SubTask",
                column: "PredecessorSubTaskId",
                principalTable: "SubTask",
                principalColumn: "SubTaskId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubTask_SubTask_PredecessorSubTaskId",
                table: "SubTask");

            migrationBuilder.DropColumn(
                name: "DurationHours",
                table: "SubTask");

            migrationBuilder.DropColumn(
                name: "IsWorkDriven",
                table: "SubTask");

            migrationBuilder.RenameColumn(
                name: "PredecessorSubTaskId",
                table: "SubTask",
                newName: "SubTaskId1");

            migrationBuilder.RenameIndex(
                name: "IX_SubTask_PredecessorSubTaskId",
                table: "SubTask",
                newName: "IX_SubTask_SubTaskId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SubTask_SubTask_SubTaskId1",
                table: "SubTask",
                column: "SubTaskId1",
                principalTable: "SubTask",
                principalColumn: "SubTaskId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
