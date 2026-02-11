// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class ConnectFundingSourceToPayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Payments
                SET SourceFundingSourceId = (
                    SELECT FundingSourceId
                    FROM FundingSources fs
                    WHERE fs.ProjectId = Payments.ProjectId
                    ORDER BY fs.FundingSourceId
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM FundingSources fs
                    WHERE fs.ProjectId = Payments.ProjectId
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE Payments SET SourceFundingSourceId = NULL");
        }
    }
}
