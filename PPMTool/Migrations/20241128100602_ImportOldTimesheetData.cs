using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ImportOldTimesheetData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Read in the timesheets one row at a time and convert to objects

            // Throw exceptions for missing people

            // Check for existence of activity and add if not

            // Check for existence of task and add if not

            // Create timesheet objects to represent the week

            // Write the timesheet objects to the DB

            // Remove any timesheet codes that have no tasks
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
