using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class SwitchIsLeadershipFlagToDuty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsLeadershipTask",
                table: "SubTasks",
                newName: "TaskDuty");

            migrationBuilder.Sql(@"
                UPDATE SubTasks
                SET TaskDuty = 5
                WHERE TaskDuty = 1;
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                UPDATE SubTasks
                SET TaskDuty = 1
                WHERE TaskDuty = 5;
            ");

            migrationBuilder.RenameColumn(
                name: "TaskDuty",
                table: "SubTasks",
                newName: "IsLeadershipTask");
        }
    }
}
