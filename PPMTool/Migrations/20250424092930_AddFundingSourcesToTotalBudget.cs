using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddFundingSourcesToTotalBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add funding sources where there aren't any in the DB already for those projects
            // Set the value to the tune of the budget of the project
            migrationBuilder.Sql(@"
                INSERT INTO FundingSources (ProjectId, HasAccountCode, FundingSourceType, Description, AmountAvailable)
                SELECT ProjectId, 0, 2, '[Automatically Added] Source added automatically during data migration', Budget
                FROM Projects
                WHERE Budget > 0
                AND ProjectId NOT IN (
	                SELECT ProjectId
	                FROM FundingSources
	                WHERE ProjectId IS NOT NULL
                );
            ");

            // Where there are funding sources already, if there is an "Other" source with a value of zero, set its
            // value to the budget of the project minus the sum of the other funding sources if positive.
            migrationBuilder.Sql(@"
                UPDATE FundingSources
                SET AmountAvailable = (
	                SELECT p.Budget - SUM(fs.AmountAvailable)
	                FROM Projects p
	                JOIN FundingSources fs ON p.ProjectId = fs.ProjectId
	                WHERE p.ProjectId = FundingSources.ProjectId
	                GROUP BY p.ProjectId
                ),
                Description = CASE
	                WHEN Description IS NULL THEN '[Automatically Modified] Source value updated during data migration'
	                ELSE Description
                END
                WHERE FundingSourceType = 2
                AND AmountAvailable = 0
                AND ProjectId IN (
	                SELECT p.ProjectId
	                FROM Projects p
	                JOIN FundingSources fs ON p.ProjectId = fs.ProjectId
	                WHERE p.Budget > 0
	                GROUP BY p.ProjectId
	                HAVING p.Budget - SUM(fs.AmountAvailable) > 0
                );
            ");

            // Finally, add a new funding source with the value of the positive difference between budget and sum of existing sources
            // if there is a project left with a budget greater than zero and greater than the sum of existing sources
            migrationBuilder.Sql(@"
                INSERT INTO FundingSources (ProjectId, HasAccountCode, FundingSourceType, Description, AmountAvailable)
                SELECT p.ProjectId, 0, 2, '[Automatically Added] Source added automatically during data migration', p.Budget - SUM(fs.AmountAvailable)
                FROM Projects p
                LEFT JOIN FundingSources fs ON p.ProjectId = fs.ProjectId
                WHERE p.Budget > 0
                GROUP BY p.ProjectId
                HAVING p.Budget - SUM(fs.AmountAvailable) > 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not really reversible as we don't store the original values of the funding sources
            // Can remove the automatic additions though
            migrationBuilder.Sql(@"
                DELETE FROM FundingSources
                WHERE Description = '[Automatically Added] Source added automatically during data migration';
            ");
        }
    }
}
