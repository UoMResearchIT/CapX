// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddPreTimesheetInnateTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Path to delimited file
            var filePath = $"./Migrations/Data/PreTimesheetInnateTasks.txt";

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

                var activityCode = codeValues[0].Trim().Replace("'", "''");
                var activityName = codeValues.Length == 2 ? codeValues[1].Trim().Replace("'", "''") : string.Join(" - ", codeValues.Skip(1)).Trim().Replace("'", "''");
                var taskName = values[1].Trim().Replace("'", "''");
                var duty = int.Parse(values[2]);

                var sqlCommand = $@"
                    INSERT INTO InnateCodeTasks (TaskName, Duty, InnateCodeId)
                    SELECT '{taskName}', {duty}, InnateCodeId
                    FROM InnateCodes
                    WHERE ActivityCode = '{activityCode}' AND ActivityName = '{activityName}'
                    AND NOT EXISTS (
                        SELECT 1 FROM InnateCodeTasks
                        WHERE TaskName = '{taskName}' AND InnateCodeId = (
                            SELECT InnateCodeId FROM InnateCodes
                            WHERE ActivityCode = '{activityCode}' AND ActivityName = '{activityName}'
                        )
                    );
                ";

                // Execute the SQL script
                migrationBuilder.Sql(sqlCommand);

                // Clean up Innate codes by removing those that have no tasks
                var cleanupSqlCommand = @"
                    DELETE FROM InnateCodes
                    WHERE InnateCodeId NOT IN (SELECT DISTINCT InnateCodeId FROM InnateCodeTasks)
                    AND InnateCodeId NOT IN (SELECT DISTINCT InnateActivityInnateCodeId FROM Projects);
                ";

                // Execute the cleanup script
                migrationBuilder.Sql(cleanupSqlCommand);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
