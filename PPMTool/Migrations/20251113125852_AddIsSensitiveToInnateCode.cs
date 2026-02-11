// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSensitiveToInnateCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSensitive",
                table: "InnateCodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Mark specific codes as sensitive from data file
            var filePath = "./Migrations/Data/SensitiveInnateCodes.txt";
            if (File.Exists(filePath))
            {
                Console.WriteLine($"Reading sensitive codes from {filePath}");
                var lines = File.ReadAllLines(filePath);
                Console.WriteLine($"Found {lines.Length} codes to mark as sensitive");

                foreach (var line in lines)
                {
                    var activityCode = line.Trim().Replace("'", "''");
                    if (!string.IsNullOrWhiteSpace(activityCode))
                    {
                        Console.WriteLine($"  Marking code '{activityCode}' as sensitive");
                        var sqlCommand = $@"
                            UPDATE InnateCodes
                            SET IsSensitive = 1
                            WHERE ActivityCode = '{activityCode}';
                        ";
                        migrationBuilder.Sql(sqlCommand);
                    }
                }
                Console.WriteLine("Completed marking sensitive codes");
            }
            else
            {
                Console.WriteLine($"Warning: Sensitive codes file not found at {filePath}");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSensitive",
                table: "InnateCodes");
        }
    }
}
