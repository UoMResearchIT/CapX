using System;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an entry in the timesheet indicating the number of hours worked on a particular timesheet task on a given day
    /// </summary>
    public class TimesheetEntry
    {
        /// <summary>
        /// Represents the ID of the timesheet entry record.
        /// </summary>
        public int TimesheetEntryId { get; set; }

        /// <summary>
        /// Represents the timesheet which owns the timesheet entry
        /// </summary>
        public Timesheet Timesheet { get; set; }

        /// <summary>
        /// Represents the date of the timesheet entry.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Represents the innate code task associated with the timesheet entry.
        /// </summary>
        public InnateCodeTask InnateCodeTask { get; set; }

        /// <summary>
        /// Represents the number of hours spent on the task.
        /// </summary>
        public double Hours { get; set; }
    }
}