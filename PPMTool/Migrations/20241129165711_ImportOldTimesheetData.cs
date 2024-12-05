using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentDateTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ImportOldTimesheetData : Migration
    {
        private class Entry
        {
            public DateTime Date { get; set; }
            public double Hours { get; set; }
        }

        private class LineAsObject
        {
            public string Resource { get; set; }
            public string Activity { get; set; }
            public string Task { get; set; }
            public List<Entry> Entries { get; set; }
        }

        private string Clean(string initial)
        {
            return initial.Replace("\"\"", "**").Replace("\"", "").Replace("**", "\"\"").Replace("\r", "");
        }

        private bool IsMatch(LineAsObject a, LineAsObject b)
        {
            return a.Resource == b.Resource && a.Activity == b.Activity && a.Task == b.Task;
        }

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
                // Rows for the full date range
                var listOfRowObjects = new List<LineAsObject>();

                for (var y = 2019; y <= 2024; y++)
                {
                    // Path to delimited file
                    var filePath = $"./Migrations/Data/InnateData_20241128_{y}-{y + 1}_mod.txt";

                    // Store the day dates
                    var dates = new List<DateTime>();

                    // Read all lines from the file
                    var lines = File.ReadAllLines(filePath);

                    List<LineAsObject> linesAsObjects = new List<LineAsObject>();
                    foreach (var line in lines)
                    {
                        // Split the line by the delimiter
                        var values = line.Split('|');

                        if (values.Length < 3)
                        {
                            continue;
                        }

                        // Store the dates from the header row
                        if (Clean(values[0]) == "Resource")
                        {
                            for (var i = 3; i < values.Length; i++)
                            {
                                dates.Add(DateTime.Parse(Clean(values[i])));
                            }
                            continue;
                        }

                        // Build a new blank object
                        var obj = new LineAsObject()
                        {
                            Resource = Clean(values[0]),
                            Activity = Clean(values[1]),
                            Task = Clean(values[2]),
                            Entries = new List<Entry>()
                        };

                        // If the object already exists otherwise add it
                        var match = listOfRowObjects.FirstOrDefault(x => IsMatch(x, obj));
                        if (match != null)
                        {
                            obj = match;
                        }
                        else
                        {
                            listOfRowObjects.Add(obj);
                        }

                        // Add entries
                        for (var i = 3; i < values.Length; i++)
                        {
                            // If there is a value then add it
                            if (Clean(values[i]) != "")
                            {
                                obj.Entries.Add(new Entry()
                                {
                                    Date = dates[i - 3],
                                    Hours = double.Parse(Clean(values[i]))
                                });
                            }
                        }
                    }
                }

                Console.WriteLine($"** Have {listOfRowObjects.Count} rows from the files!");

                // Group records by person
                var groupedRows = listOfRowObjects.GroupBy(x => x.Resource);

                // Avoid non-translatable issues with LINQ by copying the data to lists
                var innateCodes = context.InnateCodes.ToList();
                var innateCodeTasks = context.InnateCodeTasks.Include(x => x.InnateCode).ToList();

                // Check existence of dependent data
                var missingActivityCodes = new List<string>();
                var missingTaskCodes = new List<string>();
                foreach (var person in groupedRows)
                {
                    Console.WriteLine($"** Have {person.Count()} rows for {person.First().Resource}");

                    // Check the person exists
                    if (context.People.FirstOrDefault(x => x.Name == person.First().Resource) == null)
                    {
                        throw new Exception($"Person {person.First().Resource} does not exist in the database!");
                    }

                    // Check to see if all the activity/task codes exist that the timesheet references
                    foreach (var row in person)
                    {
                        if (innateCodes.FirstOrDefault(x => x.GetCodeAsString() == row.Activity) == null)
                        {
                            var val = row.Activity;
                            if (!missingActivityCodes.Contains(val))
                            {
                                missingActivityCodes.Add(val);
                            }
                        }

                        if (innateCodeTasks.FirstOrDefault(x => x.TaskName == row.Task && x.InnateCode.GetCodeAsString() == row.Activity) == null)
                        {
                            var val = $"{row.Activity}|{row.Task}";
                            if (!missingTaskCodes.Contains(val))
                            {
                                missingTaskCodes.Add(val);
                            }
                        }
                    }
                }

                // Stop migration if data is missing from the DB
                if (missingActivityCodes.Count > 0 || missingTaskCodes.Count > 0)
                {
                    Console.WriteLine($"** Missing activity codes:\n{string.Join("\n", missingActivityCodes.OrderBy(x => x))}");
                    Console.WriteLine($"** Missing task codes:\n{string.Join("\n", missingTaskCodes.OrderBy(x => x))}");
                    throw new Exception($"Missing activity / task codes! Cannot continue until they are added!");
                }

                // We have everything we need in the DB so can go ahead and add
                foreach (var person in groupedRows)
                {

                    // First timesheet is going to be 7th January 2019
                    var firstWeek = new DateTime(2019, 1, 7);

                    // Last entry for this person
                    var lastWeek = person.OrderByDescending(x => x.Entries.Max(y => y.Date)).First().Entries.Max(y => y.Date).FirstDayOfWeek();
                    Console.WriteLine($"** {person.FirstOrDefault()?.Resource} has data up to week beginning {lastWeek.ToLongDateString()}");

                    // Convert to a set of timesheet and timesheet entry objects
                    for (var week = firstWeek; week <= lastWeek; week = week.AddDays(7))
                    {
                        // Create a new timesheet object for the week
                        var timesheet = new Timesheet()
                        {
                            StartDate = week,
                            TimesheetEntries = new List<TimesheetEntry>(),
                            Owner = context.People.FirstOrDefault(x => x.Name == person.First().Resource),
                            CreatedDate = DateTime.Now,
                            DateStatusChanged = DateTime.Now,
                            Info = "Automatic import from Innate",
                            Status = TimesheetStatus.Approved,
                            StatusChangedBy = context.People.FirstOrDefault(x => x.Name == person.First().Resource).LineManager ?? context.People.FirstOrDefault(x => x.Name == person.First().Resource)
                        };

                        // Extract the data for each day of the timesheet week for each task
                        foreach (var row in person)
                        {
                            var entries = row.Entries.Where(x => x.Date >= week && x.Date < week.AddDays(7)).ToList();

                            if (entries.Count > 0)
                            {
                                // Create a new timesheet entry object
                                var entry = new TimesheetEntry()
                                {
                                    Timesheet = timesheet,
                                    InnateCodeTask = innateCodeTasks.FirstOrDefault(x => x.TaskName == row.Task && x.InnateCode.GetCodeAsString() == row.Activity),
                                    MondayHours = entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Monday)?.Hours ?? 0,
                                    TuesdayHours = entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Tuesday)?.Hours ?? 0,
                                    WednesdayHours = entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Wednesday)?.Hours ?? 0,
                                    ThursdayHours = entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Thursday)?.Hours ?? 0,
                                    FridayHours = entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Friday)?.Hours ?? 0,
                                    SaturdayHours = entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Saturday)?.Hours ?? 0,
                                    SundayHours = entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Sunday)?.Hours ?? 0
                                };

                                // Add the timesheet entry to the timesheet
                                timesheet.TimesheetEntries.Add(entry);
                            }
                        }

                        // Add the timesheet to the DB
                        context.Timesheets.Add(timesheet);
                    }

                    // Save the changes
                    context.SaveChanges();
                }
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    DELETE FROM Timesheets;
                "
            );
        }
    }
}
