using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a timesheet entity, which is a one calendar week of time records.
    /// </summary>
    public class Timesheet
    {
        /// <summary>
        /// The unique identifier for the timesheet
        /// </summary>
        public int TimesheetId { get; set; }

        /// <summary>
        /// The person associated with the timesheet
        /// </summary>
        [Required]
        public Person Person { get; set; }

        /// <summary>
        /// The date when the timesheet was created
        /// </summary>
        [Required]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// The start date of the timesheet period (Monday)
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }
          
        /// <summary>
        /// Additional information about the timesheet
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// The minimum hours a person is expected to work in a week (i.e. 35)
        /// </summary>
        public int MinHours { get; set; }

        /// <summary>
        /// Represents the status of the timesheet (submitted, approved, rejected, etc.)
        /// </summary>
        public TimesheetStatus Status { get; set; }

        /// <summary>
        /// Represents the date of the status change.
        /// </summary>
        public DateTime DateChanged { get; set; }

        /// <summary>
        /// Represents the person who made the status change.
        /// </summary>
        public Person ChangedBy { get; set; }

        /// <summary>
        /// Represents the records of hours spent on tasks on the days associated with the specific timesheet.
        /// </summary>
        public ICollection<TimesheetActivity> TimesheetEntries { get; set; } = new List<TimesheetActivity>();
    }
}
