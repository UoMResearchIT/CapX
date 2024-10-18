using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;
using PPMTool.Enums;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ImportCompetenciesVersion18 : Migration
    {
        private class LineAsObject
        {
            public string LegacyId { get; set; }
            public int Grade { get; set; }
            public CompetencyCategory Category { get; set; }
            public string Description { get; set; }
            public string Objective { get; set; }
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Path to delimited file
            var filePath = "./Migrations/Data/CompetenciesV18ForImport.txt";

            // Read all lines from the file
            var lines = File.ReadAllLines(filePath);

            List<LineAsObject> linesAsObjects = new List<LineAsObject>();
            foreach (var line in lines)
            {
                // Split the line by the delimiter
                var values = line.Split('|');

                if (values.Length != 5)
                {
                    Console.WriteLine($"[ERR] Incorrect number of values => {line}");
                    continue;
                }

                // Ignore the header row
                if (values[0].Replace("\"", "").Replace("\r", "") == "LegacyId") continue;

                // Build objects
                linesAsObjects.Add(new LineAsObject
                {
                    LegacyId = values[0].Replace("\"", "").Replace("\r", ""),
                    Grade = int.Parse(values[1].Replace("\"", "").Replace("\r", "")),
                    Category = (CompetencyCategory)int.Parse(values[2].Replace("\"", "").Replace("\r", "")),
                    Description = values[3].Replace("\"", "").Replace("\r", ""),
                    Objective = values[4].Replace("\"", "").Replace("\r", "")
                });
            }

            Console.WriteLine($"Read in {lines.Length} lines! Have {linesAsObjects.Count} DB items to add!");

            // Now add to the DB







        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
