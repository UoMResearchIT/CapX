// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedNewPreSubmissionProjectStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET ProjectStatus = ProjectStatus + 2;
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET ProjectStatus = ProjectStatus - 2;
                "
            );
        }
    }
}
