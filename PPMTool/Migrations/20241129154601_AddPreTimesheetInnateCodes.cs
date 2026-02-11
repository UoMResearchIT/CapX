// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddPreTimesheetInnateCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Path to delimited file
            var filePath = $"./Migrations/Data/PreTimesheetInnateCodes.txt";

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

                var activityCode = values[0].Trim().Replace("'", "''"); ;
                var activityName = values[1].Trim().Replace("'", "''"); ;

                var sqlCommand = $@"
                    INSERT INTO InnateCodes (ActivityCode, ActivityName)
                    SELECT '{activityCode}', '{activityName}'
                    WHERE NOT EXISTS (
                        SELECT 1 FROM InnateCodes WHERE ActivityCode = '{activityCode}' AND ActivityName = '{activityName}'
                    );
                ";

                // Exceute sql command to add the record
                migrationBuilder.Sql(sqlCommand);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
