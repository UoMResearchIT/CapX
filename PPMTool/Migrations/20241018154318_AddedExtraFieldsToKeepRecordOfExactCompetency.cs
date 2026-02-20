// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedExtraFieldsToKeepRecordOfExactCompetency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompetencyDescription",
                table: "CompetencyAssessment",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompetencyObjective",
                table: "CompetencyAssessment",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetencyDescription",
                table: "CompetencyAssessment");

            migrationBuilder.DropColumn(
                name: "CompetencyObjective",
                table: "CompetencyAssessment");
        }
    }
}
