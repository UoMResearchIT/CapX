// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class SplitProjectAndServiceManagementOnWLMChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ProjectManagementFTE",
                table: "WorkloadModelChanges",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ServiceManagementFTE",
                table: "WorkloadModelChanges",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql(
                @"
                    UPDATE WorkloadModelChanges
                    SET ProjectManagementFTE = ProjectAndServiceManagementFTE
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectManagementFTE",
                table: "WorkloadModelChanges");

            migrationBuilder.DropColumn(
                name: "ServiceManagementFTE",
                table: "WorkloadModelChanges");
        }
    }
}
