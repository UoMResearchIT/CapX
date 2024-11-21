using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// The status of a timesheet
    /// </summary>
    public enum TimesheetStatus
    {

        /// <summary>
        /// Timesheets that have been submitted but not yet approved
        /// </summary>
        [Description("Submitted")]
        Submitted,

        /// <summary>
        /// Timesheets that have been approved
        /// </summary>
        [Description("Approved")]
        Approved,

        /// <summary>
        /// Timesheets that have been rejected
        /// </summary>
        [Description("Rejected")]
        Rejected
    }
}
