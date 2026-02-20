// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedGradeToWLMChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Grade",
                table: "WorkloadModelChanges",
                type: "INTEGER",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.Sql(
                @"
                    UPDATE WorkloadModelChanges
                    SET Grade = CASE
                        WHEN ProjectAndServiceManagementFTE > 0 OR StaffManagementFTE > 0 OR ArchitectureFTE > 0 THEN 7
                        ELSE 6
                    END;
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Grade",
                table: "WorkloadModelChanges");
        }
    }
}
