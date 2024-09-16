using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public abstract class BaseTask : CostedItem
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
        internal bool IsWithin(DateTime testDate)
        {
            return StartDate.Date == EndDate.Date ? testDate.Date == StartDate.Date : testDate.Date >= StartDate.Date && testDate.Date <= EndDate.Date;
        }

        /// <summary>
        /// Method to determine whether any part of the task runs within a date range [startDate endDate].
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        internal bool IsWithin(DateTime startDate, DateTime endDate)
        {
            return
                IsWithin(endDate) ||
                IsWithin(startDate) ||
                StartDate.Date <= startDate.Date && EndDate.Date >= endDate.Date;
        }
    }
}
