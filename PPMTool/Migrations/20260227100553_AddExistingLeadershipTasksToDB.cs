// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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
                    -- 1) SubTasks that require leadership and have demand (deduplicated)
                    SELECT DISTINCT
                        OwningProjectProjectId AS ProjectId,
                        DATE(StartDate) AS StartDate,
                        DATE(EndDate)   AS EndDate
                    FROM SubTasks
                    WHERE RequiresLeadership = 1
                      AND Demand > 0
                      AND OwningProjectProjectId IS NOT NULL
                ),

                ordered AS (
                    -- 2) Keep explicit shape for ordering
                    SELECT ProjectId, StartDate, EndDate
                    FROM leadership_candidates
                ),

                with_running AS (
                    -- 3) Running maximum EndDate up to and including the current row
                    SELECT
                        ProjectId,
                        StartDate,
                        EndDate,
                        MAX(EndDate) OVER (
                            PARTITION BY ProjectId
                            ORDER BY StartDate, EndDate
                            ROWS UNBOUNDED PRECEDING
                        ) AS RunningEnd
                    FROM ordered
                ),

                breaks AS (
                    -- 4) Previous running max *before* this row
                    SELECT
                        ProjectId,
                        StartDate,
                        EndDate,
                        LAG(RunningEnd) OVER (
                            PARTITION BY ProjectId
                            ORDER BY StartDate, EndDate
                        ) AS PrevRunningEnd
                    FROM with_running
                ),

                range_flags AS (
                    -- 5) Start a new group if current StartDate is after the previous running max end (+1 day stitches abutting ranges)
                    SELECT
                        ProjectId,
                        StartDate,
                        EndDate,
                        CASE
                            WHEN PrevRunningEnd IS NULL
                              OR StartDate > DATE(PrevRunningEnd, '+1 day')
                            THEN 1
                            ELSE 0
                        END AS IsNewRange
                    FROM breaks
                ),

                grouped_ranges AS (
                    -- 6) Assign a group number to each merged range
                    SELECT
                        ProjectId,
                        StartDate,
                        EndDate,
                        SUM(IsNewRange) OVER (
                            PARTITION BY ProjectId
                            ORDER BY StartDate, EndDate
                            ROWS UNBOUNDED PRECEDING
                        ) AS RangeGroup
                    FROM range_flags
                ),

                merged_ranges AS (
                    -- 7) Merge per group
                    SELECT
                        ProjectId,
                        MIN(StartDate) AS StartDate,
                        MAX(EndDate)   AS EndDate
                    FROM grouped_ranges
                    GROUP BY ProjectId, RangeGroup
                )

                -- 8) Insert leadership SubTasks
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
