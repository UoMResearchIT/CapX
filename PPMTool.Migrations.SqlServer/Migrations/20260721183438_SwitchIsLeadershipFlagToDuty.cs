using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SwitchIsLeadershipFlagToDuty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskDuty",
                table: "SubTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE SubTasks
                SET TaskDuty = 5
                WHERE IsLeadershipTask = 1;

                UPDATE SubTasks
                SET TaskDuty = 1
                WHERE IsLeadershipTask = 0;
            ");

            migrationBuilder.DropColumn(
                name: "IsLeadershipTask",
                table: "SubTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLeadershipTask",
                table: "SubTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE SubTasks
                SET IsLeadershipTask = 1
                WHERE TaskDuty = 5;

                UPDATE SubTasks
                SET IsLeadershipTask = 0
                WHERE TaskDuty = 1;
            ");

            migrationBuilder.DropColumn(
                name: "TaskDuty",
                table: "SubTasks");
        }
    }
}
