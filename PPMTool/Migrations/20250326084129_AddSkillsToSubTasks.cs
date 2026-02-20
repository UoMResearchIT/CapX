// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddSkillsToSubTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkillTagSubTask",
                columns: table => new
                {
                    SkillsRequiredSkillTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    TasksNeedingThisSkillSubTaskId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTagSubTask", x => new { x.SkillsRequiredSkillTagId, x.TasksNeedingThisSkillSubTaskId });
                    table.ForeignKey(
                        name: "FK_SkillTagSubTask_SkillTags_SkillsRequiredSkillTagId",
                        column: x => x.SkillsRequiredSkillTagId,
                        principalTable: "SkillTags",
                        principalColumn: "SkillTagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillTagSubTask_SubTasks_TasksNeedingThisSkillSubTaskId",
                        column: x => x.TasksNeedingThisSkillSubTaskId,
                        principalTable: "SubTasks",
                        principalColumn: "SubTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillTagSubTask_TasksNeedingThisSkillSubTaskId",
                table: "SkillTagSubTask",
                column: "TasksNeedingThisSkillSubTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillTagSubTask");
        }
    }
}
