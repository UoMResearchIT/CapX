using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddResourcesToLeadershipTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO Resources (
                    ActualCost,
                    ActualIndirectCost,
                    ActualWorkHours,
                    AssignmentFTE,
                    BilledFTE,
                    DayRate,
                    FundedFromFundingSourceId,
                    IsProvisional,
                    PersonId,
                    PlannedCost,
                    PlannedIndirectCost,
                    PlannedWorkHours,
                    SubTaskId,
                    UseProjectDayRate
                )
                SELECT
                    0.0 AS ActualCost,
                    0.0 AS ActualIndirectCost,
                    0.0 AS ActualWorkHours,

                    p.LeadershipFTE AS AssignmentFTE,
                    p.LeadershipFTE AS BilledFTE,

                    NULL AS DayRate,
                    p.FundingSourceId AS FundedFromFundingSourceId,

                    0 AS IsProvisional,
                    p.ProjectManagerPersonId AS PersonId,

                    0.0 AS PlannedCost,
                    0.0 AS PlannedIndirectCost,
                    0.0 AS PlannedWorkHours,

                    st.SubTaskId,
                    1 AS UseProjectDayRate
                FROM SubTasks st
                JOIN Projects p
                    ON p.ProjectId = st.OwningProjectProjectId
                WHERE
                    st.IsLeadershipTask = 1
                    AND p.ProjectManagerPersonId IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM Resources r
                        WHERE r.SubTaskId = st.SubTaskId
                    );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM Resources
                WHERE SubTaskId IN (
                    SELECT st.SubTaskId
                    FROM SubTasks st
                    WHERE st.IsLeadershipTask = 1
                );
            ");
        }
    }
}
