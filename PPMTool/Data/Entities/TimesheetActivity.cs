using System;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents the number of hours worked on a particular timesheet task on a given day
    /// </summary>
    public class TimesheetActivity
    {
        /// <summary>
        /// Represents the ID of the timesheet activity record.
        /// </summary>
        public int TimesheetActivityId { get; set; }

        /// <summary>
        /// Represents the timesheet associated with the timesheet activity
        /// </summary>
        public Timesheet Timesheet { get; set; }

        /// <summary>
        /// Represents the date of the timesheet activity.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Represents the innate code task associated with the timesheet activity.
        /// </summary>
        public InnateCodeTask InnateCodeTask { get; set; }

        /// <summary>
        /// Represents the number of hours spent on the activity.
        /// </summary>
        public double Hours { get; set; }
    }
}