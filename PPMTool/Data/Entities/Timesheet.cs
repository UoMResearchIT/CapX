using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a timesheet entity
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
        /// The start date of the timesheet period
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The total hours worked in the timesheet period
        /// </summary>
        public int TotalHours { get; set; }

        /// <summary>
        /// Additional information about the timesheet
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// The minimum hours required for the timesheet period
        /// </summary>
        public int MinHours { get; set; }

        /// <summary>
        /// Navigation property for related DailyEntries
        /// </summary>
        public ICollection<ActivityTimeRecord> TimesheetEntries { get; set; } = new List<ActivityTimeRecord>();

        /// <summary>
        /// Navigation property for workflow history
        /// </summary>
        public ICollection<TimesheetWorkflow> Workflows { get; set; } = new List<TimesheetWorkflow>();
    }
}
