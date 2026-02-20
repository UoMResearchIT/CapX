// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

namespace PPMTool.Migrations
{
    public partial class AddedSubTaskTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resource_SubTask_SubTaskId",
                table: "Resource");

            migrationBuilder.DropForeignKey(
                name: "FK_SubTask_Projects_ProjectId",
                table: "SubTask");

            migrationBuilder.DropForeignKey(
                name: "FK_SubTask_SubTask_PredecessorSubTaskId",
                table: "SubTask");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubTask",
                table: "SubTask");

            migrationBuilder.RenameTable(
                name: "SubTask",
                newName: "SubTasks");

            migrationBuilder.RenameIndex(
                name: "IX_SubTask_ProjectId",
                table: "SubTasks",
                newName: "IX_SubTasks_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_SubTask_PredecessorSubTaskId",
                table: "SubTasks",
                newName: "IX_SubTasks_PredecessorSubTaskId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubTasks",
                table: "SubTasks",
                column: "SubTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resource_SubTasks_SubTaskId",
                table: "Resource",
                column: "SubTaskId",
                principalTable: "SubTasks",
                principalColumn: "SubTaskId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubTasks_Projects_ProjectId",
                table: "SubTasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubTasks_SubTasks_PredecessorSubTaskId",
                table: "SubTasks",
                column: "PredecessorSubTaskId",
                principalTable: "SubTasks",
                principalColumn: "SubTaskId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resource_SubTasks_SubTaskId",
                table: "Resource");

            migrationBuilder.DropForeignKey(
                name: "FK_SubTasks_Projects_ProjectId",
                table: "SubTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_SubTasks_SubTasks_PredecessorSubTaskId",
                table: "SubTasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubTasks",
                table: "SubTasks");

            migrationBuilder.RenameTable(
                name: "SubTasks",
                newName: "SubTask");

            migrationBuilder.RenameIndex(
                name: "IX_SubTasks_ProjectId",
                table: "SubTask",
                newName: "IX_SubTask_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_SubTasks_PredecessorSubTaskId",
                table: "SubTask",
                newName: "IX_SubTask_PredecessorSubTaskId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubTask",
                table: "SubTask",
                column: "SubTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resource_SubTask_SubTaskId",
                table: "Resource",
                column: "SubTaskId",
                principalTable: "SubTask",
                principalColumn: "SubTaskId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubTask_Projects_ProjectId",
                table: "SubTask",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubTask_SubTask_PredecessorSubTaskId",
                table: "SubTask",
                column: "PredecessorSubTaskId",
                principalTable: "SubTask",
                principalColumn: "SubTaskId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
