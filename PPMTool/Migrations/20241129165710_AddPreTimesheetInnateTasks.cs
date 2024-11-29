using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddPreTimesheetInnateTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Path to delimited file
            var filePath = $"./Migrations/Data/PreTimesheetInnateTasks.txt";

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

                    if (values.Length != 3)
                    {
                        throw new Exception("Incorrect number of values in line");
                    }

                    // Build the objects
                    var codeValues = values[0].Split(" - ");
                    if (codeValues.Length < 2)
                    {
                        throw new Exception("Incorrect number of values in code");
                    }

                    var code = new InnateCode
                    {
                        ActivityCode = codeValues[0].Trim(),
                        ActivityName = codeValues.Length == 2 ? codeValues[1] : string.Join(" - ", codeValues.Skip(1)).Trim()
                    };

                    // Retrieve code from the database
                    var knownInnateCodes = context.InnateCodes.ToList();
                    var knownTasks = context.InnateCodeTasks.Include(x => x.InnateCode).ToList();
                    var matchingCode = knownInnateCodes.FirstOrDefault(x => x.GetCodeAsString() == code.GetCodeAsString());
                    if (matchingCode == null)
                    {
                        throw new Exception($"Code not found: {code.GetCodeAsString()}");
                    }

                    // Now build task and attache code
                    var task = new InnateCodeTask
                    {
                        TaskName = values[1].Trim(),
                        Duty = (Duty)int.Parse(values[2]),
                        InnateCode = matchingCode
                    };

                    // Check no duplicate on task before trying to add it
                    if (context.InnateCodes.Any(i => i.ActivityCode == task.InnateCode.ActivityCode &&
                        i.ActivityName == task.InnateCode.ActivityName) &&
                        knownTasks.Any(x => x.TaskName == task.TaskName && x.InnateCode.GetCodeAsString() == matchingCode.GetCodeAsString()))
                    {
                        Console.WriteLine($"Existing code/task combination already in the DB {matchingCode.GetCodeAsString()} | {task.TaskName} => Not adding it again");
                    }

                    // Add the task
                    else
                    {
                        context.InnateCodeTasks.Add(task);
                    }
                }

                // Clean up Innate codes by removing those that have no tasks
                foreach (var code in context.InnateCodes.Where(x => x.Tasks.Count == 0))
                {
                    context.InnateCodes.Remove(code);
                }

                // Save
                context.SaveChanges();
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
