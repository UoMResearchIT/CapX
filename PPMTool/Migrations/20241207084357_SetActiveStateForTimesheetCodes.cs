using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using PPMTool.Data.Context;
using PPMTool.Enums;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class SetActiveStateForTimesheetCodes : Migration
    {
        List<string> activeCodes = new List<string>
        {
            "01",
            "02",
            "03",
            "05",
            "06",
            "DMS",
            "INC",
            "P&A",
            "S-RES006",
            "S-RES007",
            "S-RES011",
            "S-RES012",
            "S-RES013",
            "S-RES014",
            "S-RES015"
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Load the configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // Get the connection string
            var connectionString = configuration.GetConnectionString("PPMToolContextConnection");

            // Create options for the custom DbContext
            var optionsBuilder = new DbContextOptionsBuilder<PPMToolContext>();
            optionsBuilder.UseSqlite(connectionString);

            // Now have the context to check stuff
            using (var context = new PPMToolContext(optionsBuilder.Options))
            {
                // Go through all the timesheet codes
                foreach (var code in context.InnateCodes)
                {
                    // Decide active
                    bool shouldBeActive = IsOnExceptionList(code.ActivityCode) ||
                        context.Projects.Any(x => x.InnateActivity.InnateCodeId == code.InnateCodeId &&
                            x.ProjectStatus != ProjectStatus.Finished &&
                            x.ProjectStatus != ProjectStatus.CancelledByCustomer &&
                            x.ProjectStatus != ProjectStatus.CancelledBidFailed &&
                            x.ProjectStatus != ProjectStatus.CancelledNoResource);

                    code.IsActive = shouldBeActive;
                    if (shouldBeActive)
                    {
                        Console.WriteLine($"** Setting {code.GetCodeAsString()} to ACTIVE");
                    }
                }

                // Save changes
                context.SaveChanges();
            }
        }

        private bool IsOnExceptionList(string activityCode)
        {
            return activeCodes.Contains(activityCode);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
