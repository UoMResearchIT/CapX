// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
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
            public string CreatedDate { get; set; } = DateTime.Now.ToString("R");
            public string RevisedDate { get; set; } = DateTime.Now.ToString("R");
            public int Revision { get; set; } = 0;

            public int Number { get; set; }
        }

        private string Clean(string initial)
        {
            return initial.Replace("\"\"", "**").Replace("\"", "").Replace("**", "\"\"").Replace("\r", "");
        }

        public bool IsValidHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return false;
            }

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                return doc.ParseErrors == null || !doc.ParseErrors.Any();
            }
            catch
            {
                return false;
            }
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

                if (values.Length != 6)
                {
                    Console.WriteLine($"** [ERR] Incorrect number of values => {line}");
                    throw new Exception("Incorrect number of entries on line!");
                }

                // Ignore the header row
                if (Clean(values[0]) == "LegacyId") continue;

                // Check the objective is valid HTML
                if (!IsValidHtml(Clean(values[4])))
                {
                    Console.WriteLine($"** [ERR] Incorrect HTML => {Clean(values[4])}");
                    throw new Exception("Invalid HTML");
                }

                // Build objects
                linesAsObjects.Add(new LineAsObject
                {
                    LegacyId = Clean(values[0]),
                    Grade = int.Parse(values[1]),
                    Category = (CompetencyCategory)int.Parse(values[2]),
                    Description = Clean(values[3]),
                    Objective = Clean(values[4]),
                    Number = int.Parse(values[5])
                });
            }

            Console.WriteLine($"** Read in {lines.Length} lines! Have {linesAsObjects.Count} DB items to add!");

            // Now add to the DB
            foreach (var obj in linesAsObjects)
            {
                migrationBuilder.Sql(
                    $@"
                        INSERT INTO Competency (LegacyId, Grade, Category, Description, Objective, Revision, CreatedDate, RevisionDate, IsActive, Number)
                        SELECT '{obj.LegacyId}', {obj.Grade}, {(int)obj.Category}, '{obj.Description}', '{obj.Objective}', {obj.Revision}, '{obj.CreatedDate}', '{obj.RevisedDate}', 1, {obj.Number}
                        WHERE NOT EXISTS (
                            SELECT 1 FROM Competency WHERE LegacyId = '{obj.LegacyId}'
                        );

                    "
                );
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
