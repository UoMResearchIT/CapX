using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class SetActiveStateForTimesheetCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // List of activity codes that should be active
            var activeCodes = new[]
            {
                "01", "02", "03", "05", "06", "DMS", "INC", "P&A",
                "S-RES006", "S-RES007", "S-RES011", "S-RES012",
                "S-RES013", "S-RES014", "S-RES015"
            };

            // Format for SQL IN clause
            var formattedCodes = string.Join(", ", activeCodes.Select(code => $"'{code.Replace("'", "''")}'"));

            // SQL to set IsActive = 1 for codes in the exception list
            migrationBuilder.Sql($@"
                UPDATE InnateCodes
                SET IsActive = 1
                WHERE ActivityCode IN ({formattedCodes});
            ");

            // SQL to set IsActive = 1 for codes linked to active projects
            // Finished and cancelled states are 7-10 as enum values at the time
            migrationBuilder.Sql(@"
                UPDATE InnateCodes
                SET IsActive = 1
                WHERE InnateCodeId IN (
                    SELECT ia.InnateCodeId
                    FROM Projects p
                    JOIN InnateCodes ia ON p.InnateActivityInnateCodeId = ia.InnateCodeId
                    WHERE p.ProjectStatus NOT IN (7, 8, 9, 10)
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reset IsActive to false although not sure what the original state was so not reversible
            migrationBuilder.Sql("UPDATE InnateCodes SET IsActive = 0;");
        }
    }

}
