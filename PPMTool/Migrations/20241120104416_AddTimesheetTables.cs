// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddTimesheetTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Timesheets",
                columns: table => new
                {
                    TimesheetId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Info = table.Column<string>(type: "TEXT", nullable: true),
                    MinHours = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DateChanged = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedByPersonId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.TimesheetId);
                    table.ForeignKey(
                        name: "FK_Timesheets_People_ChangedByPersonId",
                        column: x => x.ChangedByPersonId,
                        principalTable: "People",
                        principalColumn: "PersonId");
                    table.ForeignKey(
                        name: "FK_Timesheets_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetActivities",
                columns: table => new
                {
                    TimesheetActivityId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimesheetId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InnateCodeTaskId = table.Column<int>(type: "INTEGER", nullable: true),
                    Hours = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetActivities", x => x.TimesheetActivityId);
                    table.ForeignKey(
                        name: "FK_TimesheetActivities_InnateCodeTasks_InnateCodeTaskId",
                        column: x => x.InnateCodeTaskId,
                        principalTable: "InnateCodeTasks",
                        principalColumn: "InnateCodeTaskId");
                    table.ForeignKey(
                        name: "FK_TimesheetActivities_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheets",
                        principalColumn: "TimesheetId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetActivities_InnateCodeTaskId",
                table: "TimesheetActivities",
                column: "InnateCodeTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetActivities_TimesheetId",
                table: "TimesheetActivities",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_ChangedByPersonId",
                table: "Timesheets",
                column: "ChangedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_PersonId",
                table: "Timesheets",
                column: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimesheetActivities");

            migrationBuilder.DropTable(
                name: "Timesheets");
        }
    }
}
