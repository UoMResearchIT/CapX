// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class FundingStatusFlagDataUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Map the old enum values to the new ones
            migrationBuilder.Sql(
                "UPDATE Projects " +
                "SET FundingStatus = CASE " +
                "WHEN FundingStatus = 1 THEN 0 " +
                "WHEN FundingStatus = 2 THEN 1 " +
                "WHEN FundingStatus = 3 THEN 2 " +
                "ELSE FundingStatus = 99 " +
                "END " +
                "WHERE FundingStatus = 1 OR FundingStatus = 2 OR FundingStatus = 3"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // We can't actually map these back exactly as two enums were merged during the Up operation.
            // Instead we just map back to one of them
            migrationBuilder.Sql(
                "UPDATE Projects " +
                "SET FundingStatus = CASE " +
                "WHEN FundingStatus = 0 THEN 1 " +
                "WHEN FundingStatus = 1 THEN 2 " +
                "WHEN FundingStatus = 2 THEN 3 " +
                "ELSE FundingStatus = 99 " +
                "END " +
                "WHERE FundingStatus = 0 OR FundingStatus = 1 OR FundingStatus = 2"
            );
        }
    }
}
