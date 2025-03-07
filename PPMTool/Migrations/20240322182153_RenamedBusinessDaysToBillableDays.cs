using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class RenamedBusinessDaysToBillableDays : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DurationBusinessDays",
                table: "SubTasks",
                newName: "DurationBillableDays");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DurationBillableDays",
                table: "SubTasks",
                newName: "DurationBusinessDays");
        }
    }
}
