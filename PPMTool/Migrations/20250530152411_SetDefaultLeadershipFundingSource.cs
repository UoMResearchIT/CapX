// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultLeadershipFundingSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Projects
                SET FundingSourceId = (
	                SELECT FundingSourceId
	                FROM FundingSources
	                WHERE FundingSources.ProjectId = Projects.ProjectId
	                GROUP BY FundingSources.ProjectId
	                HAVING COUNT(*) = 1
	            )
                WHERE CostModel = 2
	                AND EXISTS (
		                SELECT 1
		                FROM FundingSources
		                WHERE FundingSources.ProjectId = Projects.ProjectId
		                GROUP BY FundingSources.ProjectId
		                HAVING COUNT(*) = 1
	                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Projects
                SET FundingSourceId = NULL;
            ");
        }
    }
}
