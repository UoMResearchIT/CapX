using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public abstract class BaseScheduledTask : BaseTask
    {
        protected double demand;
        /// <summary>
        /// The minimum demand required to complete this task in FTE.
        /// </summary>
        [Required]
        public virtual double Demand { get; set; }

        /// <summary>
        /// Used to drive the end date from the start date assuming 7 hour days. This is includes weekends.
        /// </summary>
        public int DurationDays { get; set; }

        /// <summary>
        /// Used to drive the work assuming each day is 220 billable days spread over the year of 365 days so roughly 4.22 hours per calendar day.
        /// </summary>
        public int DurationBillableDays { get; set; }

        /// <summary>
        /// Sets the calendar days duration based on the start and end dates.
        /// </summary>
        protected void UpdateDurationFromDates()
        {
            // Tasks that start and end on the same day should still have a duration of 1 day so add a day here
            DurationDays = (int)Math.Round(EndDate.Date.Subtract(StartDate.Date).TotalDays) + 1;
        }

        /// <summary>
        /// Update the end date asssuming a fixed start date and duration in calendar days.
        /// </summary>
        protected void UpdateEndDateFromDuration()
        {
            EndDate = StartDate.Date.AddDays(DurationDays - 1).Date;
        }

        /// <summary>
        /// Updates the duration of the task based on the units and planned work.
        /// </summary>
        /// <param name="units"></param>
        protected void UpdateDuration(double units)
        {
            if (units == 0)
            {
                DurationDays = 0;
                DurationBillableDays = 0;
            }
            else
            {
                // Compute the billable days from the planned work of the task where a billable day is 7 hours of work
                var billableDays = PlannedWorkHours / (7 * units);
                DurationDays = (int)Math.Ceiling(GetNumberOfCalendarDays(billableDays));
                DurationBillableDays = (int)Math.Ceiling(billableDays);
            }
        }

        /// <summary>
        /// Updates the work given the units based on the current start date and calendar duration of a task
        /// </summary>
        /// <param name="units"></param>
        protected void UpdateWork(double units)
        {
            // Duration input is calendar days so need to compute billable days to get work
            var billableDays = GetNumberOfBillableDays(StartDate, DurationDays);
            PlannedWorkHours = (int)Math.Floor(billableDays * 7 * units);
            DurationBillableDays = (int)Math.Ceiling(billableDays);
        }

        /// <summary>
        /// Uses 220 billable days per year to estimate the number of billable days between a start date and a number of calendars into the future.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="durationCalendarDays"></param>
        /// <returns></returns>
        public static double GetNumberOfBillableDays(DateTime startDate, int durationCalendarDays)
        {
            var endDate = startDate.AddDays(durationCalendarDays);
            return GetNumberOfBillableDays(startDate, endDate);
        }

        /// <summary>
        /// Uses 220 billable days per year to estimate the number of billable days between two dates.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        internal static double GetNumberOfBillableDays(DateTime startDate, DateTime endDate)
        {
            var calendarDays = endDate.Date.Subtract(startDate.Date).Days;
            return (calendarDays / 365f) * 220f;
        }

        /// <summary>
        /// Converts the number of billable days into a duration of calendar days assuming 220 billable days per 365 day year.
        /// </summary>
        /// <param name="billableDays"></param>
        /// <returns></returns>
        protected double GetNumberOfCalendarDays(double billableDays)
        {
            return (billableDays / 220f) * 365f;
        }
    }
}
