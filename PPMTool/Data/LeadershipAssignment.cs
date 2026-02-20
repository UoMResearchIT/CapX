// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a leadership pseudo-task.
    /// </summary>
    public class LeadershipAssignment : BaseAssignment
    {
        public DateRange DateRange { get; private set; }

        public float LeadershipFTE { get; private set; }

        public LeadershipAssignment(DateRange dateRange, float leadershipFTE, ProjectStatus projectStatus) : base(projectStatus)
        {
            DateRange = dateRange;
            LeadershipFTE = leadershipFTE;
        }

        public override bool IsWithin(DateTime testDate)
        {
            return DateRange.IsWithin(testDate, DateRange.StartDate, DateRange.EndDate);
        }

        public override bool IsWithin(DateTime startDate, DateTime endDate)
        {
            return DateRange.IsWithin(DateRange.StartDate, DateRange.EndDate, startDate, endDate);
        }
    }
}
