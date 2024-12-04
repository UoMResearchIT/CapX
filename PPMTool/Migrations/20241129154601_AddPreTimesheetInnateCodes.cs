using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddPreTimesheetInnateCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Path to delimited file
            var filePath = $"./Migrations/Data/PreTimesheetInnateCodes.txt";

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
                // Read all lines from the file
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    // Split the line by the delimiter
                    var values = line.Split('|');

                    if (values.Length != 2)
                    {
                        throw new Exception("Incorrect number of values in line");
                    }

                    // Add the Innate code objects
                    var obj = new InnateCode
                    {
                        ActivityCode = values[0].Trim(),
                        ActivityName = values[1].Trim()
                    };

                    // Check no duplicate on code before trying to add it
                    if (context.InnateCodes.Any(i => i.ActivityCode == obj.ActivityCode && i.ActivityName != obj.ActivityName))
                    {
                        throw new Exception($"Duplicate code found: {obj.ActivityCode} but with different name!");
                    }
                    // If both match then just leave it
                    else if (context.InnateCodes.Any(i => i.ActivityCode == obj.ActivityCode && i.ActivityName == obj.ActivityName))
                    {
                        Console.WriteLine($"Code already exists with same name so no need to add: {obj.ActivityCode} - {obj.ActivityName}");
                        continue;
                    }
                    else
                    {
                        context.InnateCodes.Add(obj);
                    }
                }

                // Save changes
                context.SaveChanges();
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
