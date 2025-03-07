using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public abstract class BaseTask : CostedItem, IWithin
    {
        [Required]
        public string Name { get; set; }

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
    }
}
