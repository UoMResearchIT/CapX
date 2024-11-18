using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddTimesheetTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityTimeRecords");

            migrationBuilder.DropTable(
                name: "TimesheetWorkflows");

            migrationBuilder.RenameColumn(
                name: "TotalHours",
                table: "Timesheets",
                newName: "Status");

            migrationBuilder.AddColumn<int>(
                name: "ChangedByPersonId",
                table: "Timesheets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateChanged",
                table: "Timesheets",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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
                name: "IX_Timesheets_ChangedByPersonId",
                table: "Timesheets",
                column: "ChangedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetActivities_InnateCodeTaskId",
                table: "TimesheetActivities",
                column: "InnateCodeTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetActivities_TimesheetId",
                table: "TimesheetActivities",
                column: "TimesheetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_People_ChangedByPersonId",
                table: "Timesheets",
                column: "ChangedByPersonId",
                principalTable: "People",
                principalColumn: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_People_ChangedByPersonId",
                table: "Timesheets");

            migrationBuilder.DropTable(
                name: "TimesheetActivities");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_ChangedByPersonId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "ChangedByPersonId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "DateChanged",
                table: "Timesheets");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Timesheets",
                newName: "TotalHours");

            migrationBuilder.CreateTable(
                name: "ActivityTimeRecords",
                columns: table => new
                {
                    ActivityTimeRecordId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InnateCodeTaskId = table.Column<int>(type: "INTEGER", nullable: true),
                    TimesheetId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DayType = table.Column<string>(type: "TEXT", nullable: true),
                    Hours = table.Column<double>(type: "REAL", nullable: false)
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
                    ChangedByPersonId = table.Column<int>(type: "INTEGER", nullable: true),
                    TimesheetId = table.Column<int>(type: "INTEGER", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    DateChanged = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "IX_TimesheetWorkflows_ChangedByPersonId",
                table: "TimesheetWorkflows",
                column: "ChangedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetWorkflows_TimesheetId",
                table: "TimesheetWorkflows",
                column: "TimesheetId");
        }
    }
}
