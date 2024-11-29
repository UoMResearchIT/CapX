using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

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

            foreach (var person in groupedRows)
            {
                Console.WriteLine($"** Have {person.Count()} rows for {person.FirstOrDefault()?.Resource}");
            }

            // SQL script to ?





            throw new Exception("Got this far!");





            // Throw exceptions for missing people

            // Check for existence of activity and add if not

            // Check for existence of task and add if not -- might need to write these out as they need a duty category

            // Create timesheet objects to represent the week

            // Write the timesheet objects to the DB

            // Remove any timesheet codes that have no tasks
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
