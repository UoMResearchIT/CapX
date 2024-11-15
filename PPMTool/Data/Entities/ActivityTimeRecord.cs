using System;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an activity time record.
    /// </summary>
    public class ActivityTimeRecord
    {
        /// <summary>
        /// Represents the ID of the activity time record.
        /// </summary>
        public int ActivityTimeRecordId { get; set; }

        /// <summary>
        /// yRepresents the timesheet associated with the activity time record.
        /// </summary>
        public Timesheet Timesheet { get; set; }

        /// <summary>
        /// Represents the date of the activity time record.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Represents the innate code task associated with the activity time record.
        /// </summary>
        public InnateCodeTask InnateCodeTask { get; set; }

        /// <summary>
        /// Represents the number of hours for the activity time record.
        /// </summary>
        public double Hours { get; set; }

        /// <summary>
        /// Represents the type of day for the activity time record (e.g., "weekday", "special day", "weekend").
        /// </summary>
        public string DayType { get; set; }
    }
}
