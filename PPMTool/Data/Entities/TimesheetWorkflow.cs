using System;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a timesheet workflow.
    /// </summary>
    public class TimesheetWorkflow
    {
        /// <summary>
        /// Represents the ID of the timesheet workflow.
        /// </summary>
        public int TimesheetWorkflowId { get; set; }

        /// <summary>
        /// Represents the timesheet associated with the workflow.
        /// </summary>
        public Timesheet Timesheet { get; set; }

        /// <summary>
        /// Represents the status of the workflow.
        /// </summary>
        public TimesheetWorkflowStatus Status { get; set; }

        /// <summary>
        /// Represents the date of the status change.
        /// </summary>
        public DateTime DateChanged { get; set; }

        /// <summary>
        /// Represents the person who made the status change.
        /// </summary>
        public Person ChangedBy { get; set; }

        /// <summary>
        /// Represents the comment associated with the status change.
        /// </summary>
        public string Comment { get; set; }
    }
}
