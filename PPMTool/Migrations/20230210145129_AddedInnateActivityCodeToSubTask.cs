using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedInnateActivityCodeToSubTask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InnateActivity",
                table: "SubTasks",
                type: "TEXT",
                nullable: false,
                defaultValue: "RCS04 - Research Software & Data Engineering (RSDE) Support");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InnateActivity",
                table: "SubTasks");
        }
    }
}
