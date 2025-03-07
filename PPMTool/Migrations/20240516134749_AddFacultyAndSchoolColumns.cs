// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddFacultyAndSchoolColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int?>(
                name: "Faculty",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int?>(
                name: "School",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET Faculty = CASE
                        WHEN Portfolio = 2 THEN 3
                        WHEN Portfolio = 3 THEN 4
                        WHEN Portfolio = 4 THEN 5
                        WHEN Portfolio IN (5, 6, 7, 8) THEN 0
                        WHEN Portfolio = 9 THEN 6
                        ELSE Portfolio
                    END;
                    ALTER TABLE Projects DROP COLUMN Portfolio;
                ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                table: "Projects",
                name: "Portfolio",
                type: "INTEGER",
                nullable: true
                );

            // Not an exact reversal of the Up method
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET Portfolio = CASE
                        WHEN Faculty = 3 THEN 2
                        WHEN Faculty = 4 THEN 3
                        WHEN Faculty = 5 THEN 4
                        WHEN Faculty = 0 THEN 6
                        WHEN Faculty = 6 THEN 9
                        ELSE Faculty
                    END;
                ");

            migrationBuilder.DropColumn(
                name: "School",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Faculty",
                table: "Projects");
        }
    }
}
