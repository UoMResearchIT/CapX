// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PPMTool.Migrations
{
    public partial class AddedProjectTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: false),
                    PI = table.Column<string>(type: "TEXT", nullable: false),
                    Portfolio = table.Column<int>(type: "INTEGER", nullable: false),
                    Budget = table.Column<double>(type: "REAL", nullable: false),
                    FundsReceived = table.Column<double>(type: "REAL", nullable: false),
                    FundingStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlannedWorkHours = table.Column<double>(type: "REAL", nullable: false),
                    ActualWorkHours = table.Column<double>(type: "REAL", nullable: false),
                    PlannedCost = table.Column<double>(type: "REAL", nullable: false),
                    ActualCost = table.Column<double>(type: "REAL", nullable: false),
                    IsDone = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                });

            migrationBuilder.CreateTable(
                name: "SubTask",
                columns: table => new
                {
                    SubTaskId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskType = table.Column<int>(type: "INTEGER", nullable: false),
                    HasFixedStart = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubTaskId1 = table.Column<int>(type: "INTEGER", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlannedWorkHours = table.Column<double>(type: "REAL", nullable: false),
                    ActualWorkHours = table.Column<double>(type: "REAL", nullable: false),
                    PlannedCost = table.Column<double>(type: "REAL", nullable: false),
                    ActualCost = table.Column<double>(type: "REAL", nullable: false),
                    IsDone = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubTask", x => x.SubTaskId);
                    table.ForeignKey(
                        name: "FK_SubTask_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubTask_SubTask_SubTaskId1",
                        column: x => x.SubTaskId1,
                        principalTable: "SubTask",
                        principalColumn: "SubTaskId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Resource",
                columns: table => new
                {
                    ResourceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: true),
                    Percentage = table.Column<double>(type: "REAL", nullable: false),
                    SubTaskId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resource", x => x.ResourceId);
                    table.ForeignKey(
                        name: "FK_Resource_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resource_SubTask_SubTaskId",
                        column: x => x.SubTaskId,
                        principalTable: "SubTask",
                        principalColumn: "SubTaskId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resource_PersonId",
                table: "Resource",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Resource_SubTaskId",
                table: "Resource",
                column: "SubTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTask_ProjectId",
                table: "SubTask",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTask_SubTaskId1",
                table: "SubTask",
                column: "SubTaskId1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Resource");

            migrationBuilder.DropTable(
                name: "SubTask");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
