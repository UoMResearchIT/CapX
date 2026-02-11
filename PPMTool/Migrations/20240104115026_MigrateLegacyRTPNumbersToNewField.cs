// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class MigrateLegacyRTPNumbersToNewField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET RTP = SUBSTR(Name, 5, INSTR(Name, ' ') - 5), Name = SUBSTR(Name, INSTR(Name, ' ') + 1)
                    WHERE Name LIKE 'RTP-%'
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                    UPDATE Projects
                    SET Name = 'RTP-' || RTP || ' ' || Name,
                    RTP = 0;
                "
            );
        }
    }
}
