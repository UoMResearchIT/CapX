using System;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a leadership pseudo-task.
    /// </summary>
    public class LeadershipAssignment : BaseAssignment
    {
        public DateRange DateRange { get; private set; }

        public Project Project { get; private set; }

        public LeadershipAssignment(ProjectStatus projectStatus) : base(projectStatus)
        {
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
