using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddLineManagerRelationships : Migration
    {
        private class LineManagerAssignment
        {
            public int PersonId { get; set; }
            public int LineManagerId { get; set; }
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Path to delimited file
            var filePath = "./Migrations/Data/LineManagerAssignments.txt";

            // Read all lines from the file
            var lines = File.ReadAllLines(filePath);

            List<LineManagerAssignment> linesAsObjects = new List<LineManagerAssignment>();
            foreach (var line in lines)
            {
                // Split the line by the delimiter
                var values = line.Split('|');

                if (values.Length != 3)
                {
                    Console.WriteLine($"** [ERR] Incorrect number of values => {line}");
                    throw new Exception("Incorrect number of entries on line!");
                }

                // Build objects
                linesAsObjects.Add(new LineManagerAssignment
                {
                    PersonId = int.Parse(values[0]),
                    LineManagerId = int.Parse(values[1])
                });
            }

            Console.WriteLine($"** Read in {lines.Length} lines!");

            // Now update DB
            foreach (var obj in linesAsObjects)
            {
                migrationBuilder.Sql(
                    $@"
                        UPDATE People
                        SET LineManagerPersonId = {obj.LineManagerId}
                        WHERE PersonId = {obj.PersonId} AND LineManagerPersonId IS NULL;
                    "
                );
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
