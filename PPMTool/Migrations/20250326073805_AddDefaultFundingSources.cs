// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddDefaultFundingSources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO FundingSources (ProjectId, FundingSourceType, HasAccountCode)
                SELECT DISTINCT p.ProjectId, 2, 0
                FROM Projects p
                JOIN Payments pay ON p.ProjectId = pay.ProjectId
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM FundingSources fs
                    WHERE fs.ProjectId = p.ProjectId
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM FundingSources");
        }
    }
}
