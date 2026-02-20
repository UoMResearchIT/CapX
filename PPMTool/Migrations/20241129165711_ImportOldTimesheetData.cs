// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentDateTime;
using Microsoft.EntityFrameworkCore.Migrations;
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
                            dates.Add(DateTime.ParseExact(Clean(values[i]), "dd/MM/yyyy", CultureInfo.InvariantCulture));
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

            // We have everything we need in the DB so can go ahead and add
            var sqlScript = string.Empty;
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
                    var ownerIdQuery = $"(SELECT PersonId FROM People WHERE Name = '{person.First().Resource.Replace("'", "''")}')";
                    var statusChangedByIdQuery = $"(SELECT COALESCE((SELECT LineManagerId FROM People WHERE Name = '{person.First().Resource.Replace("'", "''")}'), {ownerIdQuery}))";

                    sqlScript += $@"
                        INSERT INTO Timesheets (StartDate, CreatedDate, DateStatusChanged, Info, Status, OwnerId, StatusChangedById)
                        VALUES ('{week:yyyy-MM-dd}', '{DateTime.Now:yyyy-MM-dd}', '{DateTime.Now:yyyy-MM-dd}', 'Automatic import from Innate', {(int)TimesheetStatus.Approved}, {ownerIdQuery}, {statusChangedByIdQuery});
                    ";

                    // Extract the data for each day of the timesheet week for each task
                    foreach (var row in person)
                    {
                        var entries = row.Entries.Where(x => x.Date >= week && x.Date < week.AddDays(7)).ToList();

                        if (entries.Count > 0)
                        {
                            sqlScript += $@"
                                INSERT INTO TimesheetEntries (TimesheetId, InnateCodeTaskId, MondayHours, TuesdayHours, WednesdayHours, ThursdayHours, FridayHours, SaturdayHours, SundayHours)
                                SELECT (SELECT TimesheetId FROM Timesheets WHERE StartDate = '{week:yyyy-MM-dd}' AND OwnerId = {ownerIdQuery} LIMIT 1), 
                                       (SELECT InnateCodeTaskId FROM InnateCodeTasks 
                                        WHERE TaskName = '{row.Task.Replace("'", "''")}' 
                                        AND InnateCodeId = (SELECT InnateCodeId FROM InnateCodes WHERE ActivityCode = '{row.Activity.Split(" - ")[0].Trim().Replace("'", "''")}' AND ActivityName = '{row.Activity.Split(" - ")[1].Trim().Replace("'", "''")}')),
                                       {entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Monday)?.Hours ?? 0},
                                       {entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Tuesday)?.Hours ?? 0},
                                       {entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Wednesday)?.Hours ?? 0},
                                       {entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Thursday)?.Hours ?? 0},
                                       {entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Friday)?.Hours ?? 0},
                                       {entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Saturday)?.Hours ?? 0},
                                       {entries.FirstOrDefault(x => x.Date.DayOfWeek == DayOfWeek.Sunday)?.Hours ?? 0};
                            ";
                        }
                    }
                }
            }

            // Execute the SQL script
            if (!string.IsNullOrWhiteSpace(sqlScript))
            {
                migrationBuilder.Sql(sqlScript);
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
