// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class StripCarriageReturnsFromInnateActivity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET ""InnateActivity"" = REPLACE(REPLACE(""InnateActivity"", '""', ''), CHAR(13), '')
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not possible to reverse this migration
        }
    }
}
