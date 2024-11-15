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
                    TotalHours = table.Column<int>(type: "INTEGER", nullable: false),
                    Info = table.Column<string>(type: "TEXT", nullable: true),
                    MinHours = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.TimesheetId);
                    table.ForeignKey(
                        name: "FK_Timesheets_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTimeRecords",
                columns: table => new
                {
                    ActivityTimeRecordId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimesheetId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InnateCodeTaskId = table.Column<int>(type: "INTEGER", nullable: true),
                    Hours = table.Column<double>(type: "REAL", nullable: false),
                    DayType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTimeRecords", x => x.ActivityTimeRecordId);
                    table.ForeignKey(
                        name: "FK_ActivityTimeRecords_InnateCodeTasks_InnateCodeTaskId",
                        column: x => x.InnateCodeTaskId,
                        principalTable: "InnateCodeTasks",
                        principalColumn: "InnateCodeTaskId");
                    table.ForeignKey(
                        name: "FK_ActivityTimeRecords_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheets",
                        principalColumn: "TimesheetId");
                });

            migrationBuilder.CreateTable(
                name: "TimesheetWorkflows",
                columns: table => new
                {
                    TimesheetWorkflowId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimesheetId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DateChanged = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedByPersonId = table.Column<int>(type: "INTEGER", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetWorkflows", x => x.TimesheetWorkflowId);
                    table.ForeignKey(
                        name: "FK_TimesheetWorkflows_People_ChangedByPersonId",
                        column: x => x.ChangedByPersonId,
                        principalTable: "People",
                        principalColumn: "PersonId");
                    table.ForeignKey(
                        name: "FK_TimesheetWorkflows_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheets",
                        principalColumn: "TimesheetId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeRecords_InnateCodeTaskId",
                table: "ActivityTimeRecords",
                column: "InnateCodeTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeRecords_TimesheetId",
                table: "ActivityTimeRecords",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_PersonId",
                table: "Timesheets",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetWorkflows_ChangedByPersonId",
                table: "TimesheetWorkflows",
                column: "ChangedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetWorkflows_TimesheetId",
                table: "TimesheetWorkflows",
                column: "TimesheetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityTimeRecords");

            migrationBuilder.DropTable(
                name: "TimesheetWorkflows");

            migrationBuilder.DropTable(
                name: "Timesheets");
        }
    }
}
