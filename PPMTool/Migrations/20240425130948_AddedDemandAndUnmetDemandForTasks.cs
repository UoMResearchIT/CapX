// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedDemandAndUnmetDemandForTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Demand",
                table: "SubTasks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "UnmetDemand",
                table: "SubTasks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            // Migration takes existing "Unallocated Person" and any currently assigned resources and
            // uses that to populate the new "Demand" and "UnmetDemand" columns.
            // The Unallocated Person (ID = 6) is then removed from the resource table and deleted from the people table.
            migrationBuilder.Sql(
                @"
                    UPDATE SubTasks
                    SET Demand = (
                        SELECT IFNULL(SUM(AssignmentFTE), 0)
                        FROM Resources
                        WHERE Resources.SubTaskId = SubTasks.SubTaskId
                    );
                    UPDATE SubTasks
                    SET UnmetDemand = (
                        SELECT IFNULL(SUM(AssignmentFTE), 0)
                        FROM Resources
                        WHERE Resources.SubTaskId = SubTasks.SubTaskId AND Resources.PersonId = 6
                    );
                    DELETE FROM Resources
                    WHERE PersonId = 6;
                    DELETE FROM People
                    WHERE PersonId = 6;
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Demand",
                table: "SubTasks");

            migrationBuilder.DropColumn(
                name: "UnmetDemand",
                table: "SubTasks");

            // Migration is not reversible.
        }
    }
}
