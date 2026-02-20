// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using PPMTool.Enums;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddDefaultTasksForRecentCodes : Migration
    {
        private class LineAsObject
        {
            public string ActivityCode { get; set; }
            public string ActivityName { get; set; }
            public string TaskName { get; set; }
            public Duty Duty { get; set; }
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Path to delimited file
            var filePath = "./Migrations/Data/KnownInnateCodes.txt";

            // Read all lines from the file
            var lines = File.ReadAllLines(filePath);

            List<LineAsObject> linesAsObjects = new List<LineAsObject>();
            foreach (var line in lines)
            {
                // Split the line by the delimiter
                var values = line.Split('|');

                // Build objects
                linesAsObjects.Add(new LineAsObject
                {
                    ActivityCode = values[0],
                    ActivityName = values[1],
                    TaskName = values[2],
                    Duty = (Duty)int.Parse(values[3])
                });
            }

            // Group by the activity code
            var grouped = linesAsObjects.GroupBy(x => x.ActivityCode);

            foreach (var group in grouped)
            {
                // Get innate code
                var activityCode = group.Key;

                // SQL to check if match found against ActivityCode and if not then add
                migrationBuilder.Sql(
                    $@"
                        INSERT INTO InnateCodes (ActivityCode, ActivityName)
                        SELECT '{activityCode}', '{group.First().ActivityName}'
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM InnateCodes
                            WHERE LOWER(TRIM(ActivityCode)) = LOWER(TRIM('{activityCode}'))
                        );
                    "
                );

                foreach (var task in group)
                {
                    // SQL to check if match found for task name which matches matched InnateCode
                    // If not then add
                    migrationBuilder.Sql(
                        $@"
                            INSERT INTO InnateCodeTasks (TaskName, Duty, InnateCodeId)
                            SELECT '{task.TaskName}', {(int)task.Duty}, InnateCodeId
                            FROM InnateCodes
                            WHERE LOWER(TRIM(ActivityCode)) = LOWER(TRIM('{activityCode}'))
                            AND NOT EXISTS (
                                SELECT 1
                                FROM InnateCodeTasks
                                WHERE InnateCodeId = InnateCodes.InnateCodeId
                                AND LOWER(TRIM(TaskName)) = LOWER(TRIM('{task.TaskName}'))
                            )
                            LIMIT 1;
                        "
                    );
                }
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}