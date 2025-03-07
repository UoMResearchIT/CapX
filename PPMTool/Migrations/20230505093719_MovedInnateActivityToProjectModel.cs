using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class MovedInnateActivityToProjectModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InnateActivity",
                table: "SubTasks");

            migrationBuilder.AddColumn<string>(
                name: "InnateActivity",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InnateActivity",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "InnateActivity",
                table: "SubTasks",
                type: "TEXT",
                nullable: true);
        }
    }
}
