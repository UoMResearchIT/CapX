using System;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a leadership pseudo-task.
    /// </summary>
    public class LeadershipAssignment : BaseAssignment
    {
        public DateRange DateRange { get; set; }

        public LeadershipAssignment(ProjectStatus projectStatus) : base(projectStatus)
        {
        }

        public override bool IsWithin(DateTime testDate)
        {
            return DateRange.StartDate.Date == DateRange.EndDate.Date ? testDate.Date == DateRange.StartDate.Date : testDate.Date >= DateRange.StartDate.Date && testDate.Date <= DateRange.EndDate.Date;
        }

        public override bool IsWithin(DateTime startDate, DateTime endDate)
        {
            return
                IsWithin(endDate) ||
                IsWithin(startDate) ||
                (DateRange.StartDate.Date > startDate.Date && DateRange.EndDate.Date < endDate.Date);
        }
    }
}
