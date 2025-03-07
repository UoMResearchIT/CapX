// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedDueAndCompletedDatesToNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "Notes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Notes",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Notes");
        }
    }
}
