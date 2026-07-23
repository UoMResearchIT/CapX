using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations.PostgreSql.Migrations
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
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE ""SubTasks""
                SET ""TaskDuty"" = 5
                WHERE ""IsLeadershipTask"" = TRUE;

                UPDATE ""SubTasks""
                SET ""TaskDuty"" = 1
                WHERE ""IsLeadershipTask"" = FALSE;
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
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE ""SubTasks""
                SET ""IsLeadershipTask"" = TRUE
                WHERE ""TaskDuty"" = 5;

                UPDATE ""SubTasks""
                SET ""IsLeadershipTask"" = FALSE
                WHERE ""TaskDuty"" = 1;
            ");

            migrationBuilder.DropColumn(
                name: "TaskDuty",
                table: "SubTasks");
        }
    }
}
