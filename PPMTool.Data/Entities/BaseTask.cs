using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a task with durations and costs
    /// </summary>
    public abstract class BaseTask : CostedItem, IWithin
    {
        [Required]
        public string Name { get; set; } = null!;

        public DateTime StartDate { get; set; } = DateTime.Today;

        public DateTime EndDate { get; set; }

        /// <summary>
        /// Method to determine whether a date is in the range [task.startDate task.endDate].
        /// If end date and start date are the same evaluates against start date.
        /// </summary>
        /// <param name="testDate">Date to test</param>
        /// <returns></returns>
        public bool IsWithin(DateTime testDate)
        {
            return DateRange.IsWithin(testDate, StartDate, EndDate);
        }

        /// <summary>
        /// Method to determine whether any part of the task runs within a date range [startDate endDate].
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public bool IsWithin(DateTime startDate, DateTime endDate)
        {
            return DateRange.IsWithin(StartDate, EndDate, startDate, endDate);
        }

        /// <summary>
        /// Computes how many days this task runs in the given week assuming that currentWeekStart is a Monday
        /// </summary>
        /// <param name="currentWeekStart"></param>
        /// <returns></returns>
        public virtual int GetTaskDaysInWeek(DateTime currentWeekStart)
        {
            DateTime weekStart = currentWeekStart.Date;
            DateTime weekEnd = weekStart.AddDays(6);

            // Find the latest start date and the earliest end date
            DateTime overlapStart = StartDate > weekStart ? StartDate : weekStart;
            DateTime overlapEnd = EndDate < weekEnd ? EndDate : weekEnd;

            // If there's no overlap, return 0
            if (overlapEnd < overlapStart)
                return 0;

            // Calculate the number of overlapping days (inclusive)
            return (int)(overlapEnd.Date.Subtract(overlapStart.Date).TotalDays) + 1;
        }
    }
}
