// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ChangedNameOfManagementTaskColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ManagementTaskFTE",
                table: "Projects",
                newName: "LeadershipFTE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LeadershipFTE",
                table: "Projects",
                newName: "ManagementTaskFTE");
        }
    }
}
