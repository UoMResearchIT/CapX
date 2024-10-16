using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedLeadershipCostOptionToTask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChargeLeadership",
                table: "SubTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Set all the tasks called "Maintenance" to leadership charges = false
            migrationBuilder.Sql(
                @"
                    UPDATE SubTasks
                    SET ChargeLeadership = 1
                    WHERE Name = 'Maintenance';

                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargeLeadership",
                table: "SubTasks");
        }
    }
}
