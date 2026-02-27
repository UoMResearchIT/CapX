using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddExistingLeadershipTasksToDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This finds the require ranges for leadership and adds tasks to the DB
            migrationBuilder.Sql(@"
                WITH leadership_candidates AS (
                    -- 1. SubTasks that require leadership and have demand
                    SELECT
                        OwningProjectProjectId AS ProjectId,
                        DATE(StartDate) AS StartDate,
                        DATE(EndDate)   AS EndDate
                    FROM SubTasks
                    WHERE
                        RequiresLeadership = 1
                        AND Demand > 0
                        AND OwningProjectProjectId IS NOT NULL
                ),

                ordered_ranges AS (
                    -- 2. Order ranges and inspect previous EndDate per project
                    SELECT
                        ProjectId,
                        StartDate,
                        EndDate,
                        LAG(EndDate) OVER (
                            PARTITION BY ProjectId
                            ORDER BY StartDate
                        ) AS PrevEndDate
                    FROM leadership_candidates
                ),

                range_breaks AS (
                    -- 3. Detect where a new merged range begins
                    SELECT
                        ProjectId,
                        StartDate,
                        EndDate,
                        CASE
                            WHEN PrevEndDate IS NULL
                                 OR StartDate > DATE(PrevEndDate, '+1 day')
                            THEN 1
                            ELSE 0
                        END AS IsNewRange
                    FROM ordered_ranges
                ),

                grouped_ranges AS (
                    -- 4. Assign a group number to each merged range
                    SELECT
                        ProjectId,
                        StartDate,
                        EndDate,
                        SUM(IsNewRange) OVER (
                            PARTITION BY ProjectId
                            ORDER BY StartDate
                            ROWS UNBOUNDED PRECEDING
                        ) AS RangeGroup
                    FROM range_breaks
                ),

                merged_ranges AS (
                    -- 5. Merge overlapping ranges
                    SELECT
                        ProjectId,
                        MIN(StartDate) AS StartDate,
                        MAX(EndDate)   AS EndDate
                    FROM grouped_ranges
                    GROUP BY ProjectId, RangeGroup
                )

                -- 6. Insert leadership SubTasks
                INSERT INTO SubTasks (
                    Name,
                    StartDate,
                    EndDate,
                    DurationDays,
                    DurationBillableDays,
                    RequiresLeadership,
                    Demand,
                    OriginalDemand,
                    TaskType,
                    HasFixedStart,
                    HasFixedEndDate,
                    Lag,
                    PlannedCost,
                    PlannedIndirectCost,
                    PlannedWorkHours,
                    ActualCost,
                    ActualIndirectCost,
                    ActualWorkHours,
                    UnmetDemand,
                    IsLeadershipTask,
                    OwningProjectProjectId
                )
                SELECT
                    'Leadership' AS Name,
                    mr.StartDate,
                    mr.EndDate,
                    CAST(julianday(mr.EndDate) - julianday(mr.StartDate) + 1 AS INTEGER) AS DurationDays,
                    CAST(julianday(mr.EndDate) - julianday(mr.StartDate) + 1 AS INTEGER) AS DurationBillableDays,
                    0 AS RequiresLeadership,
                    p.LeadershipFTE AS Demand,
                    p.LeadershipFTE AS OriginalDemand,
                    1 AS TaskType,                 -- FixedDuration
                    1 AS HasFixedStart,
                    1 AS HasFixedEndDate,
                    0 AS Lag,
                    0.0 AS PlannedCost,
                    0.0 AS PlannedIndirectCost,
                    0.0 AS PlannedWorkHours,
                    0.0 AS ActualCost,
                    0.0 AS ActualIndirectCost,
                    0.0 AS ActualWorkHours,
                    0.0 AS UnmetDemand,
                    1 AS IsLeadershipTask,
                    mr.ProjectId AS OwningProjectProjectId
                FROM merged_ranges mr
                JOIN Projects p
                  ON p.ProjectId = mr.ProjectId;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Just delete all the leadership tasks
            migrationBuilder.Sql(@"
                DELETE FROM SubTasks
                WHERE IsLeadershipTask = 1;
            ");
        }
    }
}
